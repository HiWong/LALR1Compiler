﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LALR1Compiler
{
    /// <summary>
    /// 词法分析器的抽象基类。对一个字符串进行词法分析
    /// </summary>
    public abstract partial class LexicalAnalyzer
        : ILexicalAnalyzer
    {

        protected virtual bool GetLetter(Token result, AnalyzingContext context)
        {
            return GetIdentifier(result, context);
        }
        protected virtual bool Getunderline(Token result, AnalyzingContext context)
        {
            return GetIdentifier(result, context);
        }
        /// <summary>
        /// 获取标识符（函数名，变量名，等）
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual bool GetIdentifier(Token result, AnalyzingContext context)
        {
            StringBuilder builder = new StringBuilder();
            while (context.NextLetterIndex < context.SourceCode.Length)
            {
                char ch = context.CurrentChar();
                var ct = ch.GetCharType();
                if (ct == SourceCodeCharType.Letter
                    || ct == SourceCodeCharType.Number
                    || ct == SourceCodeCharType.UnderLine)
                {
                    builder.Append(ch);
                    context.NextLetterIndex++;
                }
                else
                { break; }
            }
            string content = builder.ToString();
            // specify if this string is a keyword
            bool isKeyword = false;
            foreach (var item in this.GetKeywords())
            {
                if (item.NickName == content)
                {
                    result.TokenType = new TokenType(item.TokenType, content, content);
                    isKeyword = true;
                    break;
                }
            }
            if (!isKeyword)
            {
                result.TokenType = new TokenType(
                    "identifier", content, "identifier");
            }

            return true;
        }

    }

}