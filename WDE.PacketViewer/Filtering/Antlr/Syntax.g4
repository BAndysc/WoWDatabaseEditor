grammar Syntax;

options {
  language=CSharp;
}

@parser::namespace { WDE.PacketViewer.Filtering.Antlr }
@lexer::namespace { WDE.PacketViewer.Filtering.Antlr }

expr
    : '!' expr                              # ENegate
    | ID '.' ID            # EFieldValue
    | ID '(' (expr (',' expr)*)? ')' #EFuncAppl
    | expr '*' expr                 # EMulOp
    | expr '/' expr                 # EDivOp
    | expr '+' expr                 # EPlusOp
    | expr '<' expr                   # ELessThan
    | expr '<=' expr                  # ELessEquals
    | expr '>' expr                   # EGreaterThan
    | expr '>=' expr                  # EGreaterEquals
    | expr ('in' | 'IN') expr          # EIn
    | expr '==' expr                  # EEquals
    | expr '!=' expr                  # ENotEquals
    | <assoc=right> expr ('&&' | 'and' | 'AND' ) expr  # EAnd
    | <assoc=right> expr ('||' | 'or' | 'OR' ) expr  # EOr
    | 'true'                                # ETrue
    | 'false'                               # EFalse
    | INT                                   # EInt
    | STRING                   #EStr
    | '(' expr ')'                      # EParen
    ;
    
fragment Letter  : Capital | Small ;
fragment Capital : [A-Z\u00C0-\u00D6\u00D8-\u00DE] ;
fragment Small   : [a-z\u00DF-\u00F6\u00F8-\u00FF] ;
fragment Digit : [0-9] ;

INT : Digit+ ;
STRING : '\'' .*? '\'' | '"' .*? '"';

fragment ID_First : Letter | '_';
ID : ID_First (ID_First | Digit)* ;
WS : (' ' | '\r' | '\t' | '\n')+ ->  skip;