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
    public unsafe static class WritableBufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<TWritableBuffer, TData>(this TWritableBuffer buffer, TData data)
            where TWritableBuffer : IWritableBuffer where TData : unmanaged
            => buffer.WriteUndefined(sizeof(TData)).Write(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<TWritableBuffer>(this IWritableBuffer buffer, unint dataPtr, unint size)
            where TWritableBuffer : IWritableBuffer
        {
            var dstPtr = buffer.WriteUndefined(size);
            M.Copy(dstPtr, dataPtr, size);
        }
    }
}