using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Hebron.Runtime
{
    public static unsafe class CRuntime
    {
        private static readonly string numbers = "0123456789";

        public static void* malloc(ulong size)
        {
            return malloc((long)size);
        }

        public static void* malloc(long size)
        {
            var ptr = Marshal.AllocHGlobal((int)size);

            MemoryStats.Allocated();

            return ptr.ToPointer();
        }

        public static void free(void* a)
        {
            if (a == null)
                return;

            var ptr = new IntPtr(a);
            Marshal.FreeHGlobal(ptr);
            MemoryStats.Freed();
        }

        public static void memcpy(void* a, void* b, long size)
        {
            var ap = (byte*)a;
            var bp = (byte*)b;
            for (long i = 0; i < size; ++i)
                *ap++ = *bp++;
        }

        public static void memcpy(void* a, void* b, ulong size)
        {
            memcpy(a, b, (long)size);
        }

        public static void memmove(void* a, void* b, long size)
        {
            void* temp = null;

            try
            {
                temp = malloc(size);
                memcpy(temp, b, size);
                memcpy(a, temp, size);
            }

            finally
            {
                if (temp != null)
                    free(temp);
            }
        }

        public static void memmove(void* a, void* b, ulong size)
        {
            memmove(a, b, (long)size);
        }

        public static int memcmp(void* a, sbyte[] b, ulong size)
        {
            fixed(void* bptr = b)
            {
                return memcmp(a, bptr, size);
            }
        }

        public static int memcmp(void* a, void* b, long size)
        {
            var result = 0;
            var ap = (byte*)a;
            var bp = (byte*)b;
            for (long i = 0; i < size; ++i)
            {
                if (*ap != *bp)
                    result += 1;

                ap++;
                bp++;
            }

            return result;
        }

        public static int memcmp(void* a, void* b, ulong size)
        {
            return memcmp(a, b, (long)size);
        }

        public static int memcmp(byte* a, byte[] b, ulong size)
        {
            fixed (void* bptr = b)
            {
                return memcmp(a, bptr, (long)size);
            }
        }

        public static void memset(void* ptr, int value, long size)
        {
            var bptr = (byte*)ptr;
            var bval = (byte)value;
            for (long i = 0; i < size; ++i)
                *bptr++ = bval;
        }

        public static void memset(void* ptr, int value, ulong size)
        {
            memset(ptr, value, (long)size);
        }

        public static uint _lrotl(uint x, int y)
        {
            return (x << y) | (x >> (32 - y));
        }

        public static void* realloc(void* a, long newSize)
        {
            if (a == null)
                return malloc(newSize);

            var ptr = new IntPtr(a);
            var result = Marshal.ReAllocHGlobal(ptr, new IntPtr(newSize));

            return result.ToPointer();
        }

        public static void* realloc(void* a, ulong newSize)
        {
            return realloc(a, (long)newSize);
        }

        public static int abs(int v)
        {
            return Math.Abs(v);
        }

        public static double pow(double a, double b)
        {
            return Math.Pow(a, b);
        }

        public static void SetArray<T>(T[] data, T value)
        {
            for (var i = 0; i < data.Length; ++i)
                data[i] = value;
        }

        public static double ldexp(double number, int exponent)
        {
            return number * Math.Pow(2, exponent);
        }

        /// <summary>
        /// 把 str1 所指向的字符串和 token 所指向的字符串进行比较
        /// </summary>
        /// <param name="src"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static int strcmp(sbyte* src, sbyte* token)
        {
            var l = strlen(token);
            return strncmp(src, new Span<sbyte>(token, (int)l), (ulong)l);
        }

        /// <summary>
        /// <see cref="CRuntime.strcmp(sbyte*, sbyte*)"/>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static int strcmp(sbyte* src, sbyte[] token)
        {
            return strncmp(src, token, (ulong)token.Length);
        }

        public static int strcmp(sbyte* src, string token)
        {
            var array = System.Text.Encoding.UTF8.GetBytes(token + '\0');
            fixed (byte* p = array)
            {
                return strcmp(src, (sbyte*)p);
            }
        }

        /// <summary>
        /// <see cref="CRuntime.strncmp(sbyte*, Span{sbyte}, ulong)"/>
        /// </summary>
        public static int strncmp(sbyte* src, sbyte[] token, ulong size)
        {
            return strncmp(src, new Span<sbyte>(token), size);
        }

        /// <summary>
        /// Compare no more than N characters of S1 and S2,
        /// returning less than, equal to or greater than zero
        /// if S1 is lexicographically less than, equal to or
        /// greater than S2
        /// </summary>
        /// <param name="src"></param>
        /// <param name="token"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        static int strncmp(sbyte* src, Span<sbyte> token, ulong size)
        {
            for (var i = 0; i < Math.Min(token.Length, (int)size); ++i)
            {
                if (src[i] != token[i])
                {
                    if (src[i] < token[i])
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        public static long strtol(sbyte* start, sbyte** end, int radix)
        {
            // First step - determine length
            var length = 0;
            sbyte* ptr = start;
            while (numbers.IndexOf((char)*ptr) != -1)
            {
                ++ptr;
                ++length;
            }

            long result = 0;

            // Now build up the number
            ptr = start;
            while (length > 0)
            {
                long num = numbers.IndexOf((char)*ptr);
                long pow = (long)Math.Pow(10, length - 1);
                result += num * pow;

                ++ptr;
                --length;
            }

            if (end != null)
            {
                *end = ptr;
            }

            return result;
        }

        /// <summary>
        /// 判断以\0结尾的字符串长度。
        /// <see href="https://github.com/gcc-mirror/gcc/blob/releases/gcc-14.2.0/libsanitizer/interception/interception_win.cpp"/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static ulong strlen(sbyte* str)
        {
            sbyte* p = str;
            while (*p != (sbyte)'\0') ++p;
            return (ulong)(p - str);
        }

        public static int strlen(sbyte[] str)
        {
            fixed (sbyte* p = str)
            {
                return (int)strlen(p);
            }
        }

        /// <summary>
        /// Convert a string to an int.
        /// </summary>
        /// <param name="nptr"></param>
        /// <returns></returns>
		/// <remarks>
		/// <see href="https://github.com/bminor/glibc/blob/glibc-2.40/stdlib/atoi.c">atoi.c</see> 
		/// </remarks>
        public static int atoi(sbyte* nptr)
        {
            return (int)strtol(nptr, null, 10);
        }

        /// <summary>
        /// Convert a string to a long long int.
		/// <see href="https://github.com/bminor/glibc/blob/glibc-2.40/stdlib/atoll.c">atoll.c</see>
        /// </summary>
        /// <returns></returns>
        public static long atoll(sbyte* nptr)
        {
            return strtol(nptr, null, 10);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <remarks>
        /// <see href="https://github.com/bminor/glibc/blob/glibc-2.40/string/strncpy.c">strncpy.c</see>
		/// </remarks>
        public static sbyte* strncpy(sbyte* dest, sbyte* src, int n)
        {
            return strncpy(dest, src, (ulong)n);
        }

        public static sbyte* strncpy(sbyte* dest, sbyte* src, ulong n)
        {
            memcpy(dest, src, n);
            return dest;
        }

        /// <summary>
        /// Convert a string to a float.
        /// </summary>
        /// <returns></returns>
        public static float atof(string str)
        {
            return float.Parse(str);
        }
        
        public static float atof(ReadOnlySpan<byte> nptr)
        {
            return float.Parse(nptr);
        }
        
        public static float atof(sbyte* nptr)
        {
            var sp = new ReadOnlySpan<byte>(nptr, (int)strlen(nptr));
            return atof(sp);
        }

        /// <summary>
        /// 如果在字符串 str 中找到字符 c，则函数返回指向该字符的指针，如果未找到该字符则返回 NULL.
        /// <see href=" https://github.com/openbsd/src/blob/master/lib/libc/string/strchr.c">strchr.c</see>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static sbyte* strchr(sbyte* p, int ch)
        {
            for (; ; ++p)
            {
                if (*p == (sbyte)ch)
                    return ((sbyte*)p);
                if (*p == (sbyte)'\0')
                    return ((sbyte*)null);
            }
        }

        static sbyte* strchr(sbyte[] s, int ch)
        {
            fixed(sbyte* p = s)
            {
                return strchr(p, ch);
            }
        }

        /// <summary>
        /// 该函数返回 str1 开头连续都不含字符串 str2 中字符的字符数。
        /// <see href="https://github.com/managarm/mlibc/blob/5.0.0/options/ansi/generic/string-stubs.cpp">string-stubs.cpp</see>
        /// </summary>
        public static int strcspn(sbyte* s, sbyte[] chrs)
        {
            var n = 0;
            while (true)
            {
                if (s[n] == (sbyte)'\0' //到结尾
                    || strchr(chrs, s[n]) != null) //找到
                    return n;
                n++;
            }
        }

        /// <summary>
        /// 从字符串的末尾开始向前搜索，直到找到指定的字符或搜索完整个字符串。如果找到字符，它将返回一个指向该字符的指针，否则返回 NULL.
        /// <see href="https://github.com/openbsd/src/blob/master/lib/libc/string/strrchr.c">strrchr.c</see>
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static sbyte* strrchr(sbyte* p, int ch)
        {
            sbyte* save;

            for (save = null; ; ++p)
            {
                if (*p == (sbyte)ch)
                    save = (sbyte*)p;
                if (*p == (sbyte)'\0')
                    return (save);
            }
        }

        /// <summary>
        /// 把 from 所指向的字符串复制到 to
        /// <see href="https://github.com/openbsd/src/blob/master/lib/libc/string/strcpy.c">strcpy.c</see>
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static sbyte* strcpy(sbyte* to, sbyte* from)
        {
            sbyte* save = to;

            for (; (*to = *from) != (sbyte)'\0'; ++from, ++to);
            return (save);
        }

        public static sbyte* strcpy(sbyte* to, string from)
        {
            var array = System.Text.Encoding.UTF8.GetBytes(from + '\0');
            fixed (byte* p = array)
            {
                return strcpy(to, (sbyte*)p);
            }
        }

        /// <summary>
        /// 返回指向 s 中第一次出现 pattern 的位置的指针。如果未找到，则返回 NULL.
        /// <see href="https://github.com/managarm/mlibc/blob/5.0.0/options/ansi/generic/string-stubs.cpp">string-stubs.cpp</see>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static sbyte* strstr(sbyte* s, sbyte[] pattern)
        {
            for (var i = 0; s[i] != (sbyte)'\0'; i++)
            {
                bool found = true;
                for (var j = 0; pattern[j] != (sbyte)'\0'; j++)
                {
                    if (pattern[j] == (sbyte)'\0' || s[i + j] == pattern[j])
                        continue;

                    found = false;
                    break;
                }

                if (found)
                    return &s[i];
            }

            return null;
        }

        public static void assert(int value)
        {
            Debug.Assert(value != 0);
        }
    }
}