public class Token {

	public TokenKind kind;
	public int startPos;
	public int endPos;
	public String text;

	Token(TokenKind kind) {
		this.kind = kind;
	}

	Token(TokenKind kind, String text) {
		this.kind = kind;
		this.text = text;
	}

	Token(TokenKind kind, int startPos, int endPos) {
		this.kind = kind;
		this.startPos = startPos;
		this.endPos = endPos;
	}

	public String toString() {
		return "[" + kind.toString() + " : " + text + "]";
	}
}