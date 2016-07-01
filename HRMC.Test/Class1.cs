﻿using System;
using System.Linq;
using NUnit.Framework;

namespace HRMC.Test
{
    [TestFixture]
    public class Class1
    {
        [TestCase("int a = input(); output(a);", new[] { 1 }, ExpectedResult = new[] { 1 }, Description = "Single variable")]
        [TestCase("int a = input(); int b = input(); output(a+b);", new[] { 1,2 }, ExpectedResult = new[] { 3 }, Description = "Addition")]
        [TestCase("output(input()+input());", new[] { 1, 2 }, ExpectedResult = new[] { 3 }, Description = "Addition")]
        [TestCase("output(input()+input()+input());", new[] { 1, 2, 7 }, ExpectedResult = new[] { 10 }, Description = "Addition")]
        [TestCase("output(input()-input()+input()-input());", new[] { 7, 11, 13, 17 }, ExpectedResult = new[] { 7-11+13-17 }, Description = "Addition and subtraction")]

        [TestCase("int a = input(); int b = input(); int c = input(); if(a == b) { output(c); }", new[] { 5, 5, 13 }, ExpectedResult = new[] { 13 }, Description = "Addition and subtraction")]
        [TestCase("int a = input(); int b; while((b = input()) == a) { output(b); }", new[] { 5, 5, 5, 5, 4 }, ExpectedResult = new[] { 5, 5, 5 }, Description = "Addition and subtraction")]

        [TestCase("int a = input(); output(((a)+a)+a);", new[] { 1 }, ExpectedResult = new[] { 3 }, Description = "Parenthesis")]

        [TestCase("int a[10]; int *b = a; while(true) { *a = input(); if(*a == 0) { break; } output(*a); a++; } output(a-b);", new[] { 1 }, ExpectedResult = new[] { 3 }, Description = "Arrays")]

        public int[] CompiledProgramShouldGenerateCorrectOutput(string program, int[] input)
        {
            var lexer = new Tokenizer();
            var parser = new Parser(lexer.Lex(program));

            var prg = parser.ParseProgram();

            var v = new ContextualAnalyzer();
            v.VisitProgram(prg);

            Assert.IsEmpty(v.Errors);

            var codegen = new CodeGenerator();
            codegen.VisitProgram(prg);

            Console.WriteLine(string.Join("\n", codegen.Instructions));

            var intr = new Interpreter();
            return intr.Interpret(codegen.Instructions, input).ToArray();
        }
    }
}