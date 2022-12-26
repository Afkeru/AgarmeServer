//归零编写，类C提供思路
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace AgarmeServer.Zeroer
{
    /// <summary>
    /// 量子id分配器，感谢类c提供的思路，此id分配器用于在固定区间分配id
    /// </summary>
    public class FixedFastIdPool : IPool<uint>
    {
        private uint[] ids;
        public uint Count { get; private set; }

        public IReadOnlyList<uint> Used
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ArraySegment<uint>(ids, 0, (int)Count);
        }

        public IReadOnlyList<uint> Unused
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new ArraySegment<uint>(ids, (int)Count, (int)(ids.Length - Count));
        }

        public unsafe FixedFastIdPool(uint bufferSize)
        {
            ids = new uint[bufferSize];
            var i = bufferSize;
            fixed (uint* p = ids)
            {
                while (i-- != 0)
                {
                    //设置数组初始值
                    p[i] = i;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanRent() => Count >= ids.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Rent()
        {
            return ids[Count++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(uint id)
        {
            ids[--Count] = id;
        }
    }
}
