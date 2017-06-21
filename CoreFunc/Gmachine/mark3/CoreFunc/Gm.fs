module Gm

open Language
open Util

type GmGlobals = (Name * Addr) list

type Instruction =
    | Unwind
    | Pushglobal of Name
    | Pushint of int
    | Push of int
    | Mkap
    | Update of int
    | Pop of int
    | Slide of int
    | Alloc of int

type GmCode = Instruction list

type GmStack = Addr list

type Node =
    | NAp of Addr * Addr
    | NNum of int
    | NGlobal of int * GmCode
    | NInd of Addr

type GmHeap = Heap<Node>

type GmStats = int

type GmEnvironment = Name * int list

type GmState =
    { Code : GmCode
      Heap : GmHeap
      Globals : GmGlobals
      Stack : GmStack
      Stats : GmStats }

type CompiledSC = Name * int * GmCode
type GmCompiler = CoreExpr -> GmEnvironment -> GmCode

let emptyState = 
    { Code = []; Heap = Map.empty; Globals = []; Stack = []; Stats = 0 }

let showInstruction i = sprintf "%A" i |> iStr

let showInstructions code =
    iConcat 
        [ iStr "    Code:{";
          code |> List.map showInstruction |> iInterleave iNewline |> iIndent;
          iStr "}"; iNewline ]

let showSc s (name, addr) =
    let arity, code = 
        match heapLookup s.Heap addr with 
        | NGlobal (x,y) -> x,y
        | _ -> failwith "wrong node, expected global"
    iConcat 
        [ iStr "Code for "; iStr name; iNewline;
          showInstructions code; iNewline; iNewline ]

let showNode s a = function
    | NNum n -> iNum n
    | NGlobal (n, g) ->
        let v, _ = List.find (snd >> ((=) a)) s.Globals
        iConcat
            [ iStr "Global "; iStr v ]
    | NAp (a1, a2) ->
        iConcat
            [ iStr "Ap "; showAddr a1;
              iStr " "; showAddr a2 ]
    | NInd a ->
        iConcat
            [ iStr "Ind "; showAddr a ]

let showStackItem s a =
    iConcat
        [ showAddr a; iStr ": ";
          heapLookup s.Heap a |> showNode s a ]

let showStack s =
    iConcat
        [ iStr " Stack:[";
          s.Stack 
          |> List.rev 
          |> List.map (showStackItem s) 
          |> iInterleave iNewline 
          |> iIndent;
          iStr "]"; ]

let showState s =
    iConcat
        [ showStack s; iNewline; 
          showInstructions s.Code; iNewline ]

let statInitial = 0
let statIncSteps s = s + 1
let statGetSteps s = s

let compiledPrimitives = []

let allocateSc heap (name, nargs, is) =
    let heap', addr = heapAlloc heap (NGlobal (nargs, is))
    heap', (name, addr)

let initialCode = [ Pushglobal "main"; Unwind ]

let argOffset n env =
    List.map (fun (name, m) -> (name, m + n)) env

let getArg = function
    | NAp (_, a) -> a
    | _ -> failwith "application node is expected"

let compileLetDefs comp defs env =
    let compileDef (is, env) (v, e) =
        comp e env |> List.append is, argOffset 1 env
    List.fold compileDef ([], env) defs 
    |> fst

let buildLetEnv env defs =
    let defVars =
        List.map fst defs 
        |> List.rev 
        |> List.mapi (fun i x -> x, i)
        |> List.rev
    let envOffset = argOffset (List.length defs) env
    List.append envOffset defVars
    

let compileLet comp defs body env =
    let defsIs = compileLetDefs comp defs env
    let n = (List.length defs)
    let newEnv = buildLetEnv env defs
    List.concat
        [ defsIs
          comp body newEnv
          [Slide n] ]

let compileLetRecDefs comp defs env =
    let compileDef (is, n) (v, e) =
        let is' = List.concat [ is; comp e env; [Update (n - 1)] ]
        is' , (n - 1)
    List.fold compileDef ([], List.length defs) defs 
    |> fst

let compileLetRec comp defs body env =
    let n = List.length defs
    let newEnv = buildLetEnv env defs
    let defsIs = compileLetRecDefs comp defs newEnv
    List.concat
        [ [Alloc n]
          defsIs
          comp body newEnv
          [Slide n] ]

let rec compileC expr env =
    match expr with
    | EVar v ->
        match List.tryFind (fst >> ((=) v)) env with
        | Some (_, n) -> [Push n]
        | None -> [Pushglobal v]
    | ENum n -> [Pushint n]
    | EAp (e1, e2) ->
        List.concat
            [ compileC e2 env
              compileC e1 (argOffset 1 env)
              [Mkap] ]
    | ELet (false, defs, body) ->
        compileLet compileC defs body env
    | ELet (true, defs, body) ->
        compileLetRec compileC defs body env
    | _ ->
        failwithf "cannot compile %A" expr

let compileR expr env = 
    let d = List.length env
    compileC expr env 
    |> List.append
    <| [ Update d; Pop d; Unwind ]

let compileSc (name, env, body) =
    let newEnv = List.mapi (fun i x -> x, i) env
    let compiled = compileR body newEnv 
    name, env.Length, compiled

let buildInitialHeap program = 
    let compiled = 
        List.map compileSc (List.append preludeDefs program)
        |> List.append compiledPrimitives
    mapAccumul allocateSc Map.empty compiled

let compile program =
    let heap, globals = buildInitialHeap program
    { Code = initialCode
      Heap = heap
      Globals = globals
      Stats = statInitial
      Stack = [] }


let gmFinal (s : GmState) =
    s.Code.IsEmpty

let tryFindGlobal s name =
    List.tryFind (fst >> ((=) name)) s.Globals

let pushglobal f (s : GmState) =
    match tryFindGlobal s f with
    | Some (_, x) ->
        { s with Stack = x :: s.Stack }
    | _ ->
        failwithf "cannot find global %s" f

let pushint (n : int) (s : GmState) =
    let name = (string n)
    match tryFindGlobal s name with
    | Some (_, x) -> 
        { s with Stack = x :: s.Stack }
    | None ->
        let newHeap, a = heapAlloc s.Heap (NNum n)
        let newGlobals = (name, a) :: s.Globals
        let newStack = a :: s.Stack
        { s with
            Heap = newHeap
            Globals = newGlobals
            Stack = newStack }

let mkap (s : GmState) =
    match s.Stack with
    | a1 :: a2 :: stackTail ->
        let newHeap, a = heapAlloc s.Heap (NAp (a1, a2))
        let newStack =  a :: stackTail
        { s with 
            Stack = newStack
            Heap = newHeap }
    | _ -> failwith "Two arguments expected in stack!"

let push n s =
    let argAddr = s.Stack |> List.item n
    { s with Stack = argAddr :: s.Stack }

let slide n s =
    let a = List.head s.Stack
    let rest = List.skip (n + 1) s.Stack
    { s with Stack = a :: rest }

let rearrange n heap stack =
    let tail = stack |> List.tail
    let args = tail |> List.take n |> List.map (heapLookup heap >> getArg)
    List.skip n stack |> List.append args 

let unwind s =
    assert (s.Code.Length = 1)

    match heapLookup s.Heap (List.head s.Stack)  with
    | NNum n ->
        { s with Code = [] }
    | NAp (a1, a2) -> 
        { s with
            Stack = a1 :: s.Stack 
            Code = [Unwind] }
    | NGlobal (n, c) ->
        if s.Stack.Length < n then
            failwith "Unwinding with two few arguments"
        else
            { s with 
                Code = c
                Stack = rearrange n s.Heap s.Stack }
    | NInd a ->
        { s with 
            Stack = a :: List.tail s.Stack
            Code = [Unwind] }

let updateInstr n s =
    let a = List.head s.Stack
    let tail = List.tail s.Stack
    let an = List.item n tail
    let newHeap = heapUpdate s.Heap an (NInd a)
    { s with
        Stack = tail
        Heap = newHeap }

let pop n s =
    { s with
        Stack = List.skip n s.Stack }

let rec alloc n s =
    if n = 0 then s
    else
        let heap', a = heapAlloc s.Heap (NInd heapNull)
        alloc (n - 1) { s with Heap = heap'; Stack = a :: s.Stack }

let dispatch (i : Instruction) =
    match i with
    | Pushglobal f -> pushglobal f
    | Pushint n -> pushint n
    | Mkap -> mkap
    | Push n -> push n
    | Update n -> updateInstr n
    | Pop n -> pop n
    | Unwind -> unwind
    | Slide n -> slide n
    | Alloc n -> alloc n

let wri s =
    System.IO.File.WriteAllText("output3.txt", s)

let step (s : GmState) = 
    match s.Code with
    | i :: is -> dispatch i { s with Code = is }
    | _ -> failwith "expected nonempty code in step"

let doAdmin (s : GmState) =
    { s with 
        Stats = statIncSteps s.Stats}

let rec eval state =        
    let rec go state states =
        if gmFinal state then
            states
        else
            let newState = state |> step |> doAdmin
            go newState (state :: states)
    go state [] |> List.rev

let showStats s =
    iConcat 
        [ iStr "Steps taken:"; s.Stats |> statGetSteps |> iNum ]

let showResults states =
    let s  = List.head states
    iConcat   
        [ iStr "Supercombinator definitions"; iNewline;
          iInterleave iNewline (List.map (showSc s) s.Globals); iNewline; iNewline; 
          iStr "State transitions"; iNewline; iNewline;
          iLayn (List.map showState states); iNewline; iNewline;
          showStats (List.last states) ]
    |> iDisplay

let runProg<'p> = parse >> compile >> eval >> showResults