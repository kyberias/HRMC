using System.Collections.Generic;
using System.Linq;

namespace HRMC
{
    public class AstBase
    {
        public virtual void Visit(IVisitor visitor)
        {

        }
    }

    public abstract class ExpressionBase : AstBase
    {
        public abstract bool IsBooleanType { get; }

        public object EvaluatedValue { get; set; }
    }

    public class LogicalExpression : ExpressionBase
    {
        public List<ExpressionBase> Expressions { get; set; } = new List<ExpressionBase>();
        public List<Token> LogicalOperators { get; set; } = new List<Token>();
        public override bool IsBooleanType => LogicalOperators.Any() || Expressions.Any(e => e.IsBooleanType);

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitLogicalExpression(this);
        }
    }

    public class EqualityExpression : ExpressionBase
    {
        public ExpressionBase Expression { get; set; }
        public Token? LogicalOperator { get; set; }
        public ExpressionBase Expression2 { get; set; }
        public override bool IsBooleanType => LogicalOperator.HasValue;

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitEqualityExpression(this);
        }
    }

    public class OperationExpression : ExpressionBase
    {
        public List<ExpressionBase> Expressions { get; set; } = new List<ExpressionBase>();
        public List<Token> Operators { get; set; } = new List<Token>();
        public override bool IsBooleanType => false;

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitOperationExpression(this);
        }
    }

    public class PrimaryExpression : ExpressionBase
    {
        public override bool IsBooleanType => false;
    }

    public class VariableExpression : PrimaryExpression
    {
        public string Name { get; set; }
        public bool Indirect { get; set; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitVariableExpression(this);
        }
    }

    public class ConstantLiteralExpression<T> : PrimaryExpression
    {
        public T Value { get; set; }
        public override bool IsBooleanType => true;

        public override void Visit(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class FunctionExpression : PrimaryExpression
    {
        public string FunctionName { get; set; }
        public ExpressionBase[] Arguments { get; set; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitFunctionExpression(this);
        }
    }

    public class Program : AstBase
    {
        public List<Statement> Statements { get; set; }

        public override void Visit(IVisitor visitor)
        {
            foreach (var statement in Statements)
            {
                visitor.VisitStatement(statement);
            }
        }
    }

    public class Statement : AstBase
    {
        
    }

    public class VariableDeclaration : Statement
    {
        public string Name { get; set; }
        public bool IsArray { get; set; }
        public bool IsConst { get; set; }
        public int ArraySize { get; set; }
        public bool Pointer { get; set; }
        public ExpressionBase Value { get; set; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitVariableDeclaration(this);
        }
    }

    public class Assignment : PrimaryExpression
    {
        public string VariableName { get; set; }
        public bool Indirect { get; set; }
        public ExpressionBase Value { get; set; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitAssignment(this);
        }
    }

    public class BlockStatement : Statement
    {
        public List<Statement> Statements { get; set; }
        public override void Visit(IVisitor visitor)
        {
            visitor.VisitBlockStatement(this);
        }
    }

    public class IfStatement : Statement
    {
        public ExpressionBase Condition { get; set; }
        public Statement Statement { get; set; }
        public Statement ElseStatement { get; set; }
        public override void Visit(IVisitor visitor)
        {
            visitor.VisitIfStatement(this);
        }
    }

    public class WhileStatement : Statement
    {
        public ExpressionBase Condition { get; set; }
        public Statement Statement { get; set; }
        public override void Visit(IVisitor visitor)
        {
            visitor.VisitWhileStatement(this);
        }
    }

    public class ExpressionStatement : Statement
    {
        public ExpressionBase Condition { get; set; }

        public override void Visit(IVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
    }
}
