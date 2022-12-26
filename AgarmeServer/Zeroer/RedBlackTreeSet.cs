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
using System.Runtime.Serialization;

#pragma warning disable

namespace AgarmeServer.Zeroer
{
    public class RedBlackTreeSet<T> : ISet<T>, ICollection<T>, IEnumerable<T>, IEnumerable, ICollection,
        IReadOnlyCollection<T>, ISerializable, IDeserializationCallback
    {
        private RedBlackTreeSet<T>.Node root;
        public IBinaryTreeNode<T> Root => root;
        private IComparer<T> comparer;
        private int count;
        private int version;
        private SerializationInfo siInfo;

        internal static class EnumerableHelpers
        {
            internal static T[] ToArray<T>(IEnumerable<T> source, out int length)
            {
                if (source is ICollection<T> objs)
                {
                    int count = objs.Count;
                    if (count != 0)
                    {
                        T[] array = new T[count];
                        objs.CopyTo(array, 0);
                        length = count;
                        return array;
                    }
                }
                else
                {
                    using (IEnumerator<T> enumerator = source.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            T[] array = new T[4]
                            {
                                enumerator.Current,
                                default,
                                default,
                                default
                            };
                            int num = 1;
                            while (enumerator.MoveNext())
                            {
                                if (num == array.Length)
                                {
                                    int newSize = num << 1;
                                    if ((uint)newSize > 2146435071U)
                                        newSize = 2146435071 <= num ? num + 1 : 2146435071;
                                    Array.Resize<T>(ref array, newSize);
                                }

                                array[num++] = enumerator.Current;
                            }

                            length = num;
                            return array;
                        }
                    }
                }

                length = 0;
                return Array.Empty<T>();
            }
        }

        public RedBlackTreeSet()
        {
            this.comparer = (IComparer<T>)System.Collections.Generic.Comparer<T>.Default;
        }

        public RedBlackTreeSet(IComparer<T> comparer)
        {
            this.comparer = comparer ?? (IComparer<T>)System.Collections.Generic.Comparer<T>.Default;
        }

        public RedBlackTreeSet(IEnumerable<T> collection)
            : this(collection, (IComparer<T>)System.Collections.Generic.Comparer<T>.Default)
        {
        }

        public RedBlackTreeSet(IEnumerable<T> collection, IComparer<T> comparer)
            : this(comparer)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection is RedBlackTreeSet<T> other && !(other is RedBlackTreeSet<T>.TreeSubSet) &&
                this.HasEqualComparer(other))
            {
                if (other.Count <= 0)
                    return;
                this.count = other.count;
                this.root = other.root.DeepClone(this.count);
            }
            else
            {
                int length;
                T[] array = EnumerableHelpers.ToArray<T>(collection, out length);
                if (length <= 0)
                    return;
                comparer = this.comparer;
                Array.Sort<T>(array, 0, length, comparer);
                int num1 = 1;
                for (int index = 1; index < length; ++index)
                {
                    if (comparer.Compare(array[index], array[index - 1]) != 0)
                        array[num1++] = array[index];
                }

                int num2 = num1;
                this.root = RedBlackTreeSet<T>.ConstructRootFromSortedArray(array, 0, num2 - 1,
                    (RedBlackTreeSet<T>.Node)null);
                this.count = num2;
            }
        }

        protected RedBlackTreeSet(SerializationInfo info, StreamingContext context)
        {
            this.siInfo = info;
        }

        private void AddAllElements(IEnumerable<T> collection)
        {
            foreach (T obj in collection)
            {
                if (!this.Contains(obj))
                    this.Add(obj);
            }
        }

        private void RemoveAllElements(IEnumerable<T> collection)
        {
            T min = this.Min;
            T max = this.Max;
            foreach (T x in collection)
            {
                if (this.comparer.Compare(x, min) >= 0 && this.comparer.Compare(x, max) <= 0 && this.Contains(x))
                    this.Remove(x);
            }
        }

        private bool ContainsAllElements(IEnumerable<T> collection)
        {
            foreach (T obj in collection)
            {
                if (!this.Contains(obj))
                    return false;
            }

            return true;
        }

        internal delegate bool TreeWalkPredicate<T>(RedBlackTreeSet<T>.Node node);

        internal virtual bool InOrderTreeWalk(TreeWalkPredicate<T> action)
        {
            if (this.root == null)
                return true;
            Stack<RedBlackTreeSet<T>.Node> nodeStack =
                new Stack<RedBlackTreeSet<T>.Node>(2 * RedBlackTreeSet<T>.Log2(this.Count + 1));
            for (RedBlackTreeSet<T>.Node node = this.root; node != null; node = node.Left)
                nodeStack.Push(node);
            while (nodeStack.Count != 0)
            {
                RedBlackTreeSet<T>.Node node1 = nodeStack.Pop();
                if (!action(node1))
                    return false;
                for (RedBlackTreeSet<T>.Node node2 = node1.Right; node2 != null; node2 = node2.Left)
                    nodeStack.Push(node2);
            }

            return true;
        }

        internal virtual bool BreadthFirstTreeWalk(TreeWalkPredicate<T> action)
        {
            if (this.root == null)
                return true;
            Queue<RedBlackTreeSet<T>.Node> nodeQueue = new Queue<RedBlackTreeSet<T>.Node>();
            nodeQueue.Enqueue(this.root);
            while (nodeQueue.Count != 0)
            {
                RedBlackTreeSet<T>.Node node = nodeQueue.Dequeue();
                if (!action(node))
                    return false;
                if (node.Left != null)
                    nodeQueue.Enqueue(node.Left);
                if (node.Right != null)
                    nodeQueue.Enqueue(node.Right);
            }

            return true;
        }

        public int Count
        {
            get
            {
                this.VersionCheck(true);
                return this.count;
            }
        }

        public IComparer<T> Comparer
        {
            get { return this.comparer; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return (object)this; }
        }

        internal virtual void VersionCheck(bool updateCount = false)
        {
        }

        internal virtual int TotalCount()
        {
            return this.Count;
        }

        internal virtual bool IsWithinRange(T item)
        {
            return true;
        }

        public bool Add(T item)
        {
            return this.AddIfNotPresent(item);
        }

        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        internal enum NodeColor : byte
        {
            Black,
            Red,
        }

        internal virtual bool AddIfNotPresent(T item)
        {
            if (this.root == null)
            {
                this.root = new RedBlackTreeSet<T>.Node(item, NodeColor.Black);
                this.count = 1;
                ++this.version;
                return true;
            }

            RedBlackTreeSet<T>.Node current1 = this.root;
            RedBlackTreeSet<T>.Node parent = (RedBlackTreeSet<T>.Node)null;
            RedBlackTreeSet<T>.Node grandParent = (RedBlackTreeSet<T>.Node)null;
            RedBlackTreeSet<T>.Node greatGrandParent = (RedBlackTreeSet<T>.Node)null;
            ++this.version;
            int num = 0;
            for (; current1 != null; current1 = num < 0 ? current1.Left : current1.Right)
            {
                num = this.comparer.Compare(item, current1.Item);
                if (num == 0)
                {
                    this.root.ColorBlack();
                    return false;
                }

                if (current1.Is4Node)
                {
                    current1.Split4Node();
                    if (RedBlackTreeSet<T>.Node.IsNonNullRed(parent))
                        this.InsertionBalance(current1, ref parent, grandParent, greatGrandParent);
                }

                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current1;
            }

            RedBlackTreeSet<T>.Node current2 = new RedBlackTreeSet<T>.Node(item, NodeColor.Red);
            if (num > 0)
                parent.Right = current2;
            else
                parent.Left = current2;
            if (parent.IsRed)
                this.InsertionBalance(current2, ref parent, grandParent, greatGrandParent);
            this.root.ColorBlack();
            ++this.count;
            return true;
        }

        public bool Remove(T item)
        {
            return this.DoRemove(item);
        }

        internal virtual bool DoRemove(T item)
        {
            if (this.root == null)
                return false;
            ++this.version;
            RedBlackTreeSet<T>.Node node1 = this.root;
            RedBlackTreeSet<T>.Node node2 = (RedBlackTreeSet<T>.Node)null;
            RedBlackTreeSet<T>.Node node3 = (RedBlackTreeSet<T>.Node)null;
            RedBlackTreeSet<T>.Node match = (RedBlackTreeSet<T>.Node)null;
            RedBlackTreeSet<T>.Node parentOfMatch = (RedBlackTreeSet<T>.Node)null;
            bool flag = false;
            int num;
            for (; node1 != null; node1 = num < 0 ? node1.Left : node1.Right)
            {
                if (node1.Is2Node)
                {
                    if (node2 == null)
                    {
                        node1.ColorRed();
                    }
                    else
                    {
                        RedBlackTreeSet<T>.Node sibling = node2.GetSibling(node1);
                        if (sibling.IsRed)
                        {
                            if (node2.Right == sibling)
                                node2.RotateLeft();
                            else
                                node2.RotateRight();
                            node2.ColorRed();
                            sibling.ColorBlack();
                            this.ReplaceChildOrRoot(node3, node2, sibling);
                            node3 = sibling;
                            if (node2 == match)
                                parentOfMatch = sibling;
                            sibling = node2.GetSibling(node1);
                        }

                        if (sibling.Is2Node)
                        {
                            node2.Merge2Nodes();
                        }
                        else
                        {
                            RedBlackTreeSet<T>.Node newChild = node2.Rotate(node2.GetRotation(node1, sibling));
                            newChild.Color = node2.Color;
                            node2.ColorBlack();
                            node1.ColorRed();
                            this.ReplaceChildOrRoot(node3, node2, newChild);
                            if (node2 == match)
                                parentOfMatch = newChild;
                        }
                    }
                }

                num = flag ? -1 : this.comparer.Compare(item, node1.Item);
                if (num == 0)
                {
                    flag = true;
                    match = node1;
                    parentOfMatch = node2;
                }

                node3 = node2;
                node2 = node1;
            }

            if (match != null)
            {
                this.ReplaceNode(match, parentOfMatch, node2, node3);
                --this.count;
            }

            this.root?.ColorBlack();
            return flag;
        }

        public virtual void Clear()
        {
            this.root = (RedBlackTreeSet<T>.Node)null;
            this.count = 0;
            ++this.version;
        }

        public virtual bool Contains(T item)
        {
            return this.FindNode(item) != null;
        }

        public void CopyTo(T[] array)
        {
            this.CopyTo(array, 0, this.Count);
        }

        public void CopyTo(T[] array, int index)
        {
            this.CopyTo(array, index, this.Count);
        }

        public void CopyTo(T[] array, int index, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), (object)index,
                    "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (count > array.Length - index)
                throw new ArgumentException("SR.Arg_ArrayPlusOffTooSmall");
            count += index;
            this.InOrderTreeWalk((TreeWalkPredicate<T>)(node =>
           {
               if (index >= count)
                   return false;
               array[index++] = node.Item;
               return true;
           }));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("SR.Arg_RankMultiDimNotSupported", nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("SR.Arg_NonZeroLowerBound", nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), (object)index,
                    "SR.ArgumentOutOfRange_NeedNonNegNum");
            if (array.Length - index < this.Count)
                throw new ArgumentException("SR.Arg_ArrayPlusOffTooSmall");
            if (array is T[] array1)
            {
                this.CopyTo(array1, index);
            }
            else
            {
                object[] objects = array as object[];
                if (objects == null)
                    throw new ArgumentException("SR.Argument_InvalidArrayType", nameof(array));
                try
                {
                    this.InOrderTreeWalk((TreeWalkPredicate<T>)(node =>
                   {
                       objects[index++] = (object)node.Item;
                       return true;
                   }));
                }
                catch (ArrayTypeMismatchException ex)
                {
                    throw new ArgumentException("SR.Argument_InvalidArrayType", nameof(array));
                }
            }
        }

        public RedBlackTreeSet<

            T>.Enumerator GetEnumerator()
        {
            return new RedBlackTreeSet<T>.Enumerator(this);
        }



        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return (IEnumerator<T>)this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }

        private void InsertionBalance(
            RedBlackTreeSet<T>.Node current,
            ref RedBlackTreeSet<T>.Node parent,
            RedBlackTreeSet<T>.Node grandParent,
            RedBlackTreeSet<T>.Node greatGrandParent)
        {
            bool flag1 = grandParent.Right == parent;
            bool flag2 = parent.Right == current;
            RedBlackTreeSet<T>.Node newChild;
            if (flag1 == flag2)
            {
                newChild = flag2 ? grandParent.RotateLeft() : grandParent.RotateRight();
            }
            else
            {
                newChild = flag2 ? grandParent.RotateLeftRight() : grandParent.RotateRightLeft();
                parent = greatGrandParent;
            }

            grandParent.ColorRed();
            newChild.ColorBlack();
            this.ReplaceChildOrRoot(greatGrandParent, grandParent, newChild);
        }

        private void ReplaceChildOrRoot(
            RedBlackTreeSet<T>.Node parent,
            RedBlackTreeSet<T>.Node child,
            RedBlackTreeSet<T>.Node newChild)
        {
            if (parent != null)
                parent.ReplaceChild(child, newChild);
            else
                this.root = newChild;
        }

        private void ReplaceNode(
            RedBlackTreeSet<T>.Node match,
            RedBlackTreeSet<T>.Node parentOfMatch,
            RedBlackTreeSet<T>.Node successor,
            RedBlackTreeSet<T>.Node parentOfSuccessor)
        {
            if (successor == match)
            {
                successor = match.Left;
            }
            else
            {
                successor.Right?.ColorBlack();
                if (parentOfSuccessor != match)
                {
                    parentOfSuccessor.Left = successor.Right;
                    successor.Right = match.Right;
                }

                successor.Left = match.Left;
            }

            if (successor != null)
                successor.Color = match.Color;
            this.ReplaceChildOrRoot(parentOfMatch, match, successor);
        }

        internal virtual RedBlackTreeSet<T>.Node FindNode(T item)
        {
            int num;
            for (RedBlackTreeSet<T>.Node node = this.root; node != null; node = num < 0 ? node.Left : node.Right)
            {
                num = this.comparer.Compare(item, node.Item);
                if (num == 0)
                    return node;
            }

            return (RedBlackTreeSet<T>.Node)null;
        }

        internal virtual int InternalIndexOf(T item)
        {
            RedBlackTreeSet<T>.Node node = this.root;
            int num1 = 0;
            while (node != null)
            {
                int num2 = this.comparer.Compare(item, node.Item);
                if (num2 == 0)
                    return num1;
                node = num2 < 0 ? node.Left : node.Right;
                num1 = num2 < 0 ? 2 * num1 + 1 : 2 * num1 + 2;
            }

            return -1;
        }

        internal RedBlackTreeSet<T>.Node FindRange(
             T from,
             T to,
            bool lowerBoundActive,
            bool upperBoundActive)
        {
            RedBlackTreeSet<T>.Node node = this.root;
            while (node != null)
            {
                if (lowerBoundActive && this.comparer.Compare(from, node.Item) > 0)
                {
                    node = node.Right;
                }
                else
                {
                    if (!upperBoundActive || this.comparer.Compare(to, node.Item) >= 0)
                        return node;
                    node = node.Left;
                }
            }

            return (RedBlackTreeSet<T>.Node)null;
        }

        internal void UpdateVersion()
        {
            ++this.version;
        }

        public static IEqualityComparer<RedBlackTreeSet<T>> CreateSetComparer()
        {
            return RedBlackTreeSet<T>.CreateSetComparer((IEqualityComparer<T>)null);
        }

        internal sealed class SortedSetEqualityComparer<T> : IEqualityComparer<RedBlackTreeSet<T>>
        {
            private readonly IComparer<T> _comparer;
            private readonly IEqualityComparer<T> _memberEqualityComparer;

            public SortedSetEqualityComparer(IEqualityComparer<T> memberEqualityComparer)
                : this((IComparer<T>)null, memberEqualityComparer)
            {
            }

            private SortedSetEqualityComparer(
                IComparer<T> comparer,
                IEqualityComparer<T> memberEqualityComparer)
            {
                this._comparer = comparer ?? (IComparer<T>)Comparer<T>.Default;
                this._memberEqualityComparer =
                    memberEqualityComparer ?? (IEqualityComparer<T>)EqualityComparer<T>.Default;
            }

            public bool Equals(RedBlackTreeSet<T> x, RedBlackTreeSet<T> y)
            {
                return RedBlackTreeSet<T>.SortedSetEquals(x, y, this._comparer);
            }

            public int GetHashCode(RedBlackTreeSet<T> obj)
            {
                int num = 0;
                if (obj != null)
                {
                    foreach (T obj1 in obj)
                    {
                        if ((object)obj1 != null)
                            num ^= this._memberEqualityComparer.GetHashCode(obj1) & int.MaxValue;
                    }
                }

                return num;
            }

            public override bool Equals(object obj)
            {
                return obj is SortedSetEqualityComparer<T> equalityComparer &&
                       this._comparer == equalityComparer._comparer;
            }

            public override int GetHashCode()
            {
                return this._comparer.GetHashCode() ^ this._memberEqualityComparer.GetHashCode();
            }
        }

        public static IEqualityComparer<RedBlackTreeSet<T>> CreateSetComparer(
            IEqualityComparer<T> memberEqualityComparer)
        {
            return (IEqualityComparer<RedBlackTreeSet<T>>)new SortedSetEqualityComparer<T>(memberEqualityComparer);
        }

        internal static bool SortedSetEquals(
            RedBlackTreeSet<T> set1,
            RedBlackTreeSet<T> set2,
            IComparer<T> comparer)
        {
            if (set1 == null)
                return set2 == null;
            if (set2 == null)
                return false;
            if (set1.HasEqualComparer(set2))
                return set1.Count == set2.Count && set1.SetEquals((IEnumerable<T>)set2);
            foreach (T x in set1)
            {
                bool flag = false;
                foreach (T y in set2)
                {
                    if (comparer.Compare(x, y) == 0)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                    return false;
            }

            return true;
        }

        private bool HasEqualComparer(RedBlackTreeSet<T> other)
        {
            return this.Comparer == other.Comparer || this.Comparer.Equals((object)other.Comparer);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            RedBlackTreeSet<T> other1 = other as RedBlackTreeSet<T>;
            var treeSubSet = this as RedBlackTreeSet<T>.TreeSubSet;
            if (treeSubSet != null)
                this.VersionCheck(false);
            if (other1 != null && treeSubSet == null && this.Count == 0)
            {
                RedBlackTreeSet<T> sortedSet = new RedBlackTreeSet<T>((IEnumerable<T>)other1, this.comparer);
                this.root = sortedSet.root;
                this.count = sortedSet.count;
                ++this.version;
            }
            else if (other1 != null && treeSubSet == null &&
                     (this.HasEqualComparer(other1) && other1.Count > this.Count / 2))
            {
                T[] arr = new T[other1.Count + this.Count];
                int num1 = 0;
                RedBlackTreeSet<T>.Enumerator enumerator1 = this.GetEnumerator();
                RedBlackTreeSet<T>.Enumerator enumerator2 = other1.GetEnumerator();
                bool flag1 = !enumerator1.MoveNext();
                bool flag2 = !enumerator2.MoveNext();
                while (!flag1 && !flag2)
                {
                    int num2 = this.Comparer.Compare(enumerator1.Current, enumerator2.Current);
                    if (num2 < 0)
                    {
                        arr[num1++] = enumerator1.Current;
                        flag1 = !enumerator1.MoveNext();
                    }
                    else if (num2 == 0)
                    {
                        arr[num1++] = enumerator2.Current;
                        flag1 = !enumerator1.MoveNext();
                        flag2 = !enumerator2.MoveNext();
                    }
                    else
                    {
                        arr[num1++] = enumerator2.Current;
                        flag2 = !enumerator2.MoveNext();
                    }
                }

                if (!flag1 || !flag2)
                {
                    RedBlackTreeSet<T>.Enumerator enumerator3 = flag1 ? enumerator2 : enumerator1;
                    do
                    {
                        arr[num1++] = enumerator3.Current;
                    } while (enumerator3.MoveNext());
                }

                this.root = (RedBlackTreeSet<T>.Node)null;
                this.root = RedBlackTreeSet<T>.ConstructRootFromSortedArray(arr, 0, num1 - 1,
                    (RedBlackTreeSet<T>.Node)null);
                this.count = num1;
                ++this.version;
            }
            else
                this.AddAllElements(other);
        }

        private static RedBlackTreeSet<T>.Node ConstructRootFromSortedArray(
            T[] arr,
            int startIndex,
            int endIndex,
            RedBlackTreeSet<T>.Node redNode)
        {
            int num = endIndex - startIndex + 1;
            RedBlackTreeSet<T>.Node node;
            switch (num)
            {
                case 0:
                    return (RedBlackTreeSet<T>.Node)null;

                case 1:
                    node = new RedBlackTreeSet<T>.Node(arr[startIndex], NodeColor.Black);
                    if (redNode != null)
                    {
                        node.Left = redNode;
                        break;
                    }

                    break;

                case 2:
                    node = new RedBlackTreeSet<T>.Node(arr[startIndex], NodeColor.Black);
                    node.Right = new RedBlackTreeSet<T>.Node(arr[endIndex], NodeColor.Black);
                    node.Right.ColorRed();
                    if (redNode != null)
                    {
                        node.Left = redNode;
                        break;
                    }

                    break;

                case 3:
                    node = new RedBlackTreeSet<T>.Node(arr[startIndex + 1], NodeColor.Black);
                    node.Left = new RedBlackTreeSet<T>.Node(arr[startIndex], NodeColor.Black);
                    node.Right = new RedBlackTreeSet<T>.Node(arr[endIndex], NodeColor.Black);
                    if (redNode != null)
                    {
                        node.Left.Left = redNode;
                        break;
                    }

                    break;

                default:
                    int index = (startIndex + endIndex) / 2;
                    node = new RedBlackTreeSet<T>.Node(arr[index], NodeColor.Black);
                    node.Left = RedBlackTreeSet<T>.ConstructRootFromSortedArray(arr, startIndex, index - 1, redNode);
                    node.Right = num % 2 == 0
                        ? RedBlackTreeSet<T>.ConstructRootFromSortedArray(arr, index + 2, endIndex,
                            new RedBlackTreeSet<T>.Node(arr[index + 1], NodeColor.Red))
                        : RedBlackTreeSet<T>.ConstructRootFromSortedArray(arr, index + 1, endIndex,
                            (RedBlackTreeSet<T>.Node)null);
                    break;
            }

            return node;
        }

        public virtual void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.Count == 0 || other == this)
                return;
            RedBlackTreeSet<T> other1 = other as RedBlackTreeSet<T>;

            var treeSubSet = this as RedBlackTreeSet<T>.TreeSubSet;
            if (treeSubSet != null)
                this.VersionCheck(false);
            if (other1 != null && treeSubSet == null && this.HasEqualComparer(other1))
            {
                T[] arr = new T[this.Count];
                int num1 = 0;
                RedBlackTreeSet<T>.Enumerator enumerator1 = this.GetEnumerator();
                RedBlackTreeSet<T>.Enumerator enumerator2 = other1.GetEnumerator();
                bool flag1 = !enumerator1.MoveNext();
                bool flag2 = !enumerator2.MoveNext();
                T max = this.Max;
                while (!flag1 && !flag2 && this.Comparer.Compare(enumerator2.Current, max) <= 0)
                {
                    int num2 = this.Comparer.Compare(enumerator1.Current, enumerator2.Current);
                    if (num2 < 0)
                        flag1 = !enumerator1.MoveNext();
                    else if (num2 == 0)
                    {
                        arr[num1++] = enumerator2.Current;
                        flag1 = !enumerator1.MoveNext();
                        flag2 = !enumerator2.MoveNext();
                    }
                    else
                        flag2 = !enumerator2.MoveNext();
                }

                this.root = (RedBlackTreeSet<T>.Node)null;
                this.root = RedBlackTreeSet<T>.ConstructRootFromSortedArray(arr, 0, num1 - 1,
                    (RedBlackTreeSet<T>.Node)null);
                this.count = num1;
                ++this.version;
            }
            else
                this.IntersectWithEnumerable(other);
        }

        internal virtual void IntersectWithEnumerable(IEnumerable<T> other)
        {
            List<T> objList = new List<T>(this.Count);
            foreach (T obj in other)
            {
                if (this.Contains(obj))
                    objList.Add(obj);
            }

            this.Clear();
            foreach (T obj in objList)
                this.Add(obj);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.count == 0)
                return;
            if (other == this)
                this.Clear();
            else if (other is RedBlackTreeSet<T> other1 && this.HasEqualComparer(other1))
            {
                if (this.comparer.Compare(other1.Max, this.Min) < 0 || this.comparer.Compare(other1.Min, this.Max) > 0)
                    return;
                T min = this.Min;
                T max = this.Max;
                foreach (T x in other)
                {
                    if (this.comparer.Compare(x, min) >= 0)
                    {
                        if (this.comparer.Compare(x, max) > 0)
                            break;
                        this.Remove(x);
                    }
                }
            }
            else
                this.RemoveAllElements(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.Count == 0)
                this.UnionWith(other);
            else if (other == this)
                this.Clear();
            else if (other is RedBlackTreeSet<T> other1 && this.HasEqualComparer(other1))
            {
                this.SymmetricExceptWithSameComparer(other1);
            }
            else
            {
                int length;
                T[] array = EnumerableHelpers.ToArray<T>(other, out length);
                Array.Sort<T>(array, 0, length, this.Comparer);
                this.SymmetricExceptWithSameComparer(array, length);
            }
        }

        private void SymmetricExceptWithSameComparer(RedBlackTreeSet<T> other)
        {
            foreach (T obj in other)
            {
                bool flag = this.Contains(obj) ? this.Remove(obj) : this.Add(obj);
            }
        }

        private void SymmetricExceptWithSameComparer(T[] other, int count)
        {
            if (count == 0)
                return;
            T y = other[0];
            for (int index = 0; index < count; ++index)
            {
                while (index < count && index != 0 && this.comparer.Compare(other[index], y) == 0)
                    ++index;
                if (index >= count)
                    break;
                T obj = other[index];
                bool flag = this.Contains(obj) ? this.Remove(obj) : this.Add(obj);
                y = obj;
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.Count == 0)
                return true;
            if (other is RedBlackTreeSet<T> sortedSet && this.HasEqualComparer(sortedSet))
                return this.Count <= sortedSet.Count && this.IsSubsetOfSortedSetWithSameComparer(sortedSet);
            RedBlackTreeSet<T>.ElementCount elementCount = this.CheckUniqueAndUnfoundElements(other, false);
            return elementCount.UniqueCount == this.Count && elementCount.UnfoundCount >= 0;
        }

        private bool IsSubsetOfSortedSetWithSameComparer(RedBlackTreeSet<T> asSorted)
        {
            RedBlackTreeSet<T> viewBetween = asSorted.GetViewBetween(this.Min, this.Max);
            foreach (T obj in this)
            {
                if (!viewBetween.Contains(obj))
                    return false;
            }

            return true;
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            switch (other)
            {
                case null:
                    throw new ArgumentNullException(nameof(other));
                case ICollection collection when this.Count == 0:
                    return collection.Count > 0;

                case RedBlackTreeSet<T> sortedSet when this.HasEqualComparer(sortedSet):
                    return this.Count < sortedSet.Count && this.IsSubsetOfSortedSetWithSameComparer(sortedSet);

                default:
                    RedBlackTreeSet<T>.ElementCount elementCount = this.CheckUniqueAndUnfoundElements(other, false);
                    return elementCount.UniqueCount == this.Count && elementCount.UnfoundCount > 0;
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            switch (other)
            {
                case null:
                    throw new ArgumentNullException(nameof(other));
                case ICollection collection when collection.Count == 0:
                    return true;

                case RedBlackTreeSet<T> other1 when this.HasEqualComparer(other1):
                    if (this.Count < other1.Count)
                        return false;
                    RedBlackTreeSet<T> viewBetween = this.GetViewBetween(other1.Min, other1.Max);
                    foreach (T obj in other1)
                    {
                        if (!viewBetween.Contains(obj))
                            return false;
                    }

                    return true;

                default:
                    return this.ContainsAllElements(other);
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.Count == 0)
                return false;
            switch (other)
            {
                case ICollection collection when collection.Count == 0:
                    return true;

                case RedBlackTreeSet<T> other1 when this.HasEqualComparer(other1):
                    if (other1.Count >= this.Count)
                        return false;
                    RedBlackTreeSet<T> viewBetween = this.GetViewBetween(other1.Min, other1.Max);
                    foreach (T obj in other1)
                    {
                        if (!viewBetween.Contains(obj))
                            return false;
                    }

                    return true;

                default:
                    RedBlackTreeSet<T>.ElementCount elementCount = this.CheckUniqueAndUnfoundElements(other, true);
                    return elementCount.UniqueCount < this.Count && elementCount.UnfoundCount == 0;
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (other is RedBlackTreeSet<T> other1 && this.HasEqualComparer(other1))
            {
                RedBlackTreeSet<T>.Enumerator enumerator1 = this.GetEnumerator();
                RedBlackTreeSet<T>.Enumerator enumerator2 = other1.GetEnumerator();
                bool flag1 = !enumerator1.MoveNext();
                bool flag2;
                for (flag2 = !enumerator2.MoveNext(); !flag1 && !flag2; flag2 = !enumerator2.MoveNext())
                {
                    if (this.Comparer.Compare(enumerator1.Current, enumerator2.Current) != 0)
                        return false;
                    flag1 = !enumerator1.MoveNext();
                }

                return flag1 & flag2;
            }

            RedBlackTreeSet<T>.ElementCount elementCount = this.CheckUniqueAndUnfoundElements(other, true);
            return elementCount.UniqueCount == this.Count && elementCount.UnfoundCount == 0;
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (this.Count == 0 || other is ICollection<T> objs && objs.Count == 0 ||
                other is RedBlackTreeSet<T> other1 && this.HasEqualComparer(other1) &&
                (this.comparer.Compare(this.Min, other1.Max) > 0 || this.comparer.Compare(this.Max, other1.Min) < 0))
                return false;
            foreach (T obj in other)
            {
                if (this.Contains(obj))
                    return true;
            }

            return false;
        }

        internal ref struct BitHelper
        {
            private readonly Span<int> _span;

            internal BitHelper(Span<int> span, bool clear)
            {
                if (clear)
                    span.Clear();
                this._span = span;
            }

            internal void MarkBit(int bitPosition)
            {
                int index = bitPosition / 32;
                if ((uint)index >= (uint)this._span.Length)
                    return;
                this._span[index] |= 1 << bitPosition % 32;
            }

            internal bool IsMarked(int bitPosition)
            {
                int index = bitPosition / 32;
                return (uint)index < (uint)this._span.Length &&
                       (uint)(this._span[index] & 1 << bitPosition % 32) > 0U;
            }

            internal static int ToIntArrayLength(int n)
            {
                return n <= 0 ? 0 : (n - 1) / 32 + 1;
            }
        }

        private unsafe RedBlackTreeSet<T>.ElementCount CheckUniqueAndUnfoundElements(
            IEnumerable<T> other,
            bool returnIfUnfound)
        {
            if (this.Count == 0)
            {
                int num = 0;
                using (IEnumerator<T> enumerator = other.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        T current = enumerator.Current;
                        ++num;
                    }
                }

                RedBlackTreeSet<T>.ElementCount elementCount;
                elementCount.UniqueCount = 0;
                elementCount.UnfoundCount = num;
                return elementCount;
            }

            int intArrayLength = BitHelper.ToIntArrayLength(this.Count);
            // ISSUE: untyped stack allocation
            var a = stackalloc byte[400];
            Span<int> span = new Span<int>(a, 100);
            BitHelper bitHelper = intArrayLength <= 100
                ? new BitHelper(span.Slice(0, intArrayLength), true)
                : new BitHelper((Span<int>)new int[intArrayLength], false);
            int num1 = 0;
            int num2 = 0;
            foreach (T obj in other)
            {
                int bitPosition = this.InternalIndexOf(obj);
                if (bitPosition >= 0)
                {
                    if (!bitHelper.IsMarked(bitPosition))
                    {
                        bitHelper.MarkBit(bitPosition);
                        ++num2;
                    }
                }
                else
                {
                    ++num1;
                    if (returnIfUnfound)
                        break;
                }
            }

            RedBlackTreeSet<T>.ElementCount elementCount1;
            elementCount1.UniqueCount = num2;
            elementCount1.UnfoundCount = num1;
            return elementCount1;
        }

        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            List<T> matches = new List<T>(this.Count);
            this.BreadthFirstTreeWalk((TreeWalkPredicate<T>)(n =>
           {
               if (match(n.Item))
                   matches.Add(n.Item);
               return true;
           }));
            int num = 0;
            for (int index = matches.Count - 1; index >= 0; --index)
            {
                if (this.Remove(matches[index]))
                    ++num;
            }

            return num;
        }

        public T Min
        {
            get { return this.MinInternal; }
        }

        internal virtual T MinInternal
        {

            get
            {
                if (this.root == null)
                    return default(T);
                RedBlackTreeSet<T>.Node node = this.root;
                while (node.Left != null)
                    node = node.Left;
                return node.Item;
            }
        }

        public T Max
        {

            get { return this.MaxInternal; }
        }

        internal virtual T MaxInternal
        {

            get
            {
                if (this.root == null)
                    return default(T);
                RedBlackTreeSet<T>.Node node = this.root;
                while (node.Right != null)
                    node = node.Right;
                return node.Item;
            }
        }

        public IEnumerable<T> Reverse()
        {
            RedBlackTreeSet<T>.Enumerator e = new RedBlackTreeSet<T>.Enumerator(this, true);
            while (e.MoveNext())
                yield return e.Current;
        }

        public virtual RedBlackTreeSet<T> GetViewBetween(T lowerValue, T upperValue)
        {
            if (this.Comparer.Compare(lowerValue, upperValue) > 0)
                throw new ArgumentException("SR.SortedSet_LowerValueGreaterThanUpperValue", nameof(lowerValue));
            return (RedBlackTreeSet<T>)new RedBlackTreeSet<T>.TreeSubSet(this, lowerValue, upperValue, true, true);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            this.GetObjectData(info, context);
        }

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue("Count", this.count);
            info.AddValue("Comparer", (object)this.comparer, typeof(IComparer<T>));
            info.AddValue("Version", this.version);
            if (this.root == null)
                return;
            T[] array = new T[this.Count];
            this.CopyTo(array, 0);
            info.AddValue("Items", (object)array, typeof(T[]));
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            this.OnDeserialization(sender);
        }

        protected virtual void OnDeserialization(object sender)
        {
            if (this.comparer != null)
                return;
            if (this.siInfo == null)
                throw new SerializationException("SR.Serialization_InvalidOnDeser");
            this.comparer = (IComparer<T>)this.siInfo.GetValue("Comparer", typeof(IComparer<T>));
            int int32 = this.siInfo.GetInt32("Count");
            if (int32 != 0)
            {
                T[] objArray = (T[])this.siInfo.GetValue("Items", typeof(T[]));
                if (objArray == null)
                    throw new SerializationException("SR.Serialization_MissingValues");
                for (int index = 0; index < objArray.Length; ++index)
                    this.Add(objArray[index]);
            }

            this.version = this.siInfo.GetInt32("Version");
            if (this.count != int32)
                throw new SerializationException("SR.Serialization_MismatchedCount");
            this.siInfo = (SerializationInfo)null;
        }

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            RedBlackTreeSet<T>.Node node = this.FindNode(equalValue);
            if (node != null)
            {
                actualValue = node.Item;
                return true;
            }

            actualValue = default(T);
            return false;
        }

        private static int Log2(int value)
        {
            int num = 0;
            for (; value > 0; value >>= 1)
                ++num;
            return num;
        }

        internal sealed class Node : IBinaryTreeNode<T>
        {
            private T item;

            public Node(T item, NodeColor color)
            {
                this.Item = item;
                this.Color = color;
            }

            public static bool IsNonNullRed(RedBlackTreeSet<T>.Node node)
            {
                return node != null && node.IsRed;
            }

            public static bool IsNullOrBlack(RedBlackTreeSet<T>.Node node)
            {
                return node == null || node.IsBlack;
            }

            public T Item
            {
                get => item;
                set => item = value;
            }

            IBinaryTreeNode<T> IBinaryTreeNode<T>.Right => Right;
            IBinaryTreeNode<T> IBinaryTreeNode<T>.Left => Left;
            public RedBlackTreeSet<T>.Node Left { get; set; }

            public RedBlackTreeSet<T>.Node Right { get; set; }

            public NodeColor Color { get; set; }

            public bool IsBlack
            {
                get { return this.Color == NodeColor.Black; }
            }

            public bool IsRed
            {
                get { return this.Color == NodeColor.Red; }
            }

            public bool Is2Node
            {
                get
                {
                    return this.IsBlack && RedBlackTreeSet<T>.Node.IsNullOrBlack(this.Left) &&
                           RedBlackTreeSet<T>.Node.IsNullOrBlack(this.Right);
                }
            }

            public bool Is4Node
            {
                get
                {
                    return RedBlackTreeSet<T>.Node.IsNonNullRed(this.Left) &&
                           RedBlackTreeSet<T>.Node.IsNonNullRed(this.Right);
                }
            }

            public void ColorBlack()
            {
                this.Color = NodeColor.Black;
            }

            public void ColorRed()
            {
                this.Color = NodeColor.Red;
            }

            public RedBlackTreeSet<T>.Node DeepClone(int count)
            {
                Stack<RedBlackTreeSet<T>.Node> nodeStack1 =
                    new Stack<RedBlackTreeSet<T>.Node>(2 * RedBlackTreeSet<T>.Log2(count) + 2);
                Stack<RedBlackTreeSet<T>.Node> nodeStack2 =
                    new Stack<RedBlackTreeSet<T>.Node>(2 * RedBlackTreeSet<T>.Log2(count) + 2);
                RedBlackTreeSet<T>.Node node1 = this.ShallowClone();
                RedBlackTreeSet<T>.Node node2 = this;
                RedBlackTreeSet<T>.Node node3 = node1;
                while (node2 != null)
                {
                    nodeStack1.Push(node2);
                    nodeStack2.Push(node3);
                    node3.Left = node2.Left?.ShallowClone();
                    node2 = node2.Left;
                    node3 = node3.Left;
                }

                while (nodeStack1.Count != 0)
                {
                    RedBlackTreeSet<T>.Node node4 = nodeStack1.Pop();
                    RedBlackTreeSet<T>.Node node5 = nodeStack2.Pop();
                    RedBlackTreeSet<T>.Node node6 = node4.Right;
                    RedBlackTreeSet<T>.Node node7 = node6?.ShallowClone();
                    node5.Right = node7;
                    while (node6 != null)
                    {
                        nodeStack1.Push(node6);
                        nodeStack2.Push(node7);
                        node7.Left = node6.Left?.ShallowClone();
                        node6 = node6.Left;
                        node7 = node7.Left;
                    }
                }

                return node1;
            }

            public TreeRotation GetRotation(
                RedBlackTreeSet<T>.Node current,
                RedBlackTreeSet<T>.Node sibling)
            {
                bool flag = this.Left == current;
                return !RedBlackTreeSet<T>.Node.IsNonNullRed(sibling.Left)
                    ? (!flag ? TreeRotation.LeftRight : TreeRotation.Left)
                    : (!flag ? TreeRotation.Right : TreeRotation.RightLeft);
            }

            public RedBlackTreeSet<T>.Node GetSibling(RedBlackTreeSet<T>.Node node)
            {
                return node != this.Left ? this.Left : this.Right;
            }

            public RedBlackTreeSet<T>.Node ShallowClone()
            {
                return new RedBlackTreeSet<T>.Node(this.Item, this.Color);
            }

            public void Split4Node()
            {
                this.ColorRed();
                this.Left.ColorBlack();
                this.Right.ColorBlack();
            }

            public RedBlackTreeSet<T>.Node Rotate(TreeRotation rotation)
            {
                switch (rotation)
                {
                    case TreeRotation.Left:
                        this.Right.Right.ColorBlack();
                        return this.RotateLeft();

                    case TreeRotation.LeftRight:
                        return this.RotateLeftRight();

                    case TreeRotation.Right:
                        this.Left.Left.ColorBlack();
                        return this.RotateRight();

                    case TreeRotation.RightLeft:
                        return this.RotateRightLeft();

                    default:
                        return (RedBlackTreeSet<T>.Node)null;
                }
            }

            public RedBlackTreeSet<T>.Node RotateLeft()
            {
                RedBlackTreeSet<T>.Node right = this.Right;
                this.Right = right.Left;
                right.Left = this;
                return right;
            }

            public RedBlackTreeSet<T>.Node RotateLeftRight()
            {
                RedBlackTreeSet<T>.Node left = this.Left;
                RedBlackTreeSet<T>.Node right = left.Right;
                this.Left = right.Right;
                right.Right = this;
                left.Right = right.Left;
                right.Left = left;
                return right;
            }

            public RedBlackTreeSet<T>.Node RotateRight()
            {
                RedBlackTreeSet<T>.Node left = this.Left;
                this.Left = left.Right;
                left.Right = this;
                return left;
            }

            public RedBlackTreeSet<T>.Node RotateRightLeft()
            {
                RedBlackTreeSet<T>.Node right = this.Right;
                RedBlackTreeSet<T>.Node left = right.Left;
                this.Right = left.Left;
                left.Left = this;
                right.Left = left.Right;
                left.Right = right;
                return left;
            }

            public void Merge2Nodes()
            {
                this.ColorBlack();
                this.Left.ColorRed();
                this.Right.ColorRed();
            }

            public void ReplaceChild(RedBlackTreeSet<T>.Node child, RedBlackTreeSet<T>.Node newChild)
            {
                if (this.Left == child)
                    this.Left = newChild;
                else
                    this.Right = newChild;
            }

            public ref T Data => ref item;

            public IBinaryTreeNode<T> Parent { get; }
        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator, ISerializable, IDeserializationCallback
        {
            private readonly

                RedBlackTreeSet<T> _tree;

            private readonly int _version;
            private readonly Stack<RedBlackTreeSet<T>.Node> _stack;
            private RedBlackTreeSet<T>.Node _current;
            private readonly bool _reverse;

            internal Enumerator(RedBlackTreeSet<T> set)
                : this(set, false)
            {
            }

            internal Enumerator(RedBlackTreeSet<T> set, bool reverse)
            {
                this._tree = set;
                set.VersionCheck(false);
                this._version = set.version;
                this._stack = new Stack<RedBlackTreeSet<T>.Node>(2 * RedBlackTreeSet<T>.Log2(set.TotalCount() + 1));
                this._current = (RedBlackTreeSet<T>.Node)null;
                this._reverse = reverse;
                this.Initialize();
            }

            void ISerializable.GetObjectData(
                SerializationInfo info,
                StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                throw new PlatformNotSupportedException();
            }

            private void Initialize()
            {
                this._current = (RedBlackTreeSet<T>.Node)null;
                RedBlackTreeSet<T>.Node node1 = this._tree.root;
                while (node1 != null)
                {
                    RedBlackTreeSet<T>.Node node2 = this._reverse ? node1.Right : node1.Left;
                    RedBlackTreeSet<T>.Node node3 = this._reverse ? node1.Left : node1.Right;
                    if (this._tree.IsWithinRange(node1.Item))
                    {
                        this._stack.Push(node1);
                        node1 = node2;
                    }
                    else
                        node1 = node2 == null || !this._tree.IsWithinRange(node2.Item) ? node3 : node2;
                }
            }

            public bool MoveNext()
            {
                this._tree.VersionCheck(false);
                if (this._version != this._tree.version)
                    throw new InvalidOperationException("SR.InvalidOperation_EnumFailedVersion");
                if (this._stack.Count == 0)
                {
                    this._current = (RedBlackTreeSet<T>.Node)null;
                    return false;
                }

                this._current = this._stack.Pop();
                RedBlackTreeSet<T>.Node node1 = this._reverse ? this._current.Left : this._current.Right;
                while (node1 != null)
                {
                    RedBlackTreeSet<T>.Node node2 = this._reverse ? node1.Right : node1.Left;
                    RedBlackTreeSet<T>.Node node3 = this._reverse ? node1.Left : node1.Right;
                    if (this._tree.IsWithinRange(node1.Item))
                    {
                        this._stack.Push(node1);
                        node1 = node2;
                    }
                    else
                        node1 = node3 == null || !this._tree.IsWithinRange(node3.Item) ? node2 : node3;
                }

                return true;
            }

            public void Dispose()
            {
            }

            public
                T Current
            {
                get { return this._current != null ? this._current.Item : default(T); }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (this._current == null)
                        throw new InvalidOperationException("SR.InvalidOperation_EnumOpCantHappen");
                    return (object)this._current.Item;
                }
            }

            internal bool NotStartedOrEnded
            {
                get { return this._current == null; }
            }

            internal void Reset()
            {
                if (this._version != this._tree.version)
                    throw new InvalidOperationException("SR.InvalidOperation_EnumFailedVersion");
                this._stack.Clear();
                this.Initialize();
            }

            void IEnumerator.Reset()
            {
                this.Reset();
            }
        }

        internal struct ElementCount
        {
            internal int UniqueCount;
            internal int UnfoundCount;
        }

        internal sealed class TreeSubSet : RedBlackTreeSet<T>, ISerializable, IDeserializationCallback
        {
            private readonly RedBlackTreeSet<T> _underlying;
            private readonly T _min;
            private readonly T _max;
            private int _countVersion;
            private readonly bool _lBoundActive;
            private readonly bool _uBoundActive;

            public TreeSubSet(
                RedBlackTreeSet<T> Underlying,
                 T Min,
                 T Max,
                bool lowerBoundActive,
                bool upperBoundActive)
                : base(Underlying.Comparer)
            {
                this._underlying = Underlying;
                this._min = Min;
                this._max = Max;
                this._lBoundActive = lowerBoundActive;
                this._uBoundActive = upperBoundActive;
                this.root = this._underlying.FindRange(this._min, this._max, this._lBoundActive, this._uBoundActive);
                this.count = 0;
                this.version = -1;
                this._countVersion = -1;
            }

            internal override bool AddIfNotPresent(T item)
            {
                if (!this.IsWithinRange(item))
                    throw new ArgumentOutOfRangeException(nameof(item));
                bool flag = this._underlying.AddIfNotPresent(item);
                this.VersionCheck(false);
                return flag;
            }

            public override bool Contains(T item)
            {
                this.VersionCheck(false);
                return base.Contains(item);
            }

            internal override bool DoRemove(T item)
            {
                if (!this.IsWithinRange(item))
                    return false;
                bool flag = this._underlying.Remove(item);
                this.VersionCheck(false);
                return flag;
            }

            public override void Clear()
            {
                if (this.Count == 0)
                    return;
                List<T> toRemove = new List<T>();
                this.BreadthFirstTreeWalk((TreeWalkPredicate<T>)(n =>
               {
                   toRemove.Add(n.Item);
                   return true;
               }));
                while (toRemove.Count != 0)
                {
                    this._underlying.Remove(toRemove[toRemove.Count - 1]);
                    toRemove.RemoveAt(toRemove.Count - 1);
                }

                this.root = (RedBlackTreeSet<T>.Node)null;
                this.count = 0;
                this.version = this._underlying.version;
            }

            internal override bool IsWithinRange(T item)
            {
                return (this._lBoundActive ? this.Comparer.Compare(this._min, item) : -1) <= 0 &&
                       (this._uBoundActive ? this.Comparer.Compare(this._max, item) : 1) >= 0;
            }

            internal override T MinInternal
            {
                get
                {
                    RedBlackTreeSet<T>.Node node = this.root;
                    T obj = default(T);
                    while (node != null)
                    {
                        int num = this._lBoundActive ? this.Comparer.Compare(this._min, node.Item) : -1;
                        if (num == 1)
                        {
                            node = node.Right;
                        }
                        else
                        {
                            obj = node.Item;
                            if (num != 0)
                                node = node.Left;
                            else
                                break;
                        }
                    }

                    return obj;
                }
            }

            internal override T MaxInternal
            {
                get
                {
                    RedBlackTreeSet<T>.Node node = this.root;
                    T obj = default(T);
                    while (node != null)
                    {
                        int num = this._uBoundActive ? this.Comparer.Compare(this._max, node.Item) : 1;
                        if (num == -1)
                        {
                            node = node.Left;
                        }
                        else
                        {
                            obj = node.Item;
                            if (num != 0)
                                node = node.Right;
                            else
                                break;
                        }
                    }

                    return obj;
                }
            }

            internal override bool InOrderTreeWalk(TreeWalkPredicate<T> action)
            {
                this.VersionCheck(false);
                if (this.root == null)
                    return true;
                Stack<RedBlackTreeSet<T>.Node> nodeStack =
                    new Stack<RedBlackTreeSet<T>.Node>(2 * RedBlackTreeSet<T>.Log2(this.count + 1));
                RedBlackTreeSet<T>.Node node1 = this.root;
                while (node1 != null)
                {
                    if (this.IsWithinRange(node1.Item))
                    {
                        nodeStack.Push(node1);
                        node1 = node1.Left;
                    }
                    else
                        node1 = !this._lBoundActive || this.Comparer.Compare(this._min, node1.Item) <= 0
                            ? node1.Left
                            : node1.Right;
                }

                while (nodeStack.Count != 0)
                {
                    RedBlackTreeSet<T>.Node node2 = nodeStack.Pop();
                    if (!action(node2))
                        return false;
                    RedBlackTreeSet<T>.Node node3 = node2.Right;
                    while (node3 != null)
                    {
                        if (this.IsWithinRange(node3.Item))
                        {
                            nodeStack.Push(node3);
                            node3 = node3.Left;
                        }
                        else
                            node3 = !this._lBoundActive || this.Comparer.Compare(this._min, node3.Item) <= 0
                                ? node3.Left
                                : node3.Right;
                    }
                }

                return true;
            }

            internal override bool BreadthFirstTreeWalk(TreeWalkPredicate<T> action)
            {
                this.VersionCheck(false);
                if (this.root == null)
                    return true;
                Queue<RedBlackTreeSet<T>.Node> nodeQueue = new Queue<RedBlackTreeSet<T>.Node>();
                nodeQueue.Enqueue(this.root);
                while (nodeQueue.Count != 0)
                {
                    RedBlackTreeSet<T>.Node node = nodeQueue.Dequeue();
                    if (this.IsWithinRange(node.Item) && !action(node))
                        return false;
                    if (node.Left != null && (!this._lBoundActive || this.Comparer.Compare(this._min, node.Item) < 0))
                        nodeQueue.Enqueue(node.Left);
                    if (node.Right != null && (!this._uBoundActive || this.Comparer.Compare(this._max, node.Item) > 0))
                        nodeQueue.Enqueue(node.Right);
                }

                return true;
            }

            internal override RedBlackTreeSet<T>.Node FindNode(T item)
            {
                if (!this.IsWithinRange(item))
                    return (RedBlackTreeSet<T>.Node)null;
                this.VersionCheck(false);
                return base.FindNode(item);
            }

            internal override int InternalIndexOf(T item)
            {
                int num = -1;
                foreach (T y in (RedBlackTreeSet<T>)this)
                {
                    ++num;
                    if (this.Comparer.Compare(item, y) == 0)
                        return num;
                }

                return -1;
            }

            internal override void VersionCheck(bool updateCount = false)
            {
                this.VersionCheckImpl(updateCount);
            }

            private void VersionCheckImpl(bool updateCount)
            {
                if (this.version != this._underlying.version)
                {
                    this.root = this._underlying.FindRange(this._min, this._max, this._lBoundActive,
                        this._uBoundActive);
                    this.version = this._underlying.version;
                }

                if (!updateCount || this._countVersion == this._underlying.version)
                    return;
                this.count = 0;
                this.InOrderTreeWalk((TreeWalkPredicate<T>)(n =>
               {
                   ++this.count;
                   return true;
               }));
                this._countVersion = this._underlying.version;
            }

            internal override int TotalCount()
            {
                return this._underlying.Count;
            }

            public override RedBlackTreeSet<T> GetViewBetween(T lowerValue, T upperValue)
            {
                if (this._lBoundActive && this.Comparer.Compare(this._min, lowerValue) > 0)
                    throw new ArgumentOutOfRangeException(nameof(lowerValue));
                if (this._uBoundActive && this.Comparer.Compare(this._max, upperValue) < 0)
                    throw new ArgumentOutOfRangeException(nameof(upperValue));
                return this._underlying.GetViewBetween(lowerValue, upperValue);
            }

            void ISerializable.GetObjectData(
                SerializationInfo info,
                StreamingContext context)
            {
                this.GetObjectData(info, context);
            }

            protected override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                throw new PlatformNotSupportedException();
            }

            protected override void OnDeserialization(object sender)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}