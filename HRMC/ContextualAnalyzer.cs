using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HRMC
{
    public class CompilerError
    {
        public string Message { get; set; }
    }

    public enum ContextualErrorCode
    {
        UndefinedVariable,
        VariableAlreadyDeclared
    }

    public class ContextualError : CompilerError
    {
        public ContextualErrorCode ErrorCode { get; set; }
    }

    public class ContextualAnalyzer : IVisitor
    {
        public class Error
        {
        }

        public class Variable
        {
            public string Name { get; set; }
            public bool Initialized { get; set; }
        }

        List<Variable> variables = new List<Variable>();

        List<ContextualError> errors = new List<ContextualError>();
        public IList<ContextualError> Errors => errors;

        void AddError(string msg, params object[] prms)
        {
            errors.Add(new ContextualError { Message = string.Format(msg, prms) });
        }

        public void VisitProgram(Program program)
        {
            foreach (var s in program.Statements)
            {
                s.Visit(this);
            }
        }

        public void VisitVariableDeclaration(VariableDeclaration vardec)
        {
            vardec.Value?.Visit(this);

            if (variables.Any(v => v.Name == vardec.Name))
            {
                AddError("Variable {0} already declared.", vardec.Name);
            }
            else
            {
                variables.Add(new Variable { Name = vardec.Name, Initialized = vardec.Value != null});
            }
        }

        public void VisitStatement(Statement stmt)
        {
            stmt.Visit(this);
        }

        public void VisitExpression(ExpressionBase stmt)
        {
            stmt.Visit(this);
        }

        public void VisitVariableExpression(VariableExpression expr)
        {
            var variable = variables.FirstOrDefault(v => v.Name == expr.Name);
            if (variable == null)
            {
                AddError("{0} not declared.", expr.Name);
            }
            else if(!variable.Initialized)
            {
                AddError("Using uninitialized variable {0}.", expr.Name);
            }
        }

        public void VisitFunctionExpression(FunctionExpression expr)
        {
            if (expr.FunctionName == "input")
            {
                if (expr.Arguments != null && expr.Arguments.Length > 0)
                {
                    AddError("input() cannot have arguments.");
                }
            }
            else if (expr.FunctionName == "output")
            {
                if (expr.Arguments == null || expr.Arguments.Length != 1)
                {
                    AddError("output() should have one argument.");
                }
            }
            else
            {
                AddError("Unknown function {0}.", expr.FunctionName);
            }

            if (expr.Arguments != null)
            {
                foreach (var arg in expr.Arguments)
                {
                    arg.Visit(this);
                }
            }
        }

        public void VisitIfStatement(IfStatement stmt)
        {
            stmt.Condition.Visit(this);

            if (!stmt.Condition.IsBooleanType)
            {
                AddError("If statement condition must be of boolean type.");
            }

            stmt.Statement.Visit(this);
        }

        public void VisitWhileStatement(WhileStatement stmt)
        {
            stmt.Condition.Visit(this);

            if (!stmt.Condition.IsBooleanType)
            {
                AddError("While statement condition must be of boolean type.");
            }

            stmt.Statement.Visit(this);
        }

        public void VisitBlockStatement(BlockStatement stmt)
        {
            foreach (var s in stmt.Statements)
            {
                s.Visit(this);
            }
        }

        public void VisitAssignment(Assignment stmt)
        {
            stmt.Value.Visit(this);

            var variable = variables.FirstOrDefault(v => v.Name == stmt.VariableName);

            if (stmt.Value.IsBooleanType)
            {
                AddError("Cannot assign boolean value to a variable");
            }

            if (variable == null)
            {
                AddError("{0} not declared.", stmt.VariableName);
            }
            else
            {
                variable.Initialized = true;
            }
        }

        public void VisitExpressionStatement(ExpressionStatement stmt)
        {
            stmt.Condition.Visit(this);
        }

        public void VisitLogicalExpression(LogicalExpression expr)
        {
            foreach (var e in expr.Expressions)
            {
                e.Visit(this);
            }
        }

        public void VisitEqualityExpression(EqualityExpression expr)
        {
            expr.Expression.Visit(this);
            if (expr.Expression2 != null)
            {
                expr.Expression2.Visit(this);
            }
        }

        public void VisitOperationExpression(OperationExpression expr)
        {
            foreach (var e in expr.Expressions)
            {
                e.Visit(this);
            }
        }
    }
}