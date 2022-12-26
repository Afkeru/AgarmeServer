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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AgarmeServer.Zeroer
{
    /// <summary>
    /// 表示一块固定内存，内存的申请和释放全部需要手动
    /// </summary>
    public unsafe struct MemoryBlock : IDisposable, IList<byte>
    {
        public unint Size { get; private set; }
        public unint Pointer { get; private set; }

        public unint EndPointer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Pointer + Size; }
        }

        int ICollection<byte>.Count => (int)Size;

        bool ICollection<byte>.IsReadOnly => false;

        public byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *((byte*)Pointer + index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => *((byte*)Pointer + index) = value;
        }

        public byte this[unint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *((byte*)Pointer + (ulong)index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => *((byte*)Pointer + (ulong)index) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemoryBlock Create(unint beginPtr, unint endPtr)
        {
            return new MemoryBlock(beginPtr, endPtr - beginPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryBlock(unint data, unint size)
        {
            Size = size;
            Pointer = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryBlock(unint size)
        {
            Size = size;
            Pointer = M.Allocate(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Allocate(unint size)
        {
            Size = size;
            Pointer = M.Allocate(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Allocate(int size)
        {
            Size = (unint)size;
            Pointer = M.Allocate(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free()
        {
            M.Free(Pointer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator unint(MemoryBlock a) => a.Pointer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(byte value = 0)
        {
            M.Fill(Pointer, Size, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(byte item)
        {
            for (int i = 0; i < Size; i++)
            {
                if ((Pointer + i).Read<byte>() == item)
                {
                    return i;
                }
            }
            return -1;
        }

        void IList<byte>.Insert(int index, byte item)
        {
            throw new NotSupportedException();
        }

        void IList<byte>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(byte item)
        {
            for (int i = 0; i < Size; i++)
            {
                if ((Pointer + i).Read<byte>() == item)
                {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(byte[] array, int arrayIndex)
        {
            var l = M.Min((int)Size, array.Length);
            for (int i = 0; i < l; i++)
            {
                array[i] = this[i];
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<byte>.Add(byte item)
        {
            throw new NotSupportedException();
        }

        void ICollection<byte>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<byte>.Remove(byte item)
        {
            throw new NotSupportedException();
        }

        private struct Enumerator : IEnumerator<byte>
        {
            public Enumerator(MemoryBlock block)
            {
                this.index = 0;
                this.block = block;
                Current = 0;
            }

            private unint index;
            private MemoryBlock block;
            public byte Current { get; private set; }

            object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (index < block.Size)
                {
                    Current = block[index];
                    index++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                index = 0;
            }
        }
    }
}