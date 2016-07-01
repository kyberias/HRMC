using System;
using System.Collections.Generic;
using System.Linq;

namespace HRMC
{
    /*
<lg-op>		= '&&' | '||'
<eq-op>		= '==' | '!=' | '<' | '>' | '<=' | '>='
<op>		= '+' | '-'

<expr> 		= <eq-expr> (<lg-op> <eq-expr>)*
<eq-expr>	= <op-expr> (<eq-op> <op-expr>)
<op-expr>	= <prim-expr> (<op> <prim-expr>)*
<prim-expr>	= <var-expr> | <func-expr> | <asgn-expr> | '(' <expr> ')'

<var-expr> 	= symbol
<func-expr> 	= symbol '(' <expr> ')'
<asgn-expr> 	= symbol = <expr>

        a==b && u==i
    */

    // TODO: Support for else
    // TODO: Support for OR (||)
    // TODO: Support for comparision operators (<=, <, >, >=, !=)
    // TODO: Support for variable incrementation (e.g. i++, ++i, i--, --i)
    // TODO: Support for constant literals (e.g. while(true))
    // TODO: Support for table access (e.g. ptr[0], ptr[a]. ptr[42] is problematic since we don't support integer literals
    // TODO: Reading from uninitialized memory address should result in error

    public class Parser
    {
        private List<TokenElement> lexicalElements;
        private int cursor;

        public Parser(IEnumerable<TokenElement> lexicalElements)
        {
            this.lexicalElements = lexicalElements.ToList();
            cursor = 0;
        }

        TokenElement PeekElement()
        {
            if (lexicalElements.Count > cursor)
            {
                return lexicalElements[cursor];
            }
            return null;
        }

        TokenElement AcceptElement(Token type)
        {
            var el = PeekElement();
            if (el.Type == type)
            {
                cursor++;
                return el;
            }
            else
            {
                throw new Exception("Expected " + type);
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

            var symbol = AcceptElement(Token.Symbol);
            var el = PeekElement();
            if (el.Type == Token.ParenOpen)
            {
                return ParseFunction(symbol.Data);
            }
            if (el.Type == Token.Is)
            {
                return ParseAssignment(symbol.Data, indirect);
            }

            return new VariableExpression
            {
                Name = symbol.Data,
                Indirect = indirect
            };
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

        ExpressionBase ParseEqualityExpression()
        {
            var expr = new EqualityExpression();
            expr.Expression = ParseOperationException();

            var tp = PeekElement().Type;
            if (tp != Token.Equals && tp != Token.LessOrEqualTo)
            {
                return expr.Expression;
            }

            if (tp == Token.Equals || tp == Token.LessOrEqualTo)
            {
                expr.LogicalOperator = tp;
                AcceptElement(tp);
                expr.Expression2 = ParseOperationException();
            }

            return expr;
        }

        ExpressionBase ParseOperationException()
        {
            var expr = new OperationExpression();
            expr.Expressions.Add(ParsePrimaryExpression());

            var tp = PeekElement().Type;
            if (tp != Token.Plus && tp != Token.Minus)
            {
                return expr.Expressions.First();
            }

            while (tp == Token.Minus || tp == Token.Plus)
            {
                expr.Operators.Add(tp);
                AcceptElement(tp);
                expr.Expressions.Add(ParsePrimaryExpression());
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
                case Token.If:
                    return ParseIfStatement();
                case Token.While:
                    return ParseWhileStatement();
                case Token.Int:
                    return ParseVariableDeclaration();
                case Token.BlockOpen:
                    return ParseBlockStatement();
                case Token.Symbol:
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
