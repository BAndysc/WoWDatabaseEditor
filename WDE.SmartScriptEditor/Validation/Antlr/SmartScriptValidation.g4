grammar SmartScriptValidation;

options {
  language=CSharp;
}

@parser::namespace { WDE.SmartScriptEditor.Validation.Antlr }
@lexer::namespace { WDE.SmartScriptEditor.Validation.Antlr }

exprBool
    : '!' exprBool                          # BNegate
    | exprInt '<' exprInt                   # BLessThan
    | exprInt '<=' exprInt                  # BLessEquals
    | exprInt '>' exprInt                   # BGreaterThan
    | exprInt '>=' exprInt                  # BGreaterEquals
    | exprInt '==' exprInt                  # BEqualsInt
    | exprInt '!=' exprInt                  # BNotEqualsInt
    | exprBool '==' exprBool                # BEquals
    | exprBool '!=' exprBool                # BNotEquals
    | <assoc=right> exprBool '&&' exprBool  # BAnd
    | <assoc=right> exprBool '||' exprBool  # BOr
    | 'true'                                # BTrue
    | 'false'                               # BFalse
    | '(' exprBool ')'                      # BParen
    ;

exprInt
    : '-' exprInt                           # ENegate
    | 'target' '.' 'type'                   # ETargetType
    | 'target' '.' 'param' '(' INT ')'      # ETargetParam
    | 'source' '.' 'param' '(' INT ')'      # ESourceParam
    | 'event' '.' 'param' '(' INT ')'       # EEventParam
    | 'action' '.' 'param' '(' INT ')'      # EActionParam
    | 'script' '.' 'source_type'            # ESourceType
    | exprInt mulOp exprInt                 # EMulOp
    | exprInt addOp exprInt                 # EAddOp
    | INT                                   # EInt
    | '(' exprInt ')'                       # EParen
    ;

addOp
    : '+' # Plus
    | '-' # Minus
    | '&' # BitwiseAnd
    | '|' # BitwiseOr
    | '^' # BitwiseXor
    ;

mulOp
    : '*' # Multiply
    | '/' # Divide
    | '%' # Modulo
    ;
    
fragment Letter  : Capital | Small ;
fragment Capital : [A-Z\u00C0-\u00D6\u00D8-\u00DE] ;
fragment Small   : [a-z\u00DF-\u00F6\u00F8-\u00FF] ;
fragment Digit : [0-9] ;

INT : Digit+ ;

fragment ID_First : Letter | '_';
ID : ID_First (ID_First | Digit)* ;
WS : (' ' | '\r' | '\t' | '\n')+ ->  skip;