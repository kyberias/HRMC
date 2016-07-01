using System;
using System.Collections.Generic;

namespace HRMC
{
    public class CodeGenerator : IVisitor
    {
        public enum Opcode
        {
            Label,
            CopyFrom,
            CopyTo,
            CopyFromIndirect,
            CopyToIndirect,
            Jump,
            JumpZ,
            JumpLZ,
            Add,
            Sub,
            Outbox,
            Inbox
        }

        public class Instruction
        {
            public Instruction(Opcode opcode, int operand)
            {
                Opcode = opcode;
                Operand = operand;
            }

            public Instruction(Opcode opcode, string operand)
            {
                Opcode = opcode;
                TextOperand = operand;
            }

            public Opcode Opcode { get; set; }
            public int Operand { get; set; }
            public string TextOperand { get; set; }

            public override string ToString()
            {
                switch (Opcode)
                {
                    case Opcode.Label:
                        return TextOperand + ":";
                    case Opcode.Add:
                    case Opcode.Sub:
                    case Opcode.CopyFrom:
                    case Opcode.CopyTo:
                        return Opcode.ToString().ToUpper() + " " + Operand;
                    case Opcode.Jump:
                    case Opcode.JumpZ:
                    case Opcode.JumpLZ:
                        return Opcode.ToString().ToUpper() + " " + TextOperand;
                    case Opcode.Inbox:
                    case Opcode.Outbox:
                        return Opcode.ToString().ToUpper();
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public void VisitProgram(Program program)
        {
            foreach (var stmt in program.Statements)
            {
                stmt.Visit(this);
            }
        }

        public class Error
        {
            public string Message { get; set; }
        }

        public List<Instruction> Instructions { get; } = new List<Instruction>();

        void EmitInstruction(Opcode opcode, int operand = 0)
        {
            Instructions.Add(new Instruction(opcode, operand));
        }

        void EmitInstruction(Opcode opcode, string operand)
        {
            Instructions.Add(new Instruction(opcode, operand));
        }

        List<Error> errors = new List<Error>();
        public IList<Error> Errors => errors;

        void AddError(string msg, params object[] prms)
        {
            errors.Add(new Error { Message = string.Format(msg, prms) });
        }

        private int usedVars;
        Dictionary<string,int> variables = new Dictionary<string, int>();
        private int labels;

        int AllocVariable()
        {
            return usedVars++;
        }

        void FreeVariable()
        {
            usedVars--;
        }

        string GetNewLabel()
        {
            labels++;
            return "l" + labels;
        }

        public void VisitVariableDeclaration(VariableDeclaration vardec)
        {
            var addr = usedVars++;
            variables[vardec.Name] = addr;

            vardec.Value?.Visit(this);

            if (vardec.Value != null)
            {
                EmitInstruction(Opcode.CopyTo, addr);
            }
        }

        public void VisitStatement(Statement stmt)
        {
            //throw new System.NotImplementedException();
        }

        public void VisitExpression(ExpressionBase stmt)
        {
            //throw new System.NotImplementedException();
        }

        public void VisitVariableExpression(VariableExpression expr)
        {
            if (expr.Indirect)
            {
                EmitInstruction(Opcode.CopyFromIndirect, variables[expr.Name]);
            }
            else
            {
                EmitInstruction(Opcode.CopyFrom, variables[expr.Name]);
            }
        }

        public void VisitFunctionExpression(FunctionExpression expr)
        {
            if (expr.Arguments != null)
            {
                foreach (var arg in expr.Arguments)
                {
                    arg.Visit(this);
                }
            }
            if (expr.FunctionName == "output")
            {
                EmitInstruction(Opcode.Outbox);
            }
            if (expr.FunctionName == "input")
            {
                EmitInstruction(Opcode.Inbox);
            }
        }

        public void VisitIfStatement(IfStatement stmt)
        {
            //throw new System.NotImplementedException();
            var label1 = GetNewLabel();
            var label2 = GetNewLabel();
            var label3 = GetNewLabel();

            if (stmt.Condition.EvaluatedValue.HasValue)
            {
                if (stmt.Condition.EvaluatedValue.Value)
                {
                    // condition is always true, always run code
                    stmt.Statement.Visit(this);
                    return;
                }
                else
                {
                    // condition is always false, don't generate code
                    return;
                }
            }

            stmt.Condition.Visit(this);

            EmitInstruction(Opcode.JumpZ, label1);
            EmitInstruction(Opcode.Jump, label2);
            EmitInstruction(Opcode.Label, label1);

            // Evaluate condition in code
            stmt.Statement.Visit(this);
            if (stmt.ElseStatement != null)
            {
                EmitInstruction(Opcode.Jump, label3);
            }

            EmitInstruction(Opcode.Label, label2);
            if (stmt.ElseStatement != null)
            {
                stmt.ElseStatement.Visit(this);
                EmitInstruction(Opcode.Label, label3);
            }

        }

        public void VisitWhileStatement(WhileStatement stmt)
        {
            var label1 = GetNewLabel();
            var label2 = GetNewLabel();
            var label3 = GetNewLabel();

            EmitInstruction(Opcode.Label, label1);

            if (stmt.Condition.EvaluatedValue.HasValue)
            {
                if (stmt.Condition.EvaluatedValue.Value)
                {
                    stmt.Statement.Visit(this);
                    EmitInstruction(Opcode.Jump, label1);
                    return;
                }
                else
                {
                    // condition is always false, don't generate code
                    return;
                }
            }

            stmt.Condition.Visit(this);
            EmitInstruction(Opcode.JumpZ, label2);
            EmitInstruction(Opcode.Jump, label3);
            EmitInstruction(Opcode.Label, label2);
            stmt.Statement.Visit(this);
            EmitInstruction(Opcode.Jump, label1);
            EmitInstruction(Opcode.Label, label3);
        }

        public void VisitBlockStatement(BlockStatement stmt)
        {
            foreach (var st in stmt.Statements)
            {
                st.Visit(this);
            }
        }

        public void VisitAssignment(Assignment stmt)
        {
            //throw new System.NotImplementedException();
            stmt.Value.Visit(this);
            if (stmt.Indirect)
            {
                EmitInstruction(Opcode.CopyToIndirect, variables[stmt.VariableName]);
            }
            else
            {
                EmitInstruction(Opcode.CopyTo, variables[stmt.VariableName]);
            }
        }

        public void VisitExpressionStatement(ExpressionStatement stmt)
        {
            stmt.Condition.Visit(this);
        }

        public void VisitLogicalExpression(LogicalExpression expr)
        {
            for (int i = 0; i < expr.Expressions.Count; i++)
            {
                var e = expr.Expressions[i];
                e.Visit(this);
            }
        }

        public void VisitEqualityExpression(EqualityExpression expr)
        {
            // If either expression is variable, calculate the other one first

            var notVar = expr.Expression is VariableExpression ? expr.Expression2 : expr.Expression;
            var varExpr = notVar == expr.Expression
                ? expr.Expression2 as VariableExpression
                : expr.Expression as VariableExpression;

            if (varExpr != null)
            {
                notVar.Visit(this);
                EmitInstruction(Opcode.Sub, variables[varExpr.Name]);
                // Accumulator is zero when left == right
            }
            else
            {
                // Both are non var, we need a temp register
                expr.Expression.Visit(this);
                var temp = AllocVariable();
                EmitInstruction(Opcode.CopyTo, temp);
                expr.Expression2.Visit(this);
                EmitInstruction(Opcode.Sub, temp);
                FreeVariable();
            }
        }

        public void VisitOperationExpression(OperationExpression expr)
        {
            var temp = AllocVariable();
            for(int i=0;i<expr.Expressions.Count;i++)
            {
                var e = expr.Expressions[i];
                e.Visit(this);

                if (i > 0)
                {
                    if (expr.Operators[i-1] == Token.Plus)
                    {
                        EmitInstruction(Opcode.Add, temp);
                    }
                    else
                    {
                        var t2 = AllocVariable();

                        EmitInstruction(Opcode.CopyTo, t2);
                        EmitInstruction(Opcode.CopyFrom, temp);
                        EmitInstruction(Opcode.Sub, t2);

                        FreeVariable();
                    }
                }

                if (i < expr.Expressions.Count - 1)
                {
                    EmitInstruction(Opcode.CopyTo, temp);
                }
            }
        }

        public void Visit(ConstantLiteralExpression expr)
        {
            
        }
    }
}