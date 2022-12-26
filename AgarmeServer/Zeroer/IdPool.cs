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
using System.Runtime.CompilerServices;

namespace AgarmeServer.Zeroer
{
    /// <summary>
    /// 咋都不会占太多内存的最小id分配器，基于红黑树实现
    /// </summary>
    public class IdPool : IPool<uint>
    {
        RedBlackTreeSet<IdRange> ids = new RedBlackTreeSet<IdRange>();


        private class IdRange : IComparable<IdRange>
        {
            public uint Left, Right;
            public uint Length => Right - Left;

            public int CompareTo(IdRange other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return Left.CompareTo(other.Left);
            }



        }

        class PrivateSingleComparer : ISingleComparer<IBinaryTreeNode<IdRange>>
        {
            private uint id;

            public PrivateSingleComparer(uint id)
            {
                this.id = id;
            }

            public int Compare(IBinaryTreeNode<IdRange> a)
            {
                var x = a.Data;
                if (x.Left > id)
                {
                    return -1;
                }
                else
                {
                    if (x.Right <= id)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Rent()
        {
            var count = ids.Count;
            switch (count)
            {
                case 0: //没有块的情况
                    {
                        var newRange = new IdRange();
                        newRange.Left = 0;
                        newRange.Right = 1;
                        ids.Add(newRange);
                        return 0;
                    }
                case 1: //只有一个块的情况
                    {
                        var range = ids.Min;
                        if (range.Left != 0)
                        {
                            range.Left--;
                            ids.Remove(range);
                            ids.Add(range);
                            return range.Left;
                        }
                        return range.Right++;
                    }
                default: //两个及两个以上的情况
                    {
                        //获取前两个块
                        var enumerator = ids.GetEnumerator();
                        enumerator.MoveNext();
                        var range1 = enumerator.Current;
                        enumerator.MoveNext(); //含有第二个块的情况

                        var range2 = enumerator.Current;

                        //增加第一个块申请的id
                        var result = range1.Right;
                        range1.Right++;
                        if (range1.Right == range2.Left)
                        {
                            //当前id块连接到下一个块，合并块
                            range1.Right = range2.Right;
                            //移除被合并的块
                            ids.Remove(range2);
                        }

                        return result;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(uint id)
        {
            switch (ids.Root.Search<IBinaryTreeNode<IdRange>, IdRange, PrivateSingleComparer>(new PrivateSingleComparer(id),
                out var result))
            {
                case BinaryTreeNodeFindResult.This:
                    {
                        var length = result.Data.Length;
                        if (length == 1) //块仅剩一个id
                        {
                            ids.Remove(result.Data);
                            return;
                        }
                        else //块有两个及两个以上的id
                        if (id == result.Data.Right - 1) //释放的是最后一个id,且该块id总数不为1
                        {
                            result.Data.Right--;
                            return;
                        }
                        else if (id == result.Data.Left) //释放的是第一个id，,且该块id总数不为1
                        {
                            result.Data.Left++;
                            ids.Remove(result.Data); //left改变，重新设置位置
                            ids.Add(result.Data);
                            return;
                        }
                        else //释放的不是最后一个id
                        {
                            //分裂这个快
                            var newRange = new IdRange();
                            newRange.Left = id + 1;
                            newRange.Right = result.Data.Right;
                            ids.Add(newRange);

                            result.Data.Right = id;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}