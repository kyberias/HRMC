using System;
using System.Collections.Generic;
using System.Linq;

namespace HRMC
{
    public class Interpreter
    {
        public IEnumerable<int> Interpret(List<CodeGenerator.Instruction> instructions, IEnumerable<int> input)
        {
            // scan labels
            var labels = instructions
                .Select((a, b) => new { ix = b, label = a })
                .Where(i => i.label.Opcode == CodeGenerator.Opcode.Label)
                .ToDictionary(e => e.label.TextOperand);

            int pc = 0;
            int? acc = 0;

            var memory = new int[100];

            var inputen = input.GetEnumerator();
            bool inputvalid = inputen.MoveNext();

            while (pc < instructions.Count)
            {
                var inst = instructions[pc];

                switch (inst.Opcode)
                {
                    case CodeGenerator.Opcode.Inbox:
                        if (!inputvalid)
                        {
                            yield break;
                        }
                        acc = inputen.Current;
                        inputvalid = inputen.MoveNext();
                        break;
                    case CodeGenerator.Opcode.Outbox:
                        yield return acc.Value;
                        acc = null;
                        break;
                    case CodeGenerator.Opcode.CopyFrom:
                        acc = memory[inst.Operand];
                        break;
                    case CodeGenerator.Opcode.CopyTo:
                        memory[inst.Operand] = acc.Value;
                        break;
                    case CodeGenerator.Opcode.Jump:
                        pc = labels[inst.TextOperand].ix;
                        break;
                    case CodeGenerator.Opcode.JumpZ:
                        if (acc == 0)
                        {
                            pc = labels[inst.TextOperand].ix;
                        }
                        break;
                    case CodeGenerator.Opcode.JumpLZ:
                        if (acc < 0)
                        {
                            pc = labels[inst.TextOperand].ix;
                        }
                        break;
                    case CodeGenerator.Opcode.Add:
                        acc = acc + memory[inst.Operand];
                        break;
                    case CodeGenerator.Opcode.Sub:
                        acc = acc - memory[inst.Operand];
                        break;
                    case CodeGenerator.Opcode.Label:
                        break;
                    default:
                        throw new NotSupportedException();
                }

                pc++;
            }
        }
    }
}