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

            string FreshTemp() => $"tmp{tempCounter++}";

            foreach (var instr in func.Body)
            {
                if (instr.StartsWith("i32.const"))
                {
                    var valueStr = instr.Substring("i32.const".Length).Trim();
                    if (int.TryParse(valueStr, out int val))
                    {
                        stack.Push(new BoogieLiteralExpr(val));
                    }
                }
                else if (instr == "i32.add" || instr == "i32.sub" || instr == "i32.mul" || instr == "i32.div_s")
                {
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();

                        var tmpName = FreshTemp();
                        var tmpVar = new BoogieLocalVariable(new BoogieTypedIdent(tmpName, BoogieType.Int));
                        locals.Add(tmpVar);

                        var opcode = instr switch
                        {
                            "i32.add" => BoogieBinaryOperation.Opcode.ADD,
                            "i32.sub" => BoogieBinaryOperation.Opcode.SUB,
                            "i32.mul" => BoogieBinaryOperation.Opcode.MUL,
                            "i32.div_s" => BoogieBinaryOperation.Opcode.DIV,
                            _ => throw new InvalidOperationException("Unknown arithmetic opcode")
                        };

                        var binExpr = new BoogieBinaryOperation(opcode, left, right);
                        body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(tmpName), binExpr));
                        stack.Push(new BoogieIdentifierExpr(tmpName));
                    }
                }
                else if (instr == "i32.and" || instr == "i32.or")
                {
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();

                        var leftBool = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, left, new BoogieLiteralExpr(0));
                        var rightBool = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, right, new BoogieLiteralExpr(0));

                        var tmpName = FreshTemp();
                        var tmpVar = new BoogieLocalVariable(new BoogieTypedIdent(tmpName, BoogieType.Int));
                        locals.Add(tmpVar);

                        var opcode = instr == "i32.and" ? BoogieBinaryOperation.Opcode.AND : BoogieBinaryOperation.Opcode.OR;
                        var logicExpr = new BoogieBinaryOperation(opcode, leftBool, rightBool);

                        body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(tmpName), logicExpr));
                        stack.Push(new BoogieIdentifierExpr(tmpName));
                    }
                }
                else if (instr == "i32.eq" || instr == "i32.ne" || instr == "i32.lt_s" ||
                         instr == "i32.gt_s" || instr == "i32.le_s" || instr == "i32.ge_s")
                {
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();

                        var tmpName = FreshTemp();
                        var tmpVar = new BoogieLocalVariable(new BoogieTypedIdent(tmpName, BoogieType.Int));
                        locals.Add(tmpVar);

                        var opcode = instr switch
                        {
                            "i32.eq" => BoogieBinaryOperation.Opcode.EQ,
                            "i32.ne" => BoogieBinaryOperation.Opcode.NEQ,
                            "i32.lt_s" => BoogieBinaryOperation.Opcode.LT,
                            "i32.gt_s" => BoogieBinaryOperation.Opcode.GT,
                            "i32.le_s" => BoogieBinaryOperation.Opcode.LE,
                            "i32.ge_s" => BoogieBinaryOperation.Opcode.GE,
                            _ => throw new InvalidOperationException("Unknown comparison opcode")
                        };

                        var cmpExpr = new BoogieBinaryOperation(opcode, left, right);
                        body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(tmpName), cmpExpr));
                        stack.Push(new BoogieIdentifierExpr(tmpName));
                    }
                }
                else if (instr == "drop")
                {
                    if (stack.Count > 0)
                        stack.Pop();
                }
                else
                {
                    body.AddStatement(new BoogieAssertCmd(new BoogieLiteralExpr(true)));
                }
            }

            if (stack.Count > 0)
            {
                body.AddStatement(new BoogieAssertCmd(stack.Peek()));
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

            var impl = new BoogieImplementation(proc.Name, inParams, outParams, locals, body);
            return (proc, impl);
        }
    }
}
