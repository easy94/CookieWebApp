using BackgroundChannelWorker;
using ParallelTaskQueDS;
using WebApi.Services;
// This is the app's entry point.
// The .NET Generic Host is responsible for:
// - dependency injection
// - logging
// - configuration
// - hosted services and application lifetime
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);



// A BackgroundService is a long-running service that lives with the host.
// It is the consumer side of the producer/consumer pattern.
var connectionstring = builder.Configuration.GetConnectionString("UserContainerDb") ?? "Data Source=../usercontainer.db";
builder.Services.AddSqlite<UserContainerDb>(connectionstring);

builder.Services.AddHostedService<QueuedHostedService>();

// Register the queue implementation as a singleton so every part of the app uses the same shared queue.
// The lambda reads QueueCapacity from configuration and creates a BackgroundWorker with that size.
builder.Services.AddSingleton<ITaskQue, TaskQue>(_ =>
{
    // If no capacity is configured, use a reasonable default.
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
    {
        queueCapacity = 100;
    }

    // The queued work system is a bounded channel, so it holds up to this many pending tasks.
    return new TaskQue(queueCapacity);
});

// Build the host and all registered services.
IHost host = builder.Build();


// Run the host and keep the app alive until shutdown.
host.Run();