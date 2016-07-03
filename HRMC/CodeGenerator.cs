using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
            JumpN,
            Add,
            Sub,
            SubIndirect,
            Outbox,
            Inbox,
            BumpUp,
            BumpDn,
            BumpUpIndirect,
            BumpDownIndirect,
            Debug
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
                    case Opcode.BumpUp:
                    case Opcode.BumpDn:
                    case Opcode.Debug:
                        return Opcode.ToString().ToUpper() + " " + Operand;
                    case Opcode.CopyFromIndirect:
                    case Opcode.CopyToIndirect:
                    case Opcode.SubIndirect:
                    case Opcode.BumpUpIndirect:
                    case Opcode.BumpDownIndirect:
                        return Opcode.ToString().ToUpper().Replace("INDIRECT", "") + " [" + Operand + "]";
                    case Opcode.Jump:
                    case Opcode.JumpZ:
                    case Opcode.JumpN:
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

        struct VarDec
        {
            public VariableDeclaration declaration;
            public int? address;
            public int? constantValue;
        }

        private int usedVars;
        Dictionary<string,VarDec> variables = new Dictionary<string, VarDec>();
        private int labels;

        List<int> reservedAddresses = new List<int>();

        int AllocVariable()
        {
            while (reservedAddresses.Contains(usedVars))
            {
                usedVars++;
            }

            return usedVars++;
        }

        void FreeVariable(int v)
        {
            usedVars--;
            Debug.Assert(v == usedVars);
        }

        private string labelAlphabet = "abcdefghijklmnopqrstuvwx";

        string GetNewLabel()
        {
            labels++;
            return LabelNumToString(labels);
        }

        string LabelNumToString(int num)
        {
            int n = num/labelAlphabet.Length;
            int m = num%labelAlphabet.Length;
            StringBuilder sb = new StringBuilder();

            if (n > 0)
            {
                sb.Append(labelAlphabet[n]);
            }
            sb.Append(labelAlphabet[m]);
            return sb.ToString();
        }

        public void VisitVariableDeclaration(VariableDeclaration vardec)
        {
            if (vardec.Pointer && vardec.Value != null && vardec.Value is ConstantLiteralExpression<int> && vardec.IsConst)
            {
                int constVal = (vardec.Value as ConstantLiteralExpression<int>).Value;
                variables[vardec.Name] = new VarDec
                {
                    declaration = vardec,
                    constantValue = constVal
                };
                reservedAddresses.Add(constVal);
                return;
            }

            if (vardec.IsArray)
            {
                variables[vardec.Name] = new VarDec
                {
                    declaration = vardec,
                    address = usedVars
                };
                usedVars += vardec.ArraySize;
                return;
            }

            var addr = AllocVariable();
            variables[vardec.Name] = new VarDec
            {
                declaration = vardec,
                address = addr
            };

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
            var variable = variables[expr.Name];
            if (expr.Indirect)
            {
                if (expr.PreIncrement || expr.PreDecrement)
                {
                    EmitInstruction(expr.PreIncrement ? Opcode.BumpUpIndirect : Opcode.BumpDownIndirect, variable.address.Value);
                }

                if (variable.constantValue.HasValue)
                {
                    EmitInstruction(Opcode.CopyFrom, variable.constantValue.Value);
                }
                else if(!expr.PreIncrement && !expr.PreDecrement)
                {
                    EmitInstruction(Opcode.CopyFromIndirect, variable.address.Value);
                }
                if (expr.PostIncrement || expr.PostDecrement)
                {
                    var v = AllocVariable();
                    EmitInstruction(Opcode.CopyTo, v);
                    EmitInstruction(expr.PostIncrement ? Opcode.BumpUp : Opcode.BumpDn, variable.address.Value);
                    EmitInstruction(Opcode.CopyFrom, v);
                    FreeVariable(v);
                }
            }
            else
            {
                if (variable.declaration.IsArray)
                {
                    expr.EvaluatedValue = variable.address.Value;
                }
                else
                {
                    if (expr.PreIncrement || expr.PreDecrement)
                    {
                        EmitInstruction(expr.PreIncrement ? Opcode.BumpUp : Opcode.BumpDn, variable.address.Value);
                    }
                    else
                    {
                        EmitInstruction(Opcode.CopyFrom, variable.address.Value);
                    }
                }
                if (expr.PostIncrement || expr.PostDecrement)
                {
                    var v = AllocVariable();
                    EmitInstruction(Opcode.CopyTo, v);
                    EmitInstruction(expr.PostIncrement ? Opcode.BumpUp : Opcode.BumpDn, variable.address.Value);
                    EmitInstruction(Opcode.CopyFrom, v);
                    FreeVariable(v);
                }
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
            switch (expr.FunctionName)
            {
                case "output":
                    EmitInstruction(Opcode.Outbox);
                    break;
                case "input":
                    EmitInstruction(Opcode.Inbox);
                    break;
                case "debug":
                    EmitInstruction(Opcode.Debug, ((ConstantLiteralExpression<int>)expr.Arguments[0]).Value);
                    break;
            }
        }

        public void VisitIfStatement(IfStatement stmt)
        {
            DoIfWhile(stmt.Condition, stmt.Statement, stmt.ElseStatement, false);
        }

        public void DoIfWhile(ExpressionBase condition, Statement statement, Statement elseStatement, bool isWhile)
        {
            var label1 = GetNewLabel();

            // Handle constant value (true/false)
            if (condition.EvaluatedValue != null)
            {
                if ((bool)condition.EvaluatedValue)
                {
                    if (isWhile)
                    {
                        EmitInstruction(Opcode.Label, label1);
                    }
                    statement.Visit(this);
                    if (isWhile)
                    {
                        EmitInstruction(Opcode.Jump, label1);
                    }
                    return;
                }
                else
                {
                    // condition is always false, don't generate code

                    if (elseStatement != null)
                    {
                        elseStatement.Visit(this);
                    }

                    return;
                }
            }

            var start = GetNewLabel();
            var exit = GetNewLabel();
            if (isWhile)
            {
                EmitInstruction(Opcode.Label, start);
            }

            var cond = condition as LogicalExpression;

            var expressions = cond != null ? cond.Expressions : new List<ExpressionBase>() { condition };

            // Supporting only AND now

            for (int i = 0; i < expressions.Count; i++)
            {
                var e = expressions[i];
                e.Visit(this);

                //if (i < expr.Expressions.Count - 1)
                {
                    switch (e.Trueness)
                    {
                        case Trueness.Zero:
                            {
                                var lb = GetNewLabel();
                                EmitInstruction(Opcode.JumpZ, lb);
                                EmitInstruction(Opcode.Jump, exit);
                                EmitInstruction(Opcode.Label, lb);
                            }
                            break;
                        case Trueness.NotZero:
                            EmitInstruction(Opcode.JumpZ, exit);
                            break;
                        case Trueness.LessThanZero:
                            {
                                var lb = GetNewLabel();
                                EmitInstruction(Opcode.JumpN, lb);
                                EmitInstruction(Opcode.Jump, exit);
                                EmitInstruction(Opcode.Label, lb);
                            }
                            break;
                        case Trueness.LessThanOrZero:
                            {
                                var lb = GetNewLabel();
                                EmitInstruction(Opcode.JumpZ, lb);
                                EmitInstruction(Opcode.JumpN, lb);
                                EmitInstruction(Opcode.Jump, exit);
                                EmitInstruction(Opcode.Label, lb);
                            }
                            break;
                        case Trueness.MoreThanOrZero:
                            EmitInstruction(Opcode.JumpN, exit);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            statement.Visit(this);

            if (isWhile)
            {
                EmitInstruction(Opcode.Jump, start);
            }

            var realExit = GetNewLabel();
            EmitInstruction(Opcode.Jump, realExit);

            EmitInstruction(Opcode.Label, exit);
            elseStatement?.Visit(this);
            EmitInstruction(Opcode.Label, realExit);
        }

        public void VisitWhileStatement(WhileStatement stmt)
        {
            DoIfWhile(stmt.Condition, stmt.Statement, null, true);
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
            stmt.Value.Visit(this);
            if (stmt.Indirect)
            {
                EmitInstruction(Opcode.CopyToIndirect, variables[stmt.VariableName].address.Value);
            }
            else
            {
                EmitInstruction(Opcode.CopyTo, variables[stmt.VariableName].address.Value);
            }
        }

        public void VisitExpressionStatement(ExpressionStatement stmt)
        {
            stmt.Condition.Visit(this);
        }

        public void VisitLogicalExpression(LogicalExpression expr)
        {
            var exit = GetNewLabel();
            for (int i = 0; i < expr.Expressions.Count; i++)
            {
                var e = expr.Expressions[i];
                e.Visit(this);

                if (e.Trueness == Trueness.Zero)
                {
                    var lb = GetNewLabel();
                    EmitInstruction(Opcode.JumpZ, lb);
                    EmitInstruction(Opcode.Jump, exit);
                    EmitInstruction(Opcode.Label, lb);
                }
                else if (e.Trueness == Trueness.NotZero)
                {
                    EmitInstruction(Opcode.JumpZ, exit);
                    if (expr.Trueness == Trueness.Zero)
                    {
                        var temp = AllocVariable();
                        EmitInstruction(Opcode.CopyTo, temp);
                        EmitInstruction(Opcode.Sub, temp);
                        FreeVariable(temp);
                    }
                }
                else if (e.Trueness == Trueness.LessThanZero)
                {
                    var lb = GetNewLabel();
                    EmitInstruction(Opcode.JumpN, lb);
                    EmitInstruction(Opcode.Jump, exit);
                    EmitInstruction(Opcode.Label, lb);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            EmitInstruction(Opcode.Label, exit);
        }

        public void VisitEqualityExpression(EqualityExpression expr)
        {
            // If either expression is variable, calculate the other one first

            if (expr.Expression is VariableExpression && expr.Expression2 is VariableExpression
                && !((VariableExpression)expr.Expression).Indirect && !((VariableExpression)expr.Expression2).Indirect)
            {
                expr.Expression.Visit(this);
                EmitInstruction(Opcode.Sub, variables[((VariableExpression)expr.Expression2).Name].address.Value);
                // Accumulator is zero when left == right
            }
            else
            {
                // Compare to 0

                var ex1c = expr.Expression as ConstantLiteralExpression<int>;
                var ex2c = expr.Expression2 as ConstantLiteralExpression<int>;

                if (ex1c != null || ex2c != null)
                {
                    if (ex1c == null)
                    {
                        expr.Expression.Visit(this);
                    }
                    else
                    {
                        expr.Expression2.Visit(this);
                    }
                    return;
                }

                expr.Expression.Visit(this);
                if (expr.Expression2 is VariableExpression && ((VariableExpression) expr.Expression2).Indirect)
                {
                    var v = variables[((VariableExpression) expr.Expression2).Name];

                    if (v.declaration.IsConst && v.declaration.Pointer)
                    {
                        EmitInstruction(Opcode.Sub, v.constantValue.Value);
                    }
                    else
                    {
                        EmitInstruction(Opcode.SubIndirect,
                            v.address.Value);
                    }
                }
                else
                {
                    // We need a temp
                    var temp = AllocVariable();
                    EmitInstruction(Opcode.CopyTo, temp);
                    expr.Expression2.Visit(this);
                    EmitInstruction(Opcode.Sub, temp);
                    FreeVariable(temp);
                }
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

                        FreeVariable(t2);
                    }
                }

                if (i < expr.Expressions.Count - 1)
                {
                    EmitInstruction(Opcode.CopyTo, temp);
                }
            }
            FreeVariable(temp);
        }

        void Multiply(MultiplyExpression expr)
        {
            expr.Expression1.Visit(this);

            var temp1 = AllocVariable();
            var res = AllocVariable();

            EmitInstruction(Opcode.CopyTo, temp1);
            EmitInstruction(Opcode.CopyTo, res);
            expr.Expression2.Visit(this);

            var endLabel = GetNewLabel();
            EmitInstruction(Opcode.JumpZ, endLabel);

            var temp2 = AllocVariable();
            EmitInstruction(Opcode.CopyTo, temp2);

            var loopLabel = GetNewLabel();
            EmitInstruction(Opcode.Label, loopLabel);
            EmitInstruction(Opcode.BumpDn, temp2);

            var resultLabel = GetNewLabel();
            EmitInstruction(Opcode.JumpZ, resultLabel);
            EmitInstruction(Opcode.CopyFrom, temp1);
            EmitInstruction(Opcode.Add, res);
            EmitInstruction(Opcode.CopyTo, res);
            EmitInstruction(Opcode.Jump, loopLabel);

            EmitInstruction(Opcode.Label, resultLabel);
            EmitInstruction(Opcode.CopyFrom, res);

            FreeVariable(temp2);
            FreeVariable(res);
            FreeVariable(temp1);
            EmitInstruction(Opcode.Label, endLabel);
        }

        void Divide(MultiplyExpression expr)
        {
            expr.Expression1.Visit(this);

            var temp1 = AllocVariable();
            var res = AllocVariable();

            EmitInstruction(Opcode.CopyTo, temp1);
            EmitInstruction(Opcode.CopyFrom, variables["Zptr"].constantValue.Value);
            EmitInstruction(Opcode.CopyTo, res);
            expr.Expression2.Visit(this);

            var endLabel = GetNewLabel();

            var temp2 = AllocVariable();
            EmitInstruction(Opcode.CopyTo, temp2);

            var loopLabel = GetNewLabel();
            EmitInstruction(Opcode.Label, loopLabel);

            var resultLabel = GetNewLabel();
            EmitInstruction(Opcode.CopyFrom, temp1);
            EmitInstruction(Opcode.Sub, temp2);
            EmitInstruction(Opcode.JumpN, resultLabel);
            EmitInstruction(Opcode.CopyTo, temp1);
            EmitInstruction(Opcode.BumpUp, res);
            EmitInstruction(Opcode.Jump, loopLabel);

            EmitInstruction(Opcode.Label, resultLabel);
            if (expr.Operator == Token.Div)
            {
                EmitInstruction(Opcode.CopyFrom, res);
            }
            else
            {
                EmitInstruction(Opcode.CopyFrom, temp1);
            }

            FreeVariable(temp2);
            FreeVariable(res);
            FreeVariable(temp1);
            EmitInstruction(Opcode.Label, endLabel);
        }

        public void Visit(MultiplyExpression expr)
        {
            if (expr.Operator == Token.Asterisk)
            {
                Multiply(expr);
            }
            else if (expr.Operator == Token.Div || expr.Operator == Token.Mod)
            {
                Divide(expr);
            }
        }

        public void Visit<T>(ConstantLiteralExpression<T> expr)
        {
            //throw new NotImplementedException();
        }
    }
}