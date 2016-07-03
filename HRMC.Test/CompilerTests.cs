using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace HRMC.Test
{
    [TestFixture]
    [Timeout(3000)]
    public class CompilerTests
    {
        [TestCase("int a = input(); output(a);", new[] {1}, ExpectedResult = new[] {1}, Description = "Single variable")]
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

        [TestCase("int * const a = 5; int * const b = 42; output(*a); output(*b);", new int[] {},
            ExpectedResult = new[] {15, 52})]
        public int[] ConstantPointerVariables(string program, int[] input)
        {
            return Evaluate(program, input, Enumerable.Range(10, 100).ToArray());
        }

        [TestCase("a+b+c", new [] { 1, 2, 3 }, ExpectedResult = new[] { 6 })]
        [TestCase("a*b+c", new[] { 2, 3, 4 }, ExpectedResult = new[] { 10 })]
        [TestCase("a+b*c", new[] { 2, 3, 4 }, ExpectedResult = new[] { 14 })]
        [TestCase("a*b*c", new[] { 2, 3, 4 }, ExpectedResult = new[] { 24 })]
        [TestCase("a/b+c", new[] { 3, 2, 0 }, ExpectedResult = new[] { 1 })]
        [TestCase("a/b+c", new[] { 30, 5, 2 }, ExpectedResult = new[] { 8 })]
        [TestCase("a%b+c", new[] { 7, 4, 0 }, ExpectedResult = new[] { 3 })]
        public int[] Arithmetic(string expression, int[] input)
        {
            var mem = new int[10];
            mem[9] = 0;
            var program = "int * const Zptr = 9; int a = input(); int b = input(); int c = input(); output(" + expression + ");";
            return Evaluate(program, input);
        }

        [TestCase("int * const a = 5; output(a);", ExpectedResult = ContextualErrorCode.CannotUseConstPointerValue)]
        public ContextualErrorCode ConstantPointerValueShouldNotWork(string program)
        {
            return EvaluateErrors(program).First();
        }

        [TestCase("int * const a;", ExpectedResult = ContextualErrorCode.ConstantVariableMustHaveValue)]
        public ContextualErrorCode ConstantValueShouldBeMandatory(string program)
        {
            return EvaluateErrors(program).First();
        }

        [TestCase("int buf[50]; int * const Z = 30; int *p = *Z; output(*Z); output(*p);", 
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
            return Evaluate("int reserved[4]; int * const addrA = 0; int * const addrB = 1; int *a=*addrA; int *b = *addrB; if(*a " + op+" *b) { output(*a); } ", 
                new int[] {}, mem);
        }


        [TestCase(new[] { 1,2,3,4,5,0 }, ExpectedResult = new int[] { 1,2,3,4,5 })]
        public int[] CopyTest(int[] input)
        {
            var prg = @"
int ad[10];

int * const Zptr = 24;
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
            var prg = ReadFileFromResource("compare.c");

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

        [TestCase(new[] { 1, 2, 3, 4, 5, 0, }, ExpectedResult = new int[] { 1, 2, 3, 4, 5 })]
        [TestCase(new[] { 5, 4, 3, 2, 1, 0, }, ExpectedResult = new int[] { 1, 2, 3, 4, 5 })]
        [TestCase(new[] { 5, 3, 4, 2, 5, 0, }, ExpectedResult = new int[] { 2, 3, 4, 5, 5 })]
        [TestCase(new[] { 5, 0, }, ExpectedResult = new int[] { 5 })]
        [TestCase(new[] { 5, 3, 4, 2, 5, 0, 1, 5, 4, 3, 0}, ExpectedResult = new int[] { 2, 3, 4, 5, 5, 1, 3, 4, 5  })]
        public int[] TestSort(int[] input)
        {
            var mem = new int[25];
            mem[24] = 0;
            var src = ReadFileFromResource("sorting.c");
            return Evaluate(src, input, mem).ToArray();
        }

        [TestCase(new[] { 502, 358, 42, 6 }, ExpectedResult = new int[] { 5, 0, 2, 3, 5, 8, 4, 2, 6 })]
        public int[] TestDigitExploder(int[] input)
        {
            var mem = new int[12];
            mem[9] = 0;
            mem[10] = 10;
            mem[11] = 100;
            var src = ReadFileFromResource("digitexploder.c");
            return Evaluate(src, input, mem).ToArray();
        }

        [TestCase(new[] { 1, 2, 3 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 1, 3, 2 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 2, 1, 3 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 2, 3, 1 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 3, 1, 2 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 3, 2, 1 }, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(new[] { 3, 2, 1, 1, 2, 3, 2, 3, 1, 3, 1, 2 }, ExpectedResult = new int[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3 })]
        public int[] TestThreeSort(int[] input)
        {
            var src = ReadFileFromResource("threesort.c");
            return Evaluate(src, input).ToArray();
        }

        [TestCase(new[] { 10, 13, 18 }, ExpectedResult = new int[] { 2, 5, 13, 2, 3, 3 })]
        [Timeout(3000)]
        public int[] TestPrimes(int[] input)
        {
            var mem = new int[25];
            mem[24] = 0;
            var src = ReadFileFromResource("primes.c");
            var res= Evaluate(src, input, mem).ToArray();

            Assert.AreEqual(2, mem[0]);
            Assert.AreEqual(3, mem[1]);
            Assert.AreEqual(5, mem[2]);
            Assert.AreEqual(7, mem[3]);
            Assert.AreEqual(11, mem[4]);
            Assert.AreEqual(13, mem[5]);
            Assert.AreEqual(17, mem[6]);
            Assert.AreEqual(19, mem[7]);
            Assert.AreEqual(23, mem[8]);

            return res;
        }

        [TestCase("int* const ptr = 20; int* const ptrB = 0; ptr = *ptrB;", ExpectedResult = new[] { ContextualErrorCode.ConstValueCannotChange })]
        public IEnumerable<ContextualErrorCode> ConstTest(string prg)
        {
            var errors = EvaluateErrors(prg).ToList();
            return errors;
        }

        string ReadFileFromResource(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "HRMC.Test." + filename;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
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

            var instructions = Optimizations.Optimize(codegen.Instructions).ToList();

            Console.WriteLine(string.Join("\n", instructions));
            Console.WriteLine();

            var intr = new Interpreter(instructions, memory);
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