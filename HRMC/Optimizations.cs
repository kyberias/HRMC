using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMC
{
    public static class Optimizations
    {
        public static IEnumerable<CodeGenerator.Instruction> Optimize(IList<CodeGenerator.Instruction> instructions)
        {
            return OptimizeCopyFrom(instructions);
        }

        static IEnumerable<CodeGenerator.Instruction> OptimizeCopyFrom(IList<CodeGenerator.Instruction> instructions)
        {
            /*
    COPYFROM 24
    COPYTO a
    COPYTO b
    COPYTO c
    COPYFROM 24

            -->
    COPYFROM 24
    COPYTO a
    COPYTO b
    COPYTO c
             */

            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Opcode == CodeGenerator.Opcode.CopyFrom)
                {
                    yield return instructions[i];

                    int j = i + 1;
                    for (; j < instructions.Count; j++)
                    {
                        if (instructions[j].Opcode != CodeGenerator.Opcode.CopyTo)
                        {
                            break;
                        }
                        yield return instructions[j];
                    }
                    if (j < instructions.Count && instructions[j].Opcode == CodeGenerator.Opcode.CopyFrom
                         && instructions[j].Operand == instructions[i].Operand)
                    {
                        i = j;
                        continue;
                    }
                    i = j - 1;
                }
                else
                {
                    yield return instructions[i];
                }
            }

        }
    }
}
