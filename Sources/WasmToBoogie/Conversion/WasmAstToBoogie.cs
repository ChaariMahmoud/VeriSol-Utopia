using BoogieAST;
using System;
using System.Collections.Generic;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Conversion
{
    public class WasmAstToBoogie
    {
        private readonly string contractName;

        public WasmAstToBoogie(string contractName)
        {
            this.contractName = contractName;
        }

        public BoogieProgram Convert(WasmModule wasmModule)
        {
            var program = new BoogieProgram();

            foreach (var func in wasmModule.Functions)
            {
                var (proc, impl) = TranslateFunction(func);
                program.Declarations.Add(proc);
                program.Declarations.Add(impl);
            }

            return program;
        }

        private (BoogieProcedure, BoogieImplementation) TranslateFunction(WasmFunction func)
        {
            var inParams = new List<BoogieVariable>();
            var outParams = new List<BoogieVariable>();
            var locals = new List<BoogieVariable>();
            var body = new BoogieStmtList();
            var stack = new Stack<BoogieExpr>();

            int tempCounter = 0;

            string FreshTemp()
            {
                return $"tmp{tempCounter++}";
            }

            foreach (var instr in func.Body)
            {
                if (instr.StartsWith("i32.const"))
                {
                    var valueStr = instr.Substring("i32.const".Length).Trim();
                    if (int.TryParse(valueStr, out int val))
                    {
                        var litExpr = new BoogieLiteralExpr(val);
                        stack.Push(litExpr);
                    }
                }
                else if (instr == "i32.add")
                {
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();

var tmpName = FreshTemp();
var tmpIdent = new BoogieTypedIdent(tmpName, BoogieType.Int);
var tmpVar = new BoogieLocalVariable(tmpIdent);
locals.Add(tmpVar);

                        var addExpr = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            left,
                            right
                        );

                        var assign = new BoogieAssignCmd(
                            new BoogieIdentifierExpr(tmpName),
                            addExpr
                        );

                        body.AddStatement(assign);
                        stack.Push(new BoogieIdentifierExpr(tmpName));
                    }
                }
                else if (instr == "drop")
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                    }
                }
                else
                {
                    body.AddStatement(new BoogieAssertCmd(new BoogieLiteralExpr(true)));
                }
            }

            if (stack.Count > 0)
            {
                var top = stack.Peek();
                body.AddStatement(new BoogieAssertCmd(top));
            }

            var proc = new BoogieProcedure(
                $"BoogieEntry_{contractName}",
                inParams,
                outParams,
                new List<BoogieAttribute>(),
                new List<BoogieGlobalVariable>(),
                new List<BoogieExpr>(),
                new List<BoogieExpr>()
            );

            var impl = new BoogieImplementation(
                proc.Name,
                inParams,
                outParams,
                locals,
                body
            );

            return (proc, impl);
        }
    }
}
