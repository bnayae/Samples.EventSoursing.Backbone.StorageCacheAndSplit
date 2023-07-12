using System;

using Demo.Abstractions;

using EventSourcing.Backbone;
using EventSourcing.Backbone.Building;

namespace Demo.Controllers;

/// <summary>
/// Consumer job
/// </summary>
/// <seealso cref="Microsoft.Extensions.Hosting.IHostedService" />
/// <seealso cref="System.IAsyncDisposable" />
/// <seealso cref="System.IDisposable" />
public sealed class ConsumerJob : IHostedService, IJobOfferConsumer
{
    private readonly IConsumerSubscribeBuilder _builder;
    private CancellationTokenSource? _cancellationTokenSource;
    private IConsumerLifetime? _subscription;

    private readonly ILogger _logger;
    private readonly IJobOfferProducer _producer;

    #region Ctor

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="consumerBuilderKeyed">The consumer builder.</param>
    /// <param name="producer">The producer.</param>
    public ConsumerJob(
        ILogger<ConsumerJob> logger,
        IKeyed<IConsumerReadyBuilder> consumerBuilderKeyed,
        IJobOfferProducer producer)
    {
        if (!consumerBuilderKeyed.TryGet(JobOfferConstants.URI, out var consumerBuilder)) 
            throw new EventSourcingException($"Consumer's registration found under the [{JobOfferConstants.URI}] key.");
        _builder = consumerBuilder.WithLogger(logger);
        _logger = logger;
        _producer = producer;
        logger.LogInformation("Consumer starts listening on: {URI}", JobOfferConstants.URI);
    }

    #endregion Ctor

    #region OnStartAsync

    /// <summary>
    /// Start Consumer Job.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var canellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        _subscription = _builder
                                .Group(JobOfferConstants.CONSUMER_GROUP)
                                .WithCancellation(canellation.Token)
                                // this extension is generate (if you change the interface use the correlated new generated extension method)
                                .SubscribeJobOfferConsumer(this);

        return Task.CompletedTask;
    }

    #endregion // OnStartAsync

    #region StopAsync

    /// <summary>
    /// Stops the Consumer Job.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    async Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.CancelSafe();
        await (_subscription?.Completion ?? Task.CompletedTask);
    }

    #endregion // StopAsync

    async ValueTask IJobOfferConsumer.BookVacationAsync(ConsumerMetadata consumerMetadata, User user, Hotel hotel, Flight flight)
    {
        var meta = consumerMetadata.Metadata;
        LogLevel level = meta.Environment == "prod" ? LogLevel.Debug : LogLevel.Information;
        _logger.Log(level, """
                                   handling {event} [{uri}]
                                   """
        , meta.Operation, meta.UriDash);


        await consumerMetadata.AckAsync(); // not required on default setting or when configuring the consumer to Ack on success.
    }
}

