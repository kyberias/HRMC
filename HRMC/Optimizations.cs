using System.Collections.Generic;
using System.Linq;

namespace HRMC
{
    public static class Optimizations
    {
        public static IEnumerable<CodeGenerator.Instruction> Optimize(IList<CodeGenerator.Instruction> instructions)
        {
            return
                CombineMultipleLabelsIntoOne(
                RemoveInstructionsAfterJump(
                OptimizeBumpUpCopyFrom(
                OptimizeCopyFrom(instructions).ToList()).ToList()).ToList());
        }

        /*    
         Any instructions following an unconditional jump without any labels in between can be removed.
         JUMP u
         JUMP w
        */

        public static IEnumerable<CodeGenerator.Instruction> RemoveInstructionsAfterJump(IList<CodeGenerator.Instruction> instructions)
        {
            bool wasJump = false;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (!wasJump && instructions[i].Opcode == CodeGenerator.Opcode.Jump)
                {
                    yield return instructions[i];
                    wasJump = true;
                    continue;
                }

                if (wasJump)
                {
                    switch (instructions[i].Opcode)
                    {
                        case CodeGenerator.Opcode.Label:
                            wasJump = false;
                            break;
                    }

                    if (!wasJump)
                    {
                        yield return instructions[i];
                    }
                }
                else
                {
                    yield return instructions[i];
                }
            }
        }

        /*
         Multiple labels can be combined into one label:
            o:
            q:
        */

        static IEnumerable<CodeGenerator.Instruction> CombineMultipleLabelsIntoOne(
            IList<CodeGenerator.Instruction> instructions)
        {
            Dictionary<string, string> removedLabels = new Dictionary<string, string>();
            List<CodeGenerator.Instruction> newList = new List<CodeGenerator.Instruction>();

            CodeGenerator.Instruction prevInstruction = null;

            foreach (var i in instructions)
            {
                if (i.Opcode == CodeGenerator.Opcode.Label)
                {
                    if (prevInstruction == null)
                    {
                        prevInstruction = i;
                        newList.Add(i);
                    }
                    else
                    {
                        removedLabels.Add(i.TextOperand, prevInstruction.TextOperand);
                    }
                }
                else
                {
                    prevInstruction = null;
                    newList.Add(i);
                }
            }

            // Fix Jumps
            foreach (var rl in removedLabels)
            {
                foreach(var jump in newList.Where(
                    i => (i.Opcode == CodeGenerator.Opcode.Jump || i.Opcode == CodeGenerator.Opcode.JumpN || i.Opcode == CodeGenerator.Opcode.JumpZ)
                    && i.TextOperand == rl.Key))
                {
                    jump.TextOperand = rl.Value;
                }
            }
            return newList;
        }

        /*
         Labels preceding a JUMP instructions can be removed and the JUMP origins replaced
         by the remaining JUMP's target. 
         
        v:
        w:
            JUMP b
        */

        /*    
         BUMPUP 19
        COPYFROM 19
        */

        static IEnumerable<CodeGenerator.Instruction> OptimizeBumpUpCopyFrom(IList<CodeGenerator.Instruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                if (i > 0 && instructions[i].Opcode == CodeGenerator.Opcode.CopyFrom
                    &&
                    (instructions[i - 1].Opcode == CodeGenerator.Opcode.BumpDn ||
                     instructions[i - 1].Opcode == CodeGenerator.Opcode.BumpUp)
                    && instructions[i].Operand == instructions[i - 1].Operand)
                {

                }
                else
                {
                    yield return instructions[i];
                }
            }
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
