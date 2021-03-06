using System;
using System.Collections.Generic;
using System.Linq;

namespace HRMC
{
    /*
<lg-op>		= '&&' | '||'
<eq-op>		= '==' | '!=' | '<' | '>' | '<=' | '>='
<mop>		= '/' | '%' | '*'
<op>		= '+' | '-'

<expr> 		= <eq-expr> (<lg-op> <eq-expr>)*
<eq-expr>	= <mop-expr> (<eq-op> <mop-expr>)
<mop-expr>	= <op-expr> (<mop> <op-expr>)*
<op-expr>	= <prim-expr> (<op> <prim-expr>)*
<prim-expr>	= <var-expr> | <func-expr> | <asgn-expr> | '(' <expr> ')'

<var-expr> 	= symbol
<func-expr> 	= symbol '(' <expr> ')'
<asgn-expr> 	= symbol = <expr>

        a==b && u==i
    */

    // TODO: Correct pointer arithmetic: *a++, (*a)++
    // TODO: Support for OR (||)
    // TODO: Support for table access (e.g. ptr[0], ptr[a]. ptr[42] is problematic since we don't support integer literals
    // TODO: Reading from uninitialized memory address should result in error
    // TODO: Support for for

    public class Parser
    {
        private List<TokenElement> lexicalElements;
        private int cursor;

        public Parser(IEnumerable<TokenElement> lexicalElements)
        {
            this.lexicalElements = lexicalElements.ToList();
            cursor = 0;
        }

        public enum ParserErrorType
        {
            UnexpectedToken,
            UnexpectedEndOfFile
        }

        public class ParserError
        {
            public ParserErrorType Type { get; set; }
            public int Column { get; set; }
            public int Line { get; set; }
        }

        public List<ParserError> Errors { get; set; } = new List<ParserError>();

        TokenElement PeekElement()
        {
            if (lexicalElements.Count > cursor)
            {
                return lexicalElements[cursor];
            }
            return null;
        }

        bool AcceptElementIfNext(Token type)
        {
            var el = PeekElement();
            if (el.Type == type)
            {
                AcceptElement(type);
                return true;
            }
            return false;
        }

        TokenElement AcceptElement(Token type)
        {
            while (true)
            {
                var el = PeekElement();

                if (el == null)
                {
                    Errors.Add(new ParserError
                    {
                        Type = ParserErrorType.UnexpectedEndOfFile,
                        Column = el.Column,
                        Line = el.Line
                    });
                    return null;
                }

                if (el.Type == type)
                {
                    cursor++;
                    return el;
                }

//                if (Errors.Count == 0 || Errors.Last().Type != ParserErrorType.UnexpectedToken)
                {
                    Errors.Add(new ParserError
                    {
                        Type = ParserErrorType.UnexpectedToken,
                        Column = el.Column,
                        Line = el.Line
                    });
                    cursor++;
                    return new TokenElement(type, new Character {column = el.Column, line = el.Line});
                }
            }
        }

        Assignment ParseAssignment(string name, bool indirect = false)
        {
            var ass = new Assignment();
            ass.VariableName = name;
            ass.Indirect = indirect;
            AcceptElement(Token.Is);
            ass.Value = ParseExpression();
            return ass;
        }

        FunctionExpression ParseFunction(string name)
        {
            AcceptElement(Token.ParenOpen);

            var func = new FunctionExpression();
            func.FunctionName = name;

            if (PeekElement().Type != Token.ParenClose)
            {
                func.Arguments = new[]
                {
                    ParseExpression()
                };
            }

            AcceptElement(Token.ParenClose);
            return func;
        }

        ExpressionBase ParsePrimaryExpression()
        {
            var peeked = PeekElement().Type;
            if (peeked == Token.False || peeked == Token.True)
            {
                AcceptElement(peeked);
                return new ConstantLiteralExpression<bool>
                {
                    Value = peeked == Token.True
                };
            }

            if (peeked == Token.Number)
            {
                var ae = AcceptElement(peeked);
                return new ConstantLiteralExpression<int>()
                {
                    Value = int.Parse(ae.Data)
                };
            }

            if (PeekElement().Type == Token.ParenOpen)
            {
                AcceptElement(Token.ParenOpen);
                var ex = ParseExpression();
                AcceptElement(Token.ParenClose);
                return ex;
            }

            bool indirect = false;
            if (PeekElement().Type == Token.Asterisk)
            {
                indirect = true;
                AcceptElement(Token.Asterisk);
            }

            bool preInc = false;
            bool preDec = false;

            var el = PeekElement();
            if (el.Type == Token.Decrement)
            {
                AcceptElement(el.Type);
                preDec = true;
            }
            else if (el.Type == Token.Increment)
            {
                AcceptElement(el.Type);
                preInc = true;
            }

            var symbol = AcceptElement(Token.Symbol);
            el = PeekElement();
            if (el.Type == Token.ParenOpen && !preInc && !preDec)
            {
                return ParseFunction(symbol.Data);
            }

            if (el.Type == Token.Is && !preInc && !preDec)
            {
                return ParseAssignment(symbol.Data, indirect);
            }

            var expr = new VariableExpression
            {
                Name = symbol.Data,
                Indirect = indirect,
                PreIncrement = preInc,
                PreDecrement = preDec
            };

            if (el.Type == Token.Increment)
            {
                AcceptElement(el.Type);
                expr.PostIncrement = true;
            }
            if (el.Type == Token.Decrement)
            {
                AcceptElement(el.Type);
                expr.PostDecrement = true;
            }

            return expr;
        }

        ExpressionBase ParseExpression()
        {
            /*if (PeekElement().Type == HrmLexType.ParenOpen)
            {
                // ( expr )
                AcceptElement(HrmLexType.ParenOpen);
                var exprr = ParseExpression();
                AcceptElement(HrmLexType.ParenClose);
                return exprr;
            }*/

            var expr = new LogicalExpression();
            expr.Expressions.Add(ParseEqualityExpression());
            var tp = PeekElement().Type;

            if (tp != Token.LogicalAnd && tp != Token.LogicalOr)
            {
                return expr.Expressions.First();
            }

            while (tp == Token.LogicalAnd || tp == Token.LogicalOr)
            {
                expr.LogicalOperators.Add(tp);
                AcceptElement(tp);
                expr.Expressions.Add(ParseEqualityExpression());
                tp = PeekElement().Type;
            }
            return expr;
        }

        ExpressionBase ParseMultiplyExpression()
        {
            var expr = new MultiplyExpression();
            expr.Expression1 = ParsePrimaryExpression();

            while (true)
            {
                var tp = PeekElement().Type;
                if (tp != Token.Asterisk && tp != Token.Div && tp != Token.Mod)
                {
                    return expr.Expression1;
                }

                if (tp == Token.Asterisk || tp == Token.Div || tp == Token.Mod)
                {
                    expr.Operator = tp;
                    AcceptElement(tp);
                    expr.Expression2 = ParsePrimaryExpression();
                    expr = new MultiplyExpression
                    {
                        Expression1 = expr
                    };
                }
            }
        }

        ExpressionBase ParseEqualityExpression()
        {
            var expr = new EqualityExpression();
            expr.Expression = ParseOperationExpression();

            var tp = PeekElement().Type;
            if (tp != Token.Equal && tp != Token.LessOrEqualTo && tp != Token.NotEqual && tp != Token.LessThan && tp != Token.GreaterThan && tp != Token.GreaterThanOrEqual)
            {
                return expr.Expression;
            }

            if (tp == Token.Equal || tp == Token.LessOrEqualTo || tp == Token.NotEqual || tp == Token.LessThan || tp == Token.GreaterThan || tp == Token.GreaterThanOrEqual)
            {
                expr.LogicalOperator = tp;
                AcceptElement(tp);
                expr.Expression2 = ParseOperationExpression();
            }

            return expr;
        }

        ExpressionBase ParseOperationExpression()
        {
            var expr = new OperationExpression();
            expr.Expressions.Add(ParseMultiplyExpression());

            var tp = PeekElement().Type;
            if (tp != Token.Plus && tp != Token.Minus)
            {
                return expr.Expressions.First();
            }

            while (tp == Token.Minus || tp == Token.Plus)
            {
                expr.Operators.Add(tp);
                AcceptElement(tp);
                expr.Expressions.Add(ParseMultiplyExpression());
                tp = PeekElement().Type;
            }
            return expr;
        }
 
        IfStatement ParseIfStatement()
        {
            AcceptElement(Token.If);
            AcceptElement(Token.ParenOpen);

            var ifs = new IfStatement();
            ifs.Condition = ParseExpression();

            AcceptElement(Token.ParenClose);

            ifs.Statement = ParseStatement();

            var peeked = PeekElement();
            if (peeked != null && peeked.Type == Token.Else)
            {
                AcceptElement(Token.Else);
                ifs.ElseStatement = ParseStatement();
            }

            return ifs;
        }

        WhileStatement ParseWhileStatement()
        {
            AcceptElement(Token.While);
            AcceptElement(Token.ParenOpen);

            var whs = new WhileStatement();
            whs.Condition = ParseExpression();

            AcceptElement(Token.ParenClose);

            whs.Statement = ParseStatement();

            return whs;
        }

        VariableDeclaration ParseVariableDeclaration()
        {
            var decl = new VariableDeclaration();

            AcceptElement(Token.Int);

            if (PeekElement().Type == Token.Asterisk)
            {
                AcceptElement(Token.Asterisk);
                decl.Pointer = true;
            }

            decl.IsConst = AcceptElementIfNext(Token.Const);

            decl.Name = AcceptElement(Token.Symbol).Data;

            if (PeekElement().Type == Token.BracketOpen)
            {
                AcceptElement(Token.BracketOpen);
                var num = AcceptElement(Token.Number);
                decl.IsArray = true;
                decl.ArraySize = int.Parse(num.Data);
                AcceptElement(Token.BracketClose);
            }

            if (PeekElement().Type == Token.Is)
            {
                AcceptElement(Token.Is);
                decl.Value = ParseExpression();
            }
            AcceptElement(Token.Semicolon);
            return decl;
        }

        BlockStatement ParseBlockStatement()
        {
            AcceptElement(Token.BlockOpen);

            var block = new BlockStatement();
            block.Statements = new List<Statement>();

            while (PeekElement().Type != Token.BlockClose)
            {
                block.Statements.Add(ParseStatement());
            }
            AcceptElement(Token.BlockClose);
            return block;
        }

        Statement ParseStatement()
        {
            var element = PeekElement();
            switch (element.Type)
            {
                case Token.Semicolon:
                    AcceptElement(Token.Semicolon);
                    return new Statement();
                case Token.If:
                    return ParseIfStatement();
                case Token.While:
                    return ParseWhileStatement();
                case Token.Int:
                case Token.Const:
                    return ParseVariableDeclaration();
                case Token.BlockOpen:
                    return ParseBlockStatement();
                case Token.Symbol:
                case Token.Asterisk:
                case Token.Increment:
                case Token.Decrement:
                case Token.ParenOpen:
                    {
                        var ex = new ExpressionStatement {Condition = ParseExpression()};
                        AcceptElement(Token.Semicolon);
                        return ex;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public Program ParseProgram()
        {
            var prg = new Program();
            prg.Statements = new List<Statement>();
            while (PeekElement() != null)
            {
                prg.Statements.Add(ParseStatement());
            }
            return prg;
        }
    }
}
