/*
 * Copyright (c) [2020] [Erxl]
 * [Ordinary] is licensed under Mulan PSL v2.
 * You can use this software according to the terms and conditions of the Mulan PSL v2.
 * You may obtain a copy of Mulan PSL v2 at:
 *          http://license.coscl.org.cn/MulanPSL2
 * THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
 * See the Mulan PSL v2 for more details.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AgarmeServer.Zeroer
{
    public unsafe static class M
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(unint dst, unint src, unint len)
        {
            var mem1 = (byte*)dst;
            var mem2 = (byte*)src;
            var i = (ulong)len;
            while (i-- != 0)
            {
                if (*(mem1 + i) != *(mem2 + i))
                    return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(void* dst, void* src, ulong len)
        {
            return Compare((unint)dst, (unint)src, (unint)len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(unint dst, unint src, unint len)
        {
            var i = (ulong)len;
            var d = (byte*)dst;
            var s = (byte*)src;
            while (i-- != 0)
            {
                *(d + i) = *(s + i);
            }
        }

        /// <summary>
        /// 针对源区域和目的区域重叠的情况进行的复制操作，此操作与<see cref="M.Copy"/>性能几乎没有差别
        /// </summary>
        public static void SafeCopy(unint dst, unint src, unint len)
        {
            var l = (ulong)len;
            var d = (byte*)dst;
            var s = (byte*)src;
            if (dst > src)
            {
                while (l-- != 0)
                {
                    *(d + l) = *(s + l);
                }
            }
            else
            {
                var i = 0ul;
                while (i != l)
                {
                    *(d + i) = *(s + i);
                    i++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(void* dst, void* src, ulong len)
        {
            Copy((unint)dst, (unint)src, (unint)len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(IntPtr dst, IntPtr src, IntPtr len)
        {
            Copy((unint)dst, (unint)src, (unint)len);
        }

        /// <summary>
        /// 短距离位移
        /// </summary>
        /// <param name="src">源地址</param>
        /// <param name="count">源长度，表示源由count*4个字节组成</param>
        /// <param name="dst">目的地址</param>
        /// <param name="distance">位移位数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftLeftShort(uint* src, int count, uint* dst, int distance)
        {
            var i = count;
            while (i-- != 0)
            {
                *(dst + i) = *(src + i) << distance;
            }

            i = count - 1;
            var d = 32 - distance;
            while (i-- != 0)
            {
                *(dst + i + 1) |= *(src + i) >> d;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftLeftShort(ulong* src, int count, ulong* dst, int distance)
        {
            var i = count;
            while (i-- != 0)
            {
                *(dst + i) = *(src + i) << distance;
            }

            i = count - 1;
            var d = 64 - distance;
            while (i-- != 0)
            {
                *(dst + i + 1) |= *(src + i) >> d;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftRightShort(uint* src, int count, uint* dst, int distance)
        {
            var i = count;
            while (i-- != 0)
            {
                *(dst + i) = *(src + i) >> distance;
            }

            i = count - 1;
            var d = 32 - distance;
            while (i-- != 0)
            {
                *(dst + i) |= *(src + i + 1) << d;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftRightShort(ulong* src, int count, ulong* dst, int distance)
        {
            var i = count;
            while (i-- != 0)
            {
                *(dst + i) = *(src + i) >> distance;
            }

            i = count - 1;
            var d = 64 - distance;
            while (i-- != 0)
            {
                *(dst + i) |= *(src + i + 1) << d;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Min(uint a, uint b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Min(short a, short b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Min(ushort a, ushort b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Min(byte a, byte b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Min(sbyte a, sbyte b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double a, double b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Min(decimal a, decimal b) => a > b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int a, int b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Max(uint a, uint b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Max(short a, short b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Max(ushort a, ushort b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Max(byte a, byte b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Max(sbyte a, sbyte b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double a, double b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Max(decimal a, decimal b) => a < b ? b : a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(this IEnumerable<T> enumerable, T item)
        {
            int i = 0;
            foreach (var a in enumerable)
            {
                if (Equals(item, a))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(this IReadOnlyList<T> list, T item)
        {
            var length = list.Count;
            for (int i = 0; i < length; i++)
            {
                if (list[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, out T result)
        {
            int i = 0;
            foreach (var a in enumerable)
            {
                if (predicate(a))
                {
                    result = a;
                    return i;
                }

                i++;
            }

            result = default;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllTrue(this bool[,] array)
        {
            var l1 = array.GetUpperBound(0) + 1;
            var l2 = array.GetUpperBound(1) + 1;
            for (var i = 0; i < l1; i++)
            {
                for (int j = 0; j < l2; j++)
                {
                    if (!array[i, j]) //任何一个为False都不行
                    {
                        return false;
                    }
                }
            }

            //此处可以确保所有成员都为True
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyTrue(this bool[,] array)
        {
            var l1 = array.GetUpperBound(0) + 1;
            var l2 = array.GetUpperBound(1) + 1;
            for (var i = 0; i < l1; i++)
            {
                for (int j = 0; j < l2; j++)
                {
                    if (array[i, j]) //任何一个为true都行
                    {
                        return true;
                    }
                }
            }

            //此处可以确保所有成员都为false
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(this T[] array, T value)
        {
            var len = array.Length;
            for (var i = 0; i < len; i++)
            {
                array[i] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill<T>(this T[,] array, T value)
        {
            var l1 = array.GetUpperBound(0) + 1;
            var l2 = array.GetUpperBound(1) + 1;
            for (var i = 0; i < l1; i++)
            {
                for (int j = 0; j < l2; j++)
                {
                    array[i, j] = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllTrue<T>(this T list) where T : IEnumerable<bool>
        {
            foreach (var item in list)
            {
                if (!item) //任何一个为False都不行
                {
                    return false;
                }
            }

            //此处可以确保所有成员都为True
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyTrue<T>(this T list) where T : IEnumerable<bool>
        {
            foreach (var item in list)
            {
                if (item) //任何一个为true都行
                {
                    return true;
                }
            }

            //此处可以确保所有成员都为false
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllTrueList<TList>(this TList list) where TList : IReadOnlyList<bool>
        {
            var l = list.Count;
            for (var i = 0; i < l; i++)
            {
                if (!list[i]) //任何一个为False都不行
                {
                    return false;
                }
            }

            //此处可以确保所有成员都为True
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyTrueList<TList>(this TList list) where TList : IReadOnlyList<bool>
        {
            var l = list.Count;
            for (var i = 0; i < l; i++)
            {
                if (list[i]) //任何一个为true都行
                {
                    return true;
                }
            }

            //此处可以确保所有成员都为false
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyFalseList<TList>(this TList list) where TList : IReadOnlyList<bool> => !list.AllTrueList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllFalseList<TList>(this TList list) where TList : IReadOnlyList<bool> => !list.AnyTrueList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyFalse<TEnumerable>(this TEnumerable list) where TEnumerable : IEnumerable<bool> =>
            !list.AllTrue();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllFalse<TEnumerable>(this TEnumerable list) where TEnumerable : IEnumerable<bool> =>
            !list.AnyTrue();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unint Allocate(int size)
        {
            return (unint)Marshal.AllocHGlobal(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Allocate<T>(unint count) where T : unmanaged
        {
            return (T*)Allocate((unint)((ulong)count * (ulong)sizeof(T)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unint Allocate(unint size)
        {
            return (unint)Marshal.AllocHGlobal((IntPtr)size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(this unint data)
        {
            Marshal.FreeHGlobal((IntPtr)data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unint ReAllocate(this unint data, unint size)
        {
            return (unint)Marshal.ReAllocHGlobal((IntPtr)data, (IntPtr)size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log2(int value)
        {
            int num = 0;
            for (; value > 0; value >>= 1)
                ++num;
            return num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref uint a, ref uint b)
        {
            a = a ^ b;
            b = a ^ b;
            a = a ^ b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref int a, ref int b)
        {
            a = a ^ b;
            b = a ^ b;
            a = a ^ b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref long a, ref long b)
        {
            a = a ^ b;
            b = a ^ b;
            a = a ^ b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref ulong a, ref ulong b)
        {
            a = a ^ b;
            b = a ^ b;
            a = a ^ b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sqr(int x)
        {
            return x * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Sqr(uint x)
        {
            return x * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Sqr(long x)
        {
            return x * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Sqr(ulong x)
        {
            return x * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForeachList<TItem>(this IReadOnlyList<TItem> list, Action<TItem> action)
        {
            var l = list.Count;
            for (int i = 0; i < l; i++)
            {
                action(list[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Foreach<TItem>(this IEnumerable<TItem> enumerable, Action<TItem> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TItem>(this ICollection<TItem> items, Action<TItem> action)
        {
            items.Add(action);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TItem>(this ICollection<TItem> collection, IEnumerable<TItem> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fill(unint ptr, unint len, byte value = 0)
        {
            var i = (ulong)len;
            var p = (byte*)ptr;
            while (i-- != 0)
            {
                *(p + i) = value;
            }
        }
    }
}