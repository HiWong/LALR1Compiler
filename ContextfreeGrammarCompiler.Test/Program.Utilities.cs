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

namespace ContextfreeGrammarCompiler.Test
{
    partial class Program
    {

        private static string GetLexicalAnalyzerName(string grammarId)
        {
            SourceCodeCharType charType = grammarId[0].GetCharType();
            if (charType == SourceCodeCharType.Letter || charType == SourceCodeCharType.UnderLine)
            {
                return string.Format("{0}LexicalAnalyzer", grammarId);
            }
            else
            {
                return string.Format("_{0}LexicalAnalyzer", grammarId);
            }
        }

        private static string GetNodeNameInParser(TreeNodeType node)
        {
            return string.Format("NODE{0}", node.Type);
        }

        private static string GetParserName(string grammarId, SyntaxParserMapAlgorithm algorithm)
        {
            SourceCodeCharType charType = grammarId[0].GetCharType();
            if (charType == SourceCodeCharType.Letter || charType == SourceCodeCharType.UnderLine)
            {
                return string.Format("{0}{1}SyntaxParser", grammarId, algorithm);
            }
            else
            {
                return string.Format("_{0}{1}SyntaxParser", grammarId, algorithm);
            }
        }

        private static string GetNamespace(string grammarId)
        {
            SourceCodeCharType charType = grammarId[0].GetCharType();
            if (charType == SourceCodeCharType.Letter || charType == SourceCodeCharType.UnderLine)
            {
                return string.Format("{0}Compiler", grammarId);
            }
            else
            {
                return string.Format("_{0}Compiler", grammarId);
            }
        }

    }
}
