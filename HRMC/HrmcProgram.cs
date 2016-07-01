using System;

namespace HRMC
{
    class HrmcProgram
    {
        static void Main(string[] args)
        {
            var program = @"
int u=input();
int a;
a = u;
if(a==a+u+u && u==u && u<=u)
{
    output(u); 
}";

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

            Console.WriteLine(string.Join("\n", codegen.Instructions));
        }
    }
}