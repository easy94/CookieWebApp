using System.Buffers;
using System.ComponentModel;
using System.Threading.Channels;

namespace ParallelTaskQueDS;

// This class is the actual in-memory queue for background tasks.
// It follows the producer/consumer pattern:
// - Producer: something adds work to the queue
// - Consumer: a background service reads work from the queue and executes it
// - This implementation uses ReadAllAsync to return multiple items at once, because of Parallel.ForEachAsync Consumer pattern
public sealed class TaskQue : ITaskQue
{
    // A Channel is a thread-safe queue.
    // It lets one or more producers write items and one or more consumers read items safely.
    // We store each queued item as a delegate: Func<CancellationToken, ValueTask>
    // That means each item is a unit of async work that knows how to run itself when it is dequeued.
    private readonly Channel<Func<CancellationToken,ValueTask>> _queue;

    public TaskQue(int capacity)
    {
        // A bounded channel supports a maximum number of queued items.
        // If the queue is full, FullMode.Wait tells the producer to wait until space is available.
        // This avoids dropping work unexpectedly.
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        // Create the queue with a maximum size of 'capacity'.
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    // This method is used by the producer.
    // For example: when the user presses W, MonitorLoop calls this method to queue work.
    public async ValueTask EnqueueWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        // Guard against null input so the app fails early and clearly.
        ArgumentNullException.ThrowIfNull(workItem);

        // WriteAsync waits until the queue can accept the item.
        // This is the producer side of the queue.
        await _queue.Writer.WriteAsync(workItem);
    }

    // This method is used by the consumer.
    // A hosted background service calls this to get the next item from the queue.
    public async ValueTask<IAsyncEnumerable<Func<CancellationToken, ValueTask>>> PopWorkItemAsync(CancellationToken cancellationToken)
    {
        // ReadAsync blocks until an item is available or cancellation is requested.
           var c = _queue.Reader.ReadAllAsync(cancellationToken);
        
        // The queue returns a delegate. The consumer later executes that delegate.
        return c;
    }
}