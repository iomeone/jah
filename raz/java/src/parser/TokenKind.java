package parser;
public enum TokenKind {	
    EOF,
    ERROR,
    L_BRACKET,
    R_BRACKET,
    L_SQUARE_BRACKET,
    R_SQUARE_BRACKET,
    HASH,
    HASH_HASH,
    L_PAREN,
    R_PAREN,
    SEMICOLON,
    COLON,
    QUESTION,
    COLON_COLON,
    DOT,
    DOT_STAR,
    ELIPSIS,
    PLUS,
    MINUS,
    STAR,
    SLASH,
    PERCENT,
    HAT,
    AMPERSAND,
    BAR,
    TILDE,
    EXCLAM,
    ASSIGN,
    LESS_THEN,
    GREATER_THEN,
    PLUS_ASSIGN,
    MINUS_ASSIGN,
    STAR_ASSIGN,
    SLASH_ASSIGN,
    PERCENT_ASSIGN,
    HAT_ASSIGN,
    AMPERSAND_ASSIGN,
    BAR_ASSIGN,
    SHIFT_LEFT,
    SHIFT_RIGHT,
    SHIFT_RIGHT_ASSIGN,
    SHIFT_LEFT_ASSIGN,
    EQUAL,
    NOT_EQUAL,
    LESS_EQ,
    GREATER_EQ,
    AMPER_AMPER,
    BAR_BAR,
    PLUS_PLUS,
    MINUS_MINUS,
    COMMA,
    ARROW_STAR,
    ARROW,
    
    COMMENT,
    IDENTIFIER,
    NUMBER,
    CHAR,
    STRING,
    EOD,
    ANGLE_INCLUDE,
    QUOTE_INCLUDE,
	
	SIZEOF("sizeof"),
	
	;

	public final String value;
	TokenKind() {
		value = "";
	}
	TokenKind(String s) {
		value = s;
	}
}
