﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LALR1Compiler
{
    /// <summary>
    /// LL1语法分析器基类
    /// </summary>
    public abstract partial class LRSyntaxParser : ISyntaxParser
    {

        protected abstract LRParsingMap GetParsingMap();

        protected abstract RegulationList GetGrammar();

#if DEBUG
        static bool print = false;
#endif

        public SyntaxTree Parse(TokenList tokenList)
        {
            LRParsingMap parsingMap = GetParsingMap();
            RegulationList grammar = GetGrammar();

            //TODO:这个convertor以后是可以配置的。
            var tokenTypeConvertor = new TokenType2TreeNodeType();
            var context = new ParsingContext(tokenList, grammar, parsingMap, tokenTypeConvertor);
            while (context.CurrentTokenIndex < context.TokenList.Count + 1)
            {
                PrintParsingProgress(context);
                TreeNodeType nodeType = context.CurrentNodeType();
                int stateId = context.StateIdStack.Peek();
                LRParsingAction action = parsingMap.GetAction(stateId, nodeType);
                int currentTokenIndex = action.Execute(context);
                context.CurrentTokenIndex = currentTokenIndex;
            }

            PrintLastState(context);

            if (context.TreeStack.Count > 0)
            {
                return context.TreeStack.Peek();
            }
            else
            {
                return new SyntaxTree();
            }
        }

        private void PrintLastState(ParsingContext context)
        {
#if DEBUG
            if (!print) { return; } // 调试时快速取消print

            Debug.WriteLine("=======================");
            Debug.WriteLine("***********************");
            {
                Debug.WriteLine("Last Stack:");
                Debug.Write("    ");
                var stateIdArray = context.StateIdStack.ToArray();
                var treeArray = context.TreeStack.ToArray();
                for (int i = stateIdArray.Length - 1; i > 0; i--)
                {
                    Debug.Write(stateIdArray[i]);
                    Debug.Write('['); Debug.Write(treeArray[i - 1].NodeType.Nickname); Debug.Write(']');
                }
                Debug.Write(stateIdArray[0]); Debug.Write(' ');
                Debug.WriteLine("");
            }
            {
                Debug.WriteLine("Last token list:");
                Debug.Write("    ");
                for (int i = context.CurrentTokenIndex; i < context.TokenList.Count; i++)
                {
                    Debug.Write(context.TokenList[i].TokenType.Content); Debug.Write(' ');
                }
                Debug.WriteLine("");
            }
            {
                Debug.WriteLine("Last action:");
                TreeNodeType nodeType = context.CurrentNodeType();
                int stateId = context.StateIdStack.Peek();
                LRParsingAction action = context.ParsingMap.GetAction(stateId, nodeType);
                Debug.Write("    "); Debug.WriteLine(action);
            }
            {
                Debug.WriteLine("Last syntax tree:");
                Debug.WriteLine(context.TreeStack.Peek());
                foreach (var item in context.TreeStack)
                {
                    item.Parent = null;
                }
            }
            Debug.WriteLine("***********************");
            Debug.WriteLine("=======================");

#endif
        }

        private void PrintParsingProgress(ParsingContext context)
        {
#if DEBUG
            if (!print) { return; } // 调试时快速取消print

            Debug.WriteLine("=======================");
            {
                Debug.WriteLine("Current Stack:");
                Debug.Write("    ");
                var stateIdArray = context.StateIdStack.ToArray();
                var treeArray = context.TreeStack.ToArray();
                for (int i = stateIdArray.Length - 1; i > 0; i--)
                {
                    Debug.Write(stateIdArray[i]);
                    Debug.Write('['); Debug.Write(treeArray[i - 1].NodeType.Nickname); Debug.Write(']');
                }
                Debug.Write(stateIdArray[0]); Debug.Write(' ');
                Debug.WriteLine("");
            }
            {
                Debug.WriteLine("Current token list:");
                Debug.Write("    ");
                for (int i = context.CurrentTokenIndex; i < context.TokenList.Count; i++)
                {
                    Debug.Write(context.TokenList[i].TokenType.Content); Debug.Write(' ');
                }
                Debug.WriteLine("");
            }
            {
                Debug.WriteLine("Next action:");
                TreeNodeType nodeType = context.CurrentNodeType();
                int stateId = context.StateIdStack.Peek();
                LRParsingAction action = context.ParsingMap.GetAction(stateId, nodeType);
                Debug.Write("    "); Debug.WriteLine(action);
            }
            {
                Debug.WriteLine("Current syntax tree:");
                SyntaxTree virtualParent = new SyntaxTree();
                virtualParent.NodeType = new TreeNodeType("VirtualParent", "", "Virtual Parent");
                var treeArray = context.TreeStack.ToArray();
                for (int i = treeArray.Length - 1; i >= 0; i--)
                {
                    virtualParent.Children.Add(treeArray[i]);
                    treeArray[i].Parent = virtualParent;
                }
                Debug.WriteLine(virtualParent);
                foreach (var item in context.TreeStack)
                {
                    item.Parent = null;
                }
            }
            Debug.WriteLine("=======================");

#endif
        }
    }

    public class ParsingContext
    {
        public int CurrentTokenIndex { get; set; }

        public Stack<int> StateIdStack { get; private set; }

        public Stack<SyntaxTree> TreeStack { get; private set; }

        public TokenList TokenList { get; private set; }

        /// <summary>
        /// 规则列表。等价于文法。
        /// </summary>
        public RegulationList Grammar { get; private set; }

        /// <summary>
        /// LR1分析表。
        /// </summary>
        public LRParsingMap ParsingMap { get; private set; }

        public TokenType2TreeNodeType TokenTypeConvertor { get; private set; }

        // TODO: 以后应该会删掉这个东西
        private static readonly TokenType endOfTokenList = new TokenType("end_of_token_list", "$", "\"$\"");

        public TreeNodeType CurrentNodeType()
        {
            TokenType tokenType;
            if (this.CurrentTokenIndex >= this.TokenList.Count)
            {
                tokenType = endOfTokenList;
            }
            else
            {
                tokenType = this.TokenList[this.CurrentTokenIndex].TokenType;
            }

            return TokenTypeConvertor.GetNodeType(tokenType);
        }

        public ParsingContext(TokenList tokenList,
            RegulationList grammar,
            LRParsingMap parsingMap,
            TokenType2TreeNodeType tokenTypeConvertor)
        {
            this.StateIdStack = new Stack<int>();
            this.StateIdStack.Push(1);
            this.TreeStack = new Stack<SyntaxTree>();

            this.TokenList = tokenList;
            this.Grammar = grammar;
            this.ParsingMap = parsingMap;
            this.TokenTypeConvertor = tokenTypeConvertor;
        }

    }

    /// <summary>
    /// 由单词类型获取对应的V类型
    /// </summary>
    public class TokenType2TreeNodeType
    {
        public virtual TreeNodeType GetNodeType(TokenType tokenType)
        {
            //TODO:“Leave__”后缀，这是个自定义规则
            string strTreeNodeType = tokenType.Type + "Leave__";
            string content = tokenType.Content;
            TreeNodeType result = new TreeNodeType(strTreeNodeType, content, tokenType.Nickname);

            return result;
        }
    }

    public class TreeNodeType2TokenType
    {
        public virtual TokenType GetTokenType(TreeNodeType treeNodeType)
        {
            //TODO:“Leave__”后缀，这是个自定义规则
            string strTokenType = treeNodeType.Type.Substring(0, treeNodeType.Type.Length - "Leave__".Length);
            string content = treeNodeType.Content;
            TokenType result = new TokenType(strTokenType, treeNodeType.Content, treeNodeType.Nickname);

            return result;
        }
    }
}
