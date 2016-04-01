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

        private static void DumpSyntaxParserCode(
            RegulationList grammar, LRParsingMap map, string grammarId, string LR0Directory,
            SyntaxParserMapAlgorithm algorithm)
        {
            var parserType = new CodeTypeDeclaration(GetParserName(grammarId, algorithm));
            parserType.IsClass = true;
            parserType.BaseTypes.Add(typeof(LRSyntaxParser));
            DumpSyntaxParserFields(grammar, parserType);
            DumpSyntaxParserMethod_GetGrammar(grammar, parserType);
            DumpSyntaxParserMethod_GetParsingMap(grammar, map, parserType);

            var parserNamespace = new CodeNamespace(GetNamespace(grammarId));
            parserNamespace.Imports.Add(new CodeNamespaceImport(typeof(System.Object).Namespace));
            parserNamespace.Imports.Add(new CodeNamespaceImport(typeof(System.Collections.Generic.List<int>).Namespace));
            parserNamespace.Imports.Add(new CodeNamespaceImport(typeof(LALR1Compiler.HashCache).Namespace));
            parserNamespace.Types.Add(parserType);

            //生成代码  
            string parserCodeFullname = Path.Combine(LR0Directory, GetParserName(grammarId, algorithm) + ".cs");
            using (var sw = new StreamWriter(parserCodeFullname, false))
            {
                CSharpCodeProvider codeProvider = new CSharpCodeProvider();
                CodeGeneratorOptions geneOptions = new CodeGeneratorOptions();//代码生成选项
                geneOptions.BlankLinesBetweenMembers = true;
                geneOptions.BracingStyle = "C";
                geneOptions.ElseOnClosing = false;
                geneOptions.IndentString = "    ";
                geneOptions.VerbatimOrder = true;

                codeProvider.GenerateCodeFromNamespace(parserNamespace, sw, geneOptions);
            }
        }

        private static void DumpSyntaxParserMethod_GetParsingMap(
            RegulationList grammar,
            LRParsingMap map,
            CodeTypeDeclaration parserType)
        {
            var method = new CodeMemberMethod();
            method.Name = "GetParsingMap";
            method.Attributes = MemberAttributes.Override | MemberAttributes.Family;
            method.ReturnType = new CodeTypeReference(typeof(LRParsingMap));
            string varName = "map";
            // LALR1Compiler.LRParsingMap map = new LALR1Compiler.LRParsingMap();
            var varDeclaration = new CodeVariableDeclarationStatement(typeof(LRParsingMap), varName);
            varDeclaration.InitExpression = new CodeObjectCreateExpression(typeof(LRParsingMap));
            method.Statements.Add(varDeclaration);
            foreach (var action in map)
            {
                var parametes = new List<CodeExpression>();
                string[] parts = action.Key.Split('+');
                var stateId = new CodePrimitiveExpression(int.Parse(parts[0]));
                var nodeType = new CodeVariableReferenceExpression(
                    GetNodeNameInParser(new TreeNodeType(parts[1], parts[1], parts[1])));
                if (action.Value.Count > 1)
                {
                    // action.Value超过1项，就说明有冲突。列出这些冲突应该是有价值的。
                    method.Comments.Add(new CodeCommentStatement(
                        string.Format("{0} confilicts:", action.Value.Count), false));
                    foreach (var value in action.Value)
                    {
                        string str = string.Format("map.SetAction({0}, {1}, new {2}({3}));",
                            parts[0], GetNodeNameInParser(new TreeNodeType(parts[1], parts[1], parts[1])),
                            value.GetType().Name, value.ActionParam());
                        method.Comments.Add(new CodeCommentStatement(str, false));
                    }
                }
                foreach (var value in action.Value)
                {
                    // map.SetAction(1, NODE__S, new LALR1Compiler.LR1GotoAction(2));
                    Type t = value.GetType();
                    CodeObjectCreateExpression ctor = null;
                    string actionParam = value.ActionParam();
                    if (t == typeof(LR1AceptAction))
                    {
                        ctor = new CodeObjectCreateExpression(value.GetType());
                    }
                    else
                    {
                        ctor = new CodeObjectCreateExpression(t,
                            new CodePrimitiveExpression(int.Parse(actionParam)));
                    }

                    var SetActionMethod = new CodeMethodReferenceExpression(
                        new CodeVariableReferenceExpression(varName), "SetAction");
                    var setAction = new CodeMethodInvokeExpression(
                        SetActionMethod, stateId, nodeType, ctor);
                    method.Statements.Add(setAction);
                }
            }
            {
                // return map;
                var returnMap = new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression(varName));
                method.Statements.Add(returnMap);
            }

            parserType.Members.Add(method);
        }

        private static void DumpSyntaxParserMethod_GetGrammar(RegulationList grammar, CodeTypeDeclaration parserType)
        {
            var method = new CodeMemberMethod();
            method.Name = "GetGrammar";
            method.Attributes = MemberAttributes.Override | MemberAttributes.Family;
            method.ReturnType = new CodeTypeReference(typeof(RegulationList));
            string varName = "grammar";
            {
                // LALR1Compiler.RegulationList grammar = new LALR1Compiler.RegulationList();
                var varDeclaration = new CodeVariableDeclarationStatement(typeof(RegulationList), varName);
                varDeclaration.InitExpression = new CodeObjectCreateExpression(typeof(RegulationList));
                method.Statements.Add(varDeclaration);
            }
            int id = 1;
            foreach (var regulation in grammar)
            {
                // <S> ::= null ;
                // private static LALR1Compiler.TreeNodeType NODE__S = new LALR1Compiler.TreeNodeType("__S", "S", "<S>");
                string str = regulation.Dump();
                var commentStatement = new CodeCommentStatement(new CodeComment(
                    string.Format("{0}: {1}", id++, str), false));
                method.Statements.Add(commentStatement);
                var parametes = new List<CodeExpression>();
                parametes.Add(new CodeVariableReferenceExpression(GetNodeNameInParser(regulation.Left)));
                parametes.AddRange(
                    from item in regulation.RightPart
                    select new CodeVariableReferenceExpression(GetNodeNameInParser(item)));
                var ctor = new CodeObjectCreateExpression(typeof(Regulation), parametes.ToArray());
                var AddMethod = new CodeMethodReferenceExpression(
                    new CodeVariableReferenceExpression(varName), "Add");
                var addRegulation = new CodeMethodInvokeExpression(AddMethod, ctor);
                method.Statements.Add(addRegulation);
            }
            {
                // return grammar;
                var returnGrammar = new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression(varName));
                method.Statements.Add(returnGrammar);
            }

            parserType.Members.Add(method);
        }

        private static void DumpSyntaxParserFields(RegulationList grammar, CodeTypeDeclaration parserType)
        {
            foreach (var node in grammar.GetAllTreeNodeNonLeaveTypes())
            {
                CodeMemberField field = new CodeMemberField(typeof(TreeNodeType), GetNodeNameInParser(node));
                // field.Attributes 不支持readonly，遗憾了。
                field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
                var ctor = new CodeObjectCreateExpression(typeof(TreeNodeType),
                    new CodePrimitiveExpression(node.Type),
                    new CodePrimitiveExpression(node.Content),
                    new CodePrimitiveExpression(node.Nickname));
                field.InitExpression = ctor;
                parserType.Members.Add(field);
            }
            foreach (var node in grammar.GetAllTreeNodeLeaveTypes())
            {
                CodeMemberField field = new CodeMemberField(typeof(TreeNodeType), GetNodeNameInParser(node));
                field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
                var ctor = new CodeObjectCreateExpression(typeof(TreeNodeType),
                    new CodePrimitiveExpression(node.Type),
                    new CodePrimitiveExpression(node.Content),
                    new CodePrimitiveExpression(node.Nickname));
                field.InitExpression = ctor;
                parserType.Members.Add(field);
            }
            {
                var node = TreeNodeType.endOfTokenListNode;
                CodeMemberField field = new CodeMemberField(typeof(TreeNodeType), GetNodeNameInParser(node));
                field.Attributes = MemberAttributes.Private | MemberAttributes.Static;
                var ctor = new CodeObjectCreateExpression(typeof(TreeNodeType),
                    new CodePrimitiveExpression(node.Type),
                    new CodePrimitiveExpression(node.Content),
                    new CodePrimitiveExpression(node.Nickname));
                field.InitExpression = ctor;
                parserType.Members.Add(field);
            }
        }

    }
}
