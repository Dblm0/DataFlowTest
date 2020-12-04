using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlowTest
{
    class DelayedItemProducer<T>
    {
        public int ItemMillisecondsDelay { get; }
        public bool IsCompleted { get; private set; } = false;
        public DelayedItemProducer(int millisecondsDelay)
        {
            ItemMillisecondsDelay = millisecondsDelay;
        }
        Queue<T> _itemQueue = new Queue<T>();
        bool _completionRequested = false;
        public void Complete() => _completionRequested = true;
        public void PostItems(params T[] items)
        {
            if (!_completionRequested)
                foreach (T i in items)
                {
                    _itemQueue.Enqueue(i);
                }
        }
        Stopwatch _delayControlSw = new Stopwatch();
        public bool TryReceve(out T item)
        {
            item = default;
            if (IsCompleted)
                return false;

            lock (_itemQueue)
            {
                if (_delayControlSw.IsRunning)
                {
                    if (_delayControlSw.ElapsedMilliseconds > ItemMillisecondsDelay)
                    {
                        var res = GetDequeResult();
                        item = res.item;
                        return res.success;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var res = GetDequeResult();
                    item = res.item;
                    return res.success;
                }
            }
        }
        (bool success, T item) GetDequeResult()
        {
            if (_itemQueue.TryDequeue(out var item))
            {
                _delayControlSw.Restart();
                IsCompleted = _completionRequested && _itemQueue.Count == 0;
                return (true, item);
            }
            else
            {
                return (false, default);
            }
        }
    }
}
