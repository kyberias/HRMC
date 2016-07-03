using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace HRMC.Test
{
    [TestFixture]
    public class Class1
    {
        [TestCase("int a = input(); output(a);", new[] {1}, ExpectedResult = new[] {1}, Description = "Single variable")
        ]
        [TestCase("int a = input(); int b = input(); output(a+b);", new[] {1, 2}, ExpectedResult = new[] {3},
            Description = "Addition")]
        [TestCase("output(input()+input());", new[] {1, 2}, ExpectedResult = new[] {3}, Description = "Addition")]
        [TestCase("output(input()+input()+input());", new[] {1, 2, 7}, ExpectedResult = new[] {10},
            Description = "Addition")]
        [TestCase("output(input()-input()+input()-input());", new[] {7, 11, 13, 17},
            ExpectedResult = new[] {7 - 11 + 13 - 17}, Description = "Addition and subtraction")]

        [TestCase("int a = input(); int b = input(); int c = input(); if(a == b) { output(c); }", new[] {5, 5, 13},
            ExpectedResult = new[] {13}, Description = "Addition and subtraction")]
        [TestCase("int a = input(); int b; while((b = input()) == a) { output(b); }", new[] {5, 5, 5, 5, 4},
            ExpectedResult = new[] {5, 5, 5}, Description = "Addition and subtraction")]

        [TestCase("int a = input(); output(((a)+a)+a);", new[] {1}, ExpectedResult = new[] {3},
            Description = "Parenthesis")]

//        [TestCase("int a[10]; int *b = a; while(true) { *a = input(); if(*a == 0) { break; } output(*a); a++; } output(a-b);", new[] { 1 }, ExpectedResult = new[] { 3 }, Description = "Arrays")]

        public int[] CompiledProgramShouldGenerateCorrectOutput(string program, int[] input)
        {
            return Evaluate(program, input);
        }

        [TestCase("int a=input(); int b=input(); if(a == b) { output(a+a); } else { output(a-b); }", new[] {1, 1},
            ExpectedResult = new[] {2})]
        [TestCase("int a=input(); int b=input(); if(a == b) { output(a+a); } else { output(a-b); }", new[] {5, 1},
            ExpectedResult = new[] {4})]
        public int[] IfElse(string program, int[] input)
        {
            return Evaluate(program, input);
        }

        [TestCase("while(true) { output(input()); }", new[] {1, 2, 3, 4, 5}, ExpectedResult = new[] {1, 2, 3, 4, 5})]
        public int[] TrueLiteral(string program, int[] input)
        {
            return Evaluate(program, input);
        }

        [TestCase("while(false) { output(input()); }", new[] {1, 2, 3, 4, 5}, ExpectedResult = new int[] {})]
        public int[] FalseLiteral(string program, int[] input)
        {
            return Evaluate(program, input);
        }

        [TestCase("const int *a = 5; const int *b = 42; output(*a); output(*b);", new int[] {},
            ExpectedResult = new[] {15, 52})]
        public int[] ConstantPointerVariables(string program, int[] input)
        {
            return Evaluate(program, input, Enumerable.Range(10, 100).ToArray());
        }

        [TestCase("const int *a = 5; output(a);", ExpectedResult = ContextualErrorCode.CannotUseConstPointerValue)]
        public ContextualErrorCode ConstantPointerValueShouldNotWork(string program)
        {
            return EvaluateErrors(program).First();
        }

        [TestCase("const int *a;", ExpectedResult = ContextualErrorCode.ConstantVariableMustHaveValue)]
        public ContextualErrorCode ConstantValueShouldBeMandatory(string program)
        {
            return EvaluateErrors(program).First();
        }

        [TestCase("int buf[50]; const int *Z = 30; int *p = *Z; output(*Z); output(*p);", 
            new [] { 666 },
            ExpectedResult = new[] { 0, 1 })]
        public int[] Arrays(string program, int[] input)
        {
            var memory = new int[100];
            for (int i = 0; i < 100; i++)
            {
                memory[i] = i+1;
            }
            memory[30] = 0;
            return Evaluate(program, input, memory);
        }

        [TestCase(new[] { 2, 2 }, ExpectedResult = new int[] { })]
        [TestCase(new[] { 1, 2 }, ExpectedResult = new int[] { 3 })]
        [TestCase(new[] { 2, 1 }, ExpectedResult = new int[] { 3 })]
        public int[] NotEqual(int[] input)
        {
            return Evaluate("int a=input(); int b = input(); if(a != b) { output(a+b); }", input);
        }

        [TestCase(new[] { 10 }, ExpectedResult = new int[] { 12 })]
        public int[] PostfixIncrementDecrement(int[] input)
        {
            return Evaluate("int a=input(); a++; a++; a++; a--; output(a); ", input);
        }

        [TestCase(new[] { 10 }, ExpectedResult = new int[] { 12 })]
        public int[] PrefixIncrementDecrement(int[] input)
        {
            return Evaluate("int a=input(); ++a; ++a; ++a; --a; output(a); ", input);
        }

        [TestCase(new[] { 1, 1, 1 }, ExpectedResult = new int[] { 1 })]
        [TestCase(new[] { 2, 1, 1 }, ExpectedResult = new int[] {  })]
        [TestCase(new[] { 1, 2, 1 }, ExpectedResult = new int[] { })]
        [TestCase(new[] { 1, 1, 2 }, ExpectedResult = new int[] { })]
        public int[] AndExpressionWithEquality(int[] input)
        {
            return Evaluate("int a=input(); int b=input(); int c=input(); if(a==b && b==c && c==a) { output(a); } ", input);
        }

        [TestCase(new[] { 1, 1, 1 }, ExpectedResult = new int[] {  })]
        [TestCase(new[] { 2, 1, 3 }, ExpectedResult = new int[] { 2 })]
        [TestCase(new[] { 1, 2, 3 }, ExpectedResult = new int[] { 1 })]
        [TestCase(new[] { 3, 1, 2 }, ExpectedResult = new int[] { 3 })]
        public int[] AndExpression(int[] input)
        {
            return Evaluate("int a=input(); int b=input(); int c=input(); if(a!=b && b!=c && c!=a) { output(a); } ", input);
        }

        [TestCase(new[] { 1, 2 }, ExpectedResult = new int[] { 1 })]
        [TestCase(new[] { 1, 3 }, ExpectedResult = new int[] { 1 })]
        [TestCase(new[] { -1, 0 }, ExpectedResult = new int[] { -1 })]
        [TestCase(new[] { -2, -1 }, ExpectedResult = new int[] { -2 })]
        [TestCase(new[] { 2, 1 }, ExpectedResult = new int[] {  })]
        [TestCase(new[] { 1, 1 }, ExpectedResult = new int[] {  })]
        [TestCase(new[] { 2, 2 }, ExpectedResult = new int[] {  })]
        public int[] LessThan(int[] input)
        {
            return Evaluate("int a=input(); int b=input(); if(a<b) { output(a); } ", input);
        }

        /*[TestCase(new[] { 5, 4, 3, 2, 1, 5 }, ExpectedResult = new int[] { 4, 3, 2, 1 })]
        [TestCase(new[] { 5, 5 }, ExpectedResult = new int[] {  })]
        public int[] WhileConditions(int[] input)
        {
            return Evaluate("int a=input(); int b=a; while(b == b && (b = input()) < a && b < a) { output(b); } ", input);
        }*/

        [TestCase(new[] { 2, 3, 5, 6, 0 }, "==", ExpectedResult = new int[] {  })]
        [TestCase(new[] { 2, 3, 5, 5, 0 }, "==", ExpectedResult = new int[] { 5 })]
        [TestCase(new[] { 2, 3, 5, 6, 0 }, "!=", ExpectedResult = new int[] { 5})]
        [TestCase(new[] { 2, 3, 5, 5, 0 }, "!=", ExpectedResult = new int[] {  })]
        [TestCase(new[] { 2, 3, 5, 6, 0 }, "<", ExpectedResult = new int[] { 5 })]
        [TestCase(new[] { 2, 3, 6, 5, 0 }, "<", ExpectedResult = new int[] {  })]
        [TestCase(new[] { 2, 3, 5, 5, 0 }, "<", ExpectedResult = new int[] { })]
        public int[] CompareIndirect(int[] memory, string op)
        {
            var mem = new int[100];
            Array.Copy(memory, mem, memory.Length);
            return Evaluate("int reserved[4]; const int *addrA = 0; const int *addrB = 1; int *a=*addrA; int *b = *addrB; if(*a " + op+" *b) { output(*a); } ", 
                new int[] {}, mem);
        }

        [TestCase(new[] { 1,2,3,4,5,0 }, ExpectedResult = new int[] { 1,2,3,4,5 })]
        public int[] CopyTest(int[] input)
        {
            var prg = @"
int ad[10];

const int *Zptr = 24;
int Zero = *Zptr;
int *a = *Zptr;

while((*a = input()) != 0)
{
	a++;
}
*a = Zero;
a = Zero;

while(*a != 0)
{
    output(*a);
    a++;
}
";

            var mem = new int[100];
            mem[24] = 0;
            mem[25] = 1;
            mem[26] = 10;
            return Evaluate(prg, input, mem);
        }


        [TestCase(new[] { 1, 2, 3, 4, 0, 1, 2, 4, 3, 0 }, ExpectedResult = new int[] { 1,2,3,4 })]
        [TestCase(new[] { 1, 2, 3, 4, 0, 1, 2, 3, 1, 0 }, ExpectedResult = new int[] { 1,2,3,1 })]
        [TestCase(new[] { 1, 2, 3, 4, 0, 1, 2, 3, 0 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 1, 2, 3, 0, 1, 2, 3, 4, 0 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 1, 2, 3, 0, 1, 2, 3, 0 }, ExpectedResult = new int[] { 1, 2, 3 })]
        public int[] CompareTest(int[] input)
        {
            var prg = @"
int ad[10];
int ab[10];

const int *Zptr = 23;
const int *Tptr = 24;

int *a = *Zptr;
int *b = *Tptr;

int *op;

while((*a = input()) != 0)
{
	++a;
}
*a = *Zptr;

while((*b = input()) != 0)
{
	++b;
}
*b = *Zptr;

a = *Zptr;
b = *Tptr;

while(*a != 0 && *b != 0 && *a == *b)
{
	++a;
	++b;
}

if(*a == 0)
{
	op = *Zptr;
}
else if(*b == 0)
{
	op = *Tptr;
}
else if(*a < *b)
{
	op = *Zptr;
}
else
{
	op = *Tptr;
}

while(*op != 0)
{
    output(*op);
    ++op;
}
";
            var mem = new int[25];
            mem[23] = 0;
            mem[24] = 10;
            var res = Evaluate(prg, input, mem).ToArray();
            Assert.AreEqual(0, mem[23]);
            Assert.AreEqual(10, mem[24]);

            int i = 0;
            while (input[i] != 0)
            {
                Assert.AreEqual(input[i], mem[i]);
                i++;
            }
            i += 1;
            int j = 10;
            while (input[i] != 0)
            {
                Assert.AreEqual(input[i], mem[j]);
                i++;
                j++;
            }

            return res;
        }

        int[] Evaluate(string program, int[] input, int[] memory = null)
        {
            var lexer = new Tokenizer();
            var parser = new Parser(lexer.Lex(program));

            var prg = parser.ParseProgram();

            var v = new ContextualAnalyzer();
            v.VisitProgram(prg);

            foreach (var err in v.Errors)
            {
                Console.WriteLine(err.Message);
            }

            Assert.IsEmpty(v.Errors);

            var codegen = new CodeGenerator();
            codegen.VisitProgram(prg);

            Console.WriteLine(string.Join("\n", codegen.Instructions));
            Console.WriteLine();

            var intr = new Interpreter(codegen.Instructions, memory);
            return intr.Run(input).ToArray();
        }

        IEnumerable<ContextualErrorCode> EvaluateErrors(string program)
        {
            var lexer = new Tokenizer();
            var parser = new Parser(lexer.Lex(program));

            var prg = parser.ParseProgram();

            var v = new ContextualAnalyzer();
            v.VisitProgram(prg);

            return v.Errors.Select(e => e.ErrorCode);
        }
    }
}