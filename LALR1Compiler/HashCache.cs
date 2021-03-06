﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LALR1Compiler
{
    // http://blog.csdn.net/lanlicen/article/details/8913065
    /// <summary>
    /// 缓存一个对象的hash code。提高比较（==、!=、Equals、GetHashCode、Compare）的效率。
    /// </summary>
    [DebuggerDisplay("{this.Dump()}")]
    public abstract class HashCache : IComparable<HashCache>, IDump2Stream
    {
        public static bool operator ==(HashCache left, HashCache right)
        {
            object leftObj = left, rightObj = right;
            if (leftObj == null)
            {
                if (rightObj == null) { return true; }
                else { return false; }
            }
            else
            {
                if (rightObj == null) { return false; }
            }

            return left.Equals(right);
        }

        public static bool operator !=(HashCache left, HashCache right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            HashCache p = obj as HashCache;
            if ((System.Object)p == null)
            {
                return false;
            }

            return this.HashCode == p.HashCode;
        }

        public override int GetHashCode()
        {
            return this.HashCode;
        }

        private Func<HashCache, string> GetUniqueString;

        private bool dirty = true;

        /// <summary>
        /// 指明此cache需要更新才能用。
        /// </summary>
        public void SetDirty() { this.dirty = true; }

        private int hashCode;
        /// <summary>
        /// hash值。
        /// </summary>
        public int HashCode
        {
            get
            {
                if (this.dirty)
                {
                    Update();
 
                    this.dirty = false;
                }

                return this.hashCode;
            }
        }

        private void Update()
        {
            string str = GetUniqueString(this);
            int hashCode = str.GetHashCode();
            this.hashCode = hashCode;
            this.uniqueString = string.Format("[{0}]", hashCode);// release后用最少的内存区分此对象
            // 有DebuggerDisplay，不需要这个了。
            //this.uniqueString = str;// debug时可以看到可读的信息
        }

        /// <summary>
        /// 功能稳定后应精简此字段的内容。
        /// </summary>
        private string uniqueString = string.Empty;

        /// <summary>
        /// 可唯一标识该对象的字符串。
        /// 功能稳定后应精简此字段的内容。
        /// </summary>
        public string UniqueString
        {
            get
            {
                if (this.dirty)
                {
                    Update();

                    this.dirty = false;
                }

                return this.uniqueString;
            }
        }

        private static string getUniqueString(HashCache cache)
        {
            return cache.Dump();
        }

        /// <summary>
        /// 缓存一个对象的hash code。提高比较（==、!=、Equals、GetHashCode、Compare）的效率。
        /// </summary>
        /// <param name="GetUniqueString">获取一个可唯一标识此对象的字符串。</param>
        public HashCache(Func<HashCache, string> GetUniqueString = null)
        {
            if (GetUniqueString == null)
            {
                this.GetUniqueString = getUniqueString;
            }
            else
            {
                this.GetUniqueString = GetUniqueString;
            }
        }

        public override string ToString()
        {
            return this.UniqueString;
        }

        public int CompareTo(HashCache other)
        {
            if (other == null) { return 1; }

            // 如果用this.HashCode - other.HashCode < 0，就会发生溢出，这个bug让我折腾了近8个小时。
            if (this.HashCode < other.HashCode)
            { return -1; }
            else if (this.HashCode == other.HashCode)
            { return 0; }
            else
            { return 1; }
        }

        /// <summary>
        /// 为了尽可能减少占用内存，提供此方法。
        /// 这样，内存中就不用同时存在太多太大的string。
        /// </summary>
        /// <param name="stream"></param>
        public abstract void Dump(System.IO.TextWriter stream);

    }
}
