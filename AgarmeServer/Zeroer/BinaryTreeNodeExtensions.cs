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
    public static class BinaryTreeNodeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BinaryTreeNodeFindResult Search<TNode, TData, TComparer>(this TNode node,
            TComparer comparer, out TNode result)
            where TNode : class, IBinaryTreeNode<TData> where TComparer : ISingleComparer<TNode>
        {
            var compareResult = comparer.Compare(node);
            switch (compareResult)
            {
                case 0:
                    {
                        result = node;
                        return BinaryTreeNodeFindResult.This;
                    }
                case 1:
                    {
                        var right = node.Right as TNode;
                        if (right == null)
                        {
                            result = null;
                            return BinaryTreeNodeFindResult.RightOfThis;
                        }
                        else
                        {
                            return Search<TNode, TData, TComparer>(node.Right as TNode, comparer, out result);
                        }
                    }
                case -1:
                    {
                        var left = node.Left as TNode;
                        if (left == null)
                        {
                            result = null;
                            return BinaryTreeNodeFindResult.LeftOfThis;
                        }
                        else
                        {
                            return Search<TNode, TData, TComparer>(node.Left as TNode, comparer, out result);
                        }
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
            }
        }
    }



}