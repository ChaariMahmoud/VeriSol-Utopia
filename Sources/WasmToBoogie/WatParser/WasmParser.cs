using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Parser
{
    public class WasmParser
    {
        private readonly string filePath;

        public WasmParser(string filePath)
        {
            this.filePath = filePath;
        }

        public WasmModule Parse()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"❌ Fichier WAT introuvable : {filePath}");

            Console.WriteLine("📖 Lecture du fichier WAT : " + filePath);
            Console.WriteLine("🔄 Conversion WAT → WASM via wat2wasm...");
            string wasmPath = ConvertWatToWasm(filePath);

            Console.WriteLine("🔄 Appel à Binaryen (via wrapper) pour extraire l'AST WAT...");
            IntPtr modulePtr = LoadWasmTextFile(wasmPath);
            if (modulePtr == IntPtr.Zero)
                throw new Exception("❌ Échec de lecture du fichier WASM avec Binaryen.");

            if (!ValidateModule(modulePtr))
                throw new Exception("❌ Le module Binaryen est invalide !");

            // 🌳 Affiche l’AST généré (WAT pretty-printed par Binaryen)
            PrintModuleAST(modulePtr);

            // 🧠 Extraire les informations de base
            int funcCount = GetFunctionCount(modulePtr);
            string firstFuncName = Marshal.PtrToStringAnsi(GetFirstFunctionName(modulePtr));

            Console.WriteLine($"✅ AST simulé généré avec Binaryen : {funcCount} fonction(s)");
            Console.WriteLine($"🧠 Première fonction : {firstFuncName}");

            // ✨ Retourne un AST simplifié
            var module = new WasmModule();
            module.Functions.Add(new WasmFunction
            {
                Body = new List<string> { $"(func ${firstFuncName})" }
            });

            return module;
        }

        private string ConvertWatToWasm(string watPath)
        {
            string wasmPath = Path.ChangeExtension(watPath, ".wasm");

            var startInfo = new ProcessStartInfo
            {
                FileName = "wat2wasm",
                Arguments = $"{watPath} -o {wasmPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = startInfo };
            proc.Start();
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Console.WriteLine("❌ wat2wasm error: " + stderr);
                throw new Exception("wat2wasm conversion failed.");
            }

            return wasmPath;
        }

        // 🔗 Fonctions importées depuis libbinaryenwrapper.so
        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LoadWasmTextFile(string filename);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetFunctionCount(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetFirstFunctionName(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrintModuleAST(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ValidateModule(IntPtr module);
    }

    namespace Ast
    {
        public class WasmModule
        {
            public List<WasmFunction> Functions { get; set; } = new();
        }

        public class WasmFunction
        {
            public List<string> Body { get; set; } = new();
        }
    }
}
