namespace BackgroundChannelWorker;
using ParallelTaskQueDS;

// This class is the consumer in the producer/consumer pattern.
// It runs as a hosted background service for the lifetime of the application.
// Its job is to continuously read jobs from the queue and execute them.
public sealed class QueuedHostedService(
        ITaskQue taskQueue,
        ILogger<QueuedHostedService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This log message tells the user how to trigger work.
        logger.LogInformation("""
            {Name} is running.
            Tap W to add a work item to the 
            background queue.
            """,
            nameof(QueuedHostedService));


        // Once the service starts, begin the loop that reads and executes queued tasks.
        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        // Keep processing until the app is shutting down.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Take the next work item from the queue.
                // If no item is available, ReadAsync waits until one is added.
                var workItems = await taskQueue.PopWorkItemAsync(stoppingToken);

                // Execute the queued job.
                // The work item itself is a delegate that knows how to run.
                var allTasks = Parallel.ForEachAsync(workItems, stoppingToken, async (workItem, token) =>
                {
                    await workItem(token);
                });
                await Task.WhenAll(allTasks);
            }
            catch (OperationCanceledException)
            {
                // This is expected when the host is shutting down.
                // We do not want to crash the whole app just because cancellation was requested.
            }
            catch (Exception ex)
            {
                // If a queued job throws an exception, we log it but continue processing the queue.
                logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            $"{nameof(QueuedHostedService)} is stopping.");

        // Let the base BackgroundService handle normal shutdown cleanup.
        await base.StopAsync(stoppingToken);
    }
}