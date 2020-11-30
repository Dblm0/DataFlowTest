using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
namespace DataFlowTheory
{
    public class DelayBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>
    {
        public DelayBlock(int millisecondsDelay = 10)
        {
            PacketsDelayMilliseconds = millisecondsDelay + 1; // +1 ms to raise lower delay limit
        }
        public int PacketsDelayMilliseconds { get; }

        BufferBlock<T> _innerBlock = new BufferBlock<T>();
        ISourceBlock<T> _output => _innerBlock;
        ITargetBlock<T> _input => _innerBlock;

        #region IPropagatorBlock
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) => _innerBlock.LinkTo(target, linkOptions);
        public T ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
        {
            var item = _output.ConsumeMessage(messageHeader, target, out messageConsumed);
            if (messageConsumed)
            {
                Thread.Sleep(PacketsDelayMilliseconds);
            }
            return item;
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target) => _output.ReserveMessage(messageHeader, target);
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target) => _output.ReleaseReservation(messageHeader, target);
        public void Complete() => _input.Complete();
        public Task Completion => _output.Completion;
        public void Fault(Exception exception) => _input.Fault(exception);
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            => _input.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        public bool TryReceive(Predicate<T> filter, out T item)
        {
            bool success = _innerBlock.TryReceive(filter, out item);
            if (success)
            {
                Thread.Sleep(PacketsDelayMilliseconds);
            }
            return success;
        }
        public bool TryReceiveAll(out IList<T> items) => throw new InvalidOperationException();
        #endregion
    }
}
