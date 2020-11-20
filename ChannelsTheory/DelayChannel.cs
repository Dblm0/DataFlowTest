using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsTheory
{
    public class DelayChannel<T> : Channel<T>
    {
        public DelayChannel(int millisecondsDelay)
        {
            PacketsDelayMilliseconds = millisecondsDelay;
            Reader = _output;
            Writer = _input;

            Task.Run(async () =>
            {
                ChannelReader<T> inpReader = _input;
                ChannelWriter<T> outWriter = _output;
                await foreach (var item in inpReader.ReadAllAsync().ConfigureAwait(false))
                {
                    await Task.WhenAll(outWriter.WriteAsync(item).AsTask(), Task.Delay(PacketsDelayMilliseconds));
                }
                outWriter.Complete();
            });
        }
        public int PacketsDelayMilliseconds { get; }
        Channel<T> _input = Channel.CreateUnbounded<T>();
        Channel<T> _output = Channel.CreateBounded<T>(1);
    }
}
