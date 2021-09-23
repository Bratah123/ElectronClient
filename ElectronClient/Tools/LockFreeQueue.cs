using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ElectronMS
{
    public sealed class LockFreeQueue<T> where T : class
    {
        private class SingleLinkNode
        {
            public SingleLinkNode Next;
            public T Item;
        }

        private SingleLinkNode mHead = null;
        private SingleLinkNode mTail = null;

        public LockFreeQueue()
        {
            mHead = new SingleLinkNode();
            mTail = mHead;
        }

        private static bool CompareAndExchange(ref SingleLinkNode pLocation, SingleLinkNode pComparand, SingleLinkNode pNewValue)
        {
            return
                (object)pComparand ==
                (object)Interlocked.CompareExchange<SingleLinkNode>(ref pLocation, pNewValue, pComparand);
        }

        public T Next
        {
            get
            {
                return mHead.Next == null ? null : mHead.Next.Item;
            }
        }
        public void Enqueue(T pItem)
        {
            SingleLinkNode oldTail = null;
            SingleLinkNode oldTailNext;

            SingleLinkNode newNode = new SingleLinkNode();
            newNode.Item = pItem;

            bool newNodeWasAdded = false;
            while (!newNodeWasAdded)
            {
                oldTail = mTail;
                oldTailNext = oldTail.Next;

                if (mTail == oldTail)
                {
                    if (oldTailNext == null)
                        newNodeWasAdded = CompareAndExchange(ref mTail.Next, null, newNode);
                    else
                        CompareAndExchange(ref mTail, oldTail, oldTailNext);
                }
            }
            CompareAndExchange(ref mTail, oldTail, newNode);
        }

        public bool Dequeue(out T pItem)
        {
            pItem = default(T);
            SingleLinkNode oldHead = null;

            bool haveAdvancedHead = false;
            while (!haveAdvancedHead)
            {

                oldHead = mHead;
                SingleLinkNode oldTail = mTail;
                SingleLinkNode oldHeadNext = oldHead.Next;

                if (oldHead == mHead)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                        {
                            return false;
                        }
                        CompareAndExchange(ref mTail, oldTail, oldHeadNext);
                    }

                    else
                    {
                        pItem = oldHeadNext.Item;
                        haveAdvancedHead =
                          CompareAndExchange(ref mHead, oldHead, oldHeadNext);
                    }
                }
            }
            return true;
        }

        public T Dequeue()
        {
            T result;
            Dequeue(out result);
            return result;
        }
    }
}