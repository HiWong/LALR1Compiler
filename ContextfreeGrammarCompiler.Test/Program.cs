﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LALR1Compiler;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace ContextfreeGrammarCompiler.Test
{
    partial class Program
    {
        static void Main(string[] args)
        {
            string[] directories = Directory.GetDirectories(
                ".", "*.Grammar", SearchOption.AllDirectories)
                .OrderBy(x =>
                    (new FileInfo(Directory.GetFiles(
                        x, "*.Grammar", SearchOption.TopDirectoryOnly)[0]).Length))
                    .ToArray();
            DeleteFiles(directories);
            TextWriter originalOut = Console.Out;
            foreach (var directory in directories)
            {
                Console.WriteLine("Testing {0}", directory);
                StreamWriter writer = new StreamWriter("ContextfreeGrammarCompiler.Test.log", false);
                writer.AutoFlush = true;
                Console.SetOut(writer);
                Run(directory);
                writer.Close();
                Console.SetOut(originalOut);
            }
        }

        private static void DeleteFiles(string[] directories)
        {
            foreach (var dir in directories)
            {
                string[] files = Directory.GetFiles(dir, "*.log", SearchOption.AllDirectories);
                foreach (var item in files)
                {
                    File.Delete(item);
                }
            }
            foreach (var dir in directories)
            {
                string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                foreach (var item in files)
                {
                    File.Delete(item);
                }
            }
        }

        private static void Run(string directory)
        {
            string[] grammarFullnames = Directory.GetFiles(
                directory, "*.Grammar", SearchOption.TopDirectoryOnly);
            foreach (var grammarFullname in grammarFullnames)
            {
                ProcessGrammar(grammarFullname);
            }
        }

        private static void ProcessGrammar(string grammarFullname)
        {
            Console.WriteLine("=====> Processing {0}", grammarFullname);

            FileInfo fileInfo = new FileInfo(grammarFullname);
            string directory = fileInfo.DirectoryName;
            string grammarId = fileInfo.Name.Substring(0,
                fileInfo.Name.Length - (".Grammar").Length);

            string sourceCode = File.ReadAllText(grammarFullname);
            RegulationList grammar = GetGrammar(sourceCode, directory, grammarId);

            DumpCode(grammar, directory, grammarId);

            CompileAndTestCode(directory, grammarId);
        }

        private static void CompileAndTestCode(string directory, string grammarId)
        {
            {
                Console.WriteLine("    Compiling {0} of LR(0) version", grammarId);
                string LR0Directory = Path.Combine(directory, "LR(0)");
                Assembly asm = CompileCode(LR0Directory, grammarId, SyntaxParserMapAlgorithm.LR0);
                Console.WriteLine("    Test Code {0} of LR(0) version", grammarId);
                TestCode(asm, directory, grammarId, LR0Directory, SyntaxParserMapAlgorithm.LR0);
            }
            {
                Console.WriteLine("    Compiling {0} of SLR version", grammarId);
                string SLRDirectory = Path.Combine(directory, "SLR");
                Assembly asm = CompileCode(SLRDirectory, grammarId, SyntaxParserMapAlgorithm.SLR);
                Console.WriteLine("    Test Code {0} of SLR version", grammarId);
                TestCode(asm, directory, grammarId, SLRDirectory, SyntaxParserMapAlgorithm.SLR);
            }
            {
                Console.WriteLine("    Compiling {0} of LR(1) version", grammarId);
                string LR1Directory = Path.Combine(directory, "LR(1)");
                Assembly asm = CompileCode(LR1Directory, grammarId, SyntaxParserMapAlgorithm.LR1);
                Console.WriteLine("    Test Code {0} of LR(1) version", grammarId);
                TestCode(asm, directory, LR1Directory, grammarId, SyntaxParserMapAlgorithm.LR1);
            }
        }

        private static void TestCode(Assembly asm, string directory, string compilerDir, string grammarId, SyntaxParserMapAlgorithm syntaxParserMapAlgorithm)
        {
            if (asm == null)
            {
                Console.WriteLine("Get Compiled Compiler Failed...");
                return;
            }
            try
            {
                LexicalAnalyzer lexi = asm.CreateInstance(
                    GetLexicalAnalyzerName(grammarId)) as LexicalAnalyzer;
                LRSyntaxParser parser = asm.CreateInstance(
                    GetParserName(grammarId, syntaxParserMapAlgorithm)) as LRSyntaxParser;
                string[] sourceCodeFullnames = Directory.GetFiles(
                    directory, "*.Code", SearchOption.TopDirectoryOnly);
                foreach (var fullname in sourceCodeFullnames)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(fullname);
                        string sourceCode = File.ReadAllText(fullname);
                        TokenList tokenList = lexi.Analyze(sourceCode);
                        tokenList.Dump(Path.Combine(compilerDir, fileInfo.Name + ".TokenList.log"));
                        SyntaxTree tree = parser.Parse(tokenList);
                        tree.Dump(Path.Combine(compilerDir, fileInfo.Name + ".Tree.log"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Testing Error:");
                        Console.WriteLine(e);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Running Errors:");
                Console.Write("    ");
                Console.WriteLine(ex);
            }
        }

        private static Assembly CompileCode(string directory, string grammarId, SyntaxParserMapAlgorithm syntaxParserMapAlgorithm)
        {
            string[] files = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
            CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();

            CompilerParameters objCompilerParameters = new CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.Add(
                typeof(LALR1Compiler.LRSyntaxParser).Assembly.Location);
            objCompilerParameters.ReferencedAssemblies.Add(
                typeof(List<>).Assembly.Location);
            objCompilerParameters.ReferencedAssemblies.Add(
                            typeof(Object).Assembly.Location);
            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;
            objCompilerParameters.IncludeDebugInformation = true;
            CompilerResults cr = objCSharpCodePrivoder.CompileAssemblyFromFile(
                objCompilerParameters, files);
            if (cr.Errors.HasErrors)
            {
                Console.WriteLine("Compiling Errors:");
                foreach (var item in cr.Errors)
                {
                    Console.Write("    ");
                    Console.WriteLine(item);
                }

                return null;
            }
            else
            {
                return cr.CompiledAssembly;
            }
        }

        /// <summary>
        /// 生成给定文法的compiler。
        /// </summary>
        /// <param name="grammar"></param>
        /// <param name="directory"></param>
        /// <param name="grammarId"></param>
        private static void DumpCode(RegulationList grammar, string directory, string grammarId)
        {
            {
                Dictionary<TreeNodeType, bool> nullableDict;
                grammar.GetNullableDict(out nullableDict);
                FIRSTCollection firstCollection;
                grammar.GetFirstCollection(out firstCollection, nullableDict);
                Console.WriteLine("    Dump {0}", grammarId + ".FIRST.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(directory, grammarId + ".FIRST.log")))
                { firstCollection.Dump(stream); }
                FOLLOWCollection followCollection;
                grammar.GetFollowCollection(out followCollection, nullableDict, firstCollection);
                Console.WriteLine("    Dump {0}", grammarId + ".FOLLOW.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(directory, grammarId + ".FOLLOW.log")))
                { followCollection.Dump(stream); }
            }
            {
                LR0StateCollection stateCollection;
                LR0EdgeCollection edgeCollection;
                LRParsingMap LR0Map;
                grammar.GetLR0ParsingMap(out LR0Map, out stateCollection, out edgeCollection);

                string LR0Directory = Path.Combine(directory, "LR(0)");
                if (!Directory.Exists(LR0Directory)) { Directory.CreateDirectory(LR0Directory); }

                Console.WriteLine("    Dump {0}", grammarId + ".State.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(LR0Directory, grammarId + ".State.log")))
                { stateCollection.Dump(stream); }
                Console.WriteLine("    Dump {0}", grammarId + ".Edge.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(LR0Directory, grammarId + ".Edge.log")))
                { edgeCollection.Dump(stream); }
                Console.WriteLine("    Dump LR(0) source code...");
                DumpSyntaxParserCode(grammar, LR0Map, grammarId, LR0Directory, SyntaxParserMapAlgorithm.LR0);
                DumpLexicalAnalyzerCode(grammar, grammarId, LR0Directory);
                CopyFixedPart(grammar, grammarId, LR0Directory);
            }

            {
                LRParsingMap SLRMap;
                LR0StateCollection stateCollection;
                LR0EdgeCollection edgeCollection;
                grammar.GetSLRParsingMap(out SLRMap, out stateCollection, out edgeCollection);

                string SLRDirectory = Path.Combine(directory, "SLR");
                if (!Directory.Exists(SLRDirectory)) { Directory.CreateDirectory(SLRDirectory); }

                Console.WriteLine("    Dump {0}", grammarId + ".State.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(SLRDirectory, grammarId + ".State.log")))
                { stateCollection.Dump(stream); }
                Console.WriteLine("    Dump {0}", grammarId + ".Edge.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(SLRDirectory, grammarId + ".Edge.log")))
                { edgeCollection.Dump(stream); }
                Console.WriteLine("    Dump SLR source code...");
                DumpSyntaxParserCode(grammar, SLRMap, grammarId, SLRDirectory, SyntaxParserMapAlgorithm.SLR);
                DumpLexicalAnalyzerCode(grammar, grammarId, SLRDirectory);
            }

            {
                LRParsingMap LR1Map;
                LR1StateCollection stateCollection;
                LR1EdgeCollection edgeCollection;
                grammar.GetLR1ParsingMap(out LR1Map, out stateCollection, out edgeCollection);

                string LR1Directory = Path.Combine(directory, "LR(1)");
                if (!Directory.Exists(LR1Directory)) { Directory.CreateDirectory(LR1Directory); }

                Console.WriteLine("    Dump {0}", grammarId + ".State.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(LR1Directory, grammarId + ".State.log")))
                { stateCollection.Dump(stream); }
                Console.WriteLine("    Dump {0}", grammarId + ".Edge.log");
                using (StreamWriter stream = new StreamWriter(
                    Path.Combine(LR1Directory, grammarId + ".Edge.log")))
                { edgeCollection.Dump(stream); }
                Console.WriteLine("    Dump LR(1) source code...");
                DumpSyntaxParserCode(grammar, LR1Map, grammarId, LR1Directory, SyntaxParserMapAlgorithm.LR1);
                DumpLexicalAnalyzerCode(grammar, grammarId, LR1Directory);
            }
        }

        private static void CopyFixedPart(RegulationList grammar, string grammarId, string directory)
        {

            //throw new NotImplementedException();
        }


    }

}
