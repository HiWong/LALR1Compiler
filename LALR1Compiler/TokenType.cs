﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LALR1Compiler
{
    /// <summary>
    /// 单词的类型信息。例如一个“;”，此对象将保存其“;”的表现值和Identifier化的含义（semicolon）。
    /// </summary>
    public class TokenType : HashCache
    {
        public static readonly TokenType endOfTokenList = new TokenType("end_of_token_list", "$", "\"$\"");

        public TokenType(string type, string content, string nickname)
        {
            this.Type = type; this.Content = content; this.Nickname = nickname;
        }

        /// <summary>
        /// 例如：“__semicolon”
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// 例如：“;”
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// 例如：“number”
        /// 对于identifier和number，此值用于保存"identifier"和"number"这样的字符串。
        /// </summary>
        public string Nickname { get; private set; }


        public override void Dump(System.IO.TextWriter stream)
        {
            stream.Write("[{0}]({1})[{2}]", this.Content, this.Type, this.Nickname);
        }
    }
}
