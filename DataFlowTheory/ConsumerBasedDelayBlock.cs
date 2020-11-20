using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataFlowTheory
{
    public class ConsumerBasedDelayBlock<T> : IPropagatorBlock<T, T>, IReceivableSourceBlock<T>
    {
        public ConsumerBasedDelayBlock(int millisecondsDelay = 10)
        {
            PacketsDelayMilliseconds = millisecondsDelay;

            Task.Run(async () =>
            {
                while (await _inputBlock.OutputAvailableAsync().ConfigureAwait(false))
                {
                    await Task.WhenAll(_outputBlock.SendAsync(_inputBlock.Receive()), TaskHelper.Sleep(PacketsDelayMilliseconds));
                }
                _outputBlock.Complete();
            }).ConfigureAwait(false);
        }
        public int PacketsDelayMilliseconds { get; }
        BufferBlock<T> _inputBlock = new BufferBlock<T>();
        BufferBlock<T> _outputBlock = new BufferBlock<T>(new DataflowBlockOptions { BoundedCapacity = 1 });
        ISourceBlock<T> _output => _outputBlock;
        ITargetBlock<T> _input => _inputBlock;

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
