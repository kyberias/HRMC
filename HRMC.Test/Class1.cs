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