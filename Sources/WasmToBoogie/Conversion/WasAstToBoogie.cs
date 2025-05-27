using System;
using WasmToBoogie.Parser.Ast;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public class WasmAstToBoogie
    {
        private readonly string contractName;

        public WasmAstToBoogie(string contractName)
        {
            this.contractName = contractName;
        }

        public BoogieProgram Convert(WasmModule wasmAst)
        {
            Console.WriteLine("🚧 Conversion de l'AST WAT vers Boogie...");

            var program = new BoogieProgram();

            // Simulation : un seul proc vide
            var proc = new BoogieProcedure(
                $"BoogieEntry_{contractName}",
                new(), new(), new(), new(), new(), new()
            );

            var body = new BoogieStmtList();
            body.AddStatement(new BoogieAssertCmd(new BoogieLiteralExpr(true)));

            var impl = new BoogieImplementation(
                proc.Name,
                proc.InParams,
                proc.OutParams,
                new(),
                body
            );

            program.Declarations.Add(proc);
            program.Declarations.Add(impl);

            return program;
        }
    }
}
