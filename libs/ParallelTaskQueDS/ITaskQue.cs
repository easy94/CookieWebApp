using System;

namespace ParallelTaskQueDS;

// This interface defines the contract for the background queue.
// It keeps the producer and consumer code loosely coupled.
// In other words, the monitor loop does not need to know how the queue is implemented.
// The hosted service also does not need to know the details of channel internals.
public interface ITaskQue
{
    // Adds a new asynchronous work item to the queue.
    // This is the producer side.
    public ValueTask EnqueueWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    // Removes and returns the next work item from the queue.
    // This is the consumer side.
    public ValueTask<IAsyncEnumerable<Func<CancellationToken,ValueTask>>> PopWorkItemAsync(CancellationToken cancellationToken);
}
