grammar LogExpression;

options {
  language=CSharp;
}

@parser::namespace { WDE.Debugger.Logs.LogExpressions.Antlr }
@lexer::namespace { WDE.Debugger.Logs.LogExpressions.Antlr }

expr
    : expr '[' expr ']'                                 # EArrayAccess
    | expr '.' ID                                       # EObjectField
    | expr '*' expr                                     # EMulOp
    | expr '/' expr                                     # EDivOp
    | expr '+' expr                                     # EPlusOp
    | expr '-' expr                                     # EPlusOp
    | expr '<' expr                                     # ELessThan
    | expr '<=' expr                                    # ELessEquals
    | expr '>' expr                                     # EGreaterThan
    | expr '>=' expr                                    # EGreaterEquals
    | expr '==' expr                                    # EEquals
    | expr '!=' expr                                    # ENotEquals
    | expr 'is' 'null'                                  # EIsNull
    | expr 'is' 'not' 'null'                            # EIsNotNull
    | <assoc=right> expr ('&&' | 'and' | 'AND' ) expr   # EAnd
    | <assoc=right> expr ('||' | 'or' | 'OR' ) expr     # EOr
    | '(' expr ')'                                      # EParen
    | ID                                                # EId
    | 'true'                                            # ETrue
    | 'false'                                           # EFalse
    | INT                                               # EInt
    | STRING                                            # EStr
    | expr expr                                         # EConcat
    ;
    
fragment Letter  : Capital | Small ;
fragment Capital : [A-Z\u00C0-\u00D6\u00D8-\u00DE] ;
fragment Small   : [a-z\u00DF-\u00F6\u00F8-\u00FF] ;
fragment Digit : [0-9] ;

INT : Digit+ ;

fragment ID_First : Letter | '_' | '$';
ID : ID_First (ID_First | Digit)* ;
WS : (' ' | '\r' | '\t' | '\n')+ ->  skip;

STRING
    :   '"' StringCharacters? '"'
    ;
fragment StringCharacters
    :   StringCharacter+
    ;
fragment
StringCharacter
    :   ~["\\]
    |   '\\' [tnr"\\]
    ;