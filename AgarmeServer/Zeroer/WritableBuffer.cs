/*
 * Copyright (c) [2020] [Erxl]
 * [Ordinary] is licensed under Mulan PSL v2.
 * You can use this software according to the terms and conditions of the Mulan PSL v2.
 * You may obtain a copy of Mulan PSL v2 at:
 *          http://license.coscl.org.cn/MulanPSL2
 * THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
 * See the Mulan PSL v2 for more details.
 */

using System.Runtime.CompilerServices;

namespace AgarmeServer.Zeroer
{
    /// <summary>
    /// 可根据写入内容自动扩容的内存缓冲区
    /// </summary>
    public unsafe class WritableBuffer : IWritableBuffer
    {
        private MemoryBlock buffer;
        private unint writerOffset, minSize;

        public static readonly unint DefaultMinSize = 32;

        public MemoryBlock Buffer
        {
            get => new MemoryBlock(buffer.Pointer, writerOffset);
        }

        public WritableBuffer(MemoryBlock bufer, unint minSize)
        {
            this.buffer = bufer;
            this.minSize = minSize;
        }

        public WritableBuffer(MemoryBlock bufer) : this(bufer, DefaultMinSize)
        {
        }

        public WritableBuffer(unint minSize)
        {
            buffer.Allocate((unint)minSize);
            this.minSize = minSize;
        }

        public WritableBuffer() : this(DefaultMinSize)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unint WriteUndefined(unint len)
        {
            var bufSize = buffer.Size;
            if (writerOffset + len >= bufSize)
            {
                //不可写入，需要扩展
                var oldLen = bufSize;
                var oldBuf = buffer;
                var newLen = oldLen << 1; //扩展为原来两倍大小
                var newBuf = new MemoryBlock(newLen);
                M.Copy(newBuf.Pointer, oldBuf.Pointer, oldLen);
                //释放旧内存
                oldBuf.Free();
                buffer = newBuf;
            }

            var ret = buffer.Pointer + writerOffset;
            //推进写入偏移
            writerOffset += len;
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            var bufSize = buffer.Size;
            if (minSize + minSize < bufSize) //如果缓冲区比最小的二倍都要大，就让他收缩为原来一半
            {
                buffer.Free();
                buffer.Allocate(bufSize >> 1);
            }

            writerOffset = default;
        }

        public unint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => buffer.Size;
        }

        /// <summary>
        /// 将缓冲区前面的一部分内存释放
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseFront(unint size)
        {
            var newWriterOffset = writerOffset - size;
            var newSize = writerOffset - size;
            if (newSize > minSize + minSize) //如果新缓冲区比最小限制的两倍大，就考虑收缩
            {
                var bufSize = buffer.Size;
                var halfBufSize = bufSize >> 1;
                if (newSize < halfBufSize) //如果新缓冲区比原来一半小，就收缩为原来一半
                {
                    var oldBuf = buffer;
                    buffer.Allocate(halfBufSize); //申请新内存
                    M.Copy(buffer.Pointer, oldBuf.Pointer + size, newWriterOffset); //把
                    oldBuf.Free();
                }
                else
                {
                    //继续沿用旧缓冲区
                    M.SafeCopy(buffer.Pointer, buffer.Pointer + size, newWriterOffset); //把
                }
            }
            else
            {
                //继续沿用旧缓冲区
                M.SafeCopy(buffer.Pointer, buffer.Pointer + size, newWriterOffset); //把
            }

            writerOffset = newWriterOffset;
        }
    }
}