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
        public DelayBlock(int millisecondsDelay, BufferBlock<T> outputBlock) : this(millisecondsDelay)
        {
            _outputBlock = outputBlock;
        }
        public DelayBlock(int millisecondsDelay = 10)
        {
            PacketsDelayMilliseconds = millisecondsDelay + 1;
            var linkOpts = new DataflowLinkOptions { PropagateCompletion = true };

            _postBlock = new ActionBlock<T>(PostToOutput);
            _postBlock.Completion.ContinueWith(x => _output.Complete());
        }
        Task DelaySleep(int ms) => Task.Run(() => Thread.Sleep(ms));
        public int PacketsDelayMilliseconds { get; }
        async Task PostToOutput(T item)
        {
            await _outputBlock.SendAsync(item);

            while (!_outputBlock.Completion.IsCompleted && _outputBlock.Count != 0) //wait for output to be consumed
            {
                await Task.Yield();
            }
            await DelaySleep(PacketsDelayMilliseconds);
        }
        ActionBlock<T> _postBlock;
        BufferBlock<T> _outputBlock = new BufferBlock<T>(new DataflowBlockOptions { BoundedCapacity = 1 });
        ISourceBlock<T> _output => _outputBlock;
        ITargetBlock<T> _input => _postBlock;

        #region IPropagatorBlock
        public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) => _output.LinkTo(target, linkOptions);
        public T ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
            => _output.ConsumeMessage(messageHeader, target, out messageConsumed);
        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target) => _output.ReserveMessage(messageHeader, target);
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target) => _output.ReleaseReservation(messageHeader, target);
        public void Complete() => _input.Complete();
        public Task Completion => _output.Completion;
        public void Fault(Exception exception) => _input.Fault(exception);
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            => _input.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        public bool TryReceive(Predicate<T> filter, out T item) => _outputBlock.TryReceive(filter, out item);
        public bool TryReceiveAll(out IList<T> items) => _outputBlock.TryReceiveAll(out items);
        #endregion
    }
}
