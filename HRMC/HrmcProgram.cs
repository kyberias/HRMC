﻿using System;
using System.IO;
using System.Linq;

namespace HRMC
{
    class HrmcProgram
    {
        static void Main(string[] args)
        {
            var program = File.ReadAllText(args[0]);

            var lexer = new Tokenizer();
            var tokenStream = lexer.Lex(program);
            var parser = new Parser(tokenStream);
            var prg = parser.ParseProgram();

            var v = new ContextualAnalyzer();
            v.VisitProgram(prg);

            foreach (var err in v.Errors)
            {
                Console.WriteLine(err.Message);
            }

            if (v.Errors.Count > 0)
            {
                return;
            }

            var codegen = new CodeGenerator();
            codegen.VisitProgram(prg);
            Console.WriteLine("-- HUMAN RESOURCE MACHINE PROGRAM --");
            Console.WriteLine();
            Console.WriteLine(string.Join("\r\n", codegen.Instructions.Select(i => i.Opcode == CodeGenerator.Opcode.Label ? i.ToString() : "    " + i.ToString())));
        }
    }
}