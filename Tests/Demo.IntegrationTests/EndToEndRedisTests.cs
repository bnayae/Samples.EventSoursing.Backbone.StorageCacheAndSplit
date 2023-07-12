using EventSourcing.Backbone;

using FakeItEasy;

using Microsoft.Extensions.Logging;

using Demo.Abstractions;

using StackExchange.Redis;

using Xunit.Abstractions;


// cd ./dockers/compose
// docker compose up -d

namespace Demo.Service.IntegrationTest;

[Trait("test-type", "integration")]
public sealed class EndToEndRedisTests : IDisposable
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IJobOfferConsumer _subscriber = A.Fake<IJobOfferConsumer>();

    private readonly string URI = $"integration-{DateTime.UtcNow:yyyy-MM-dd HH_mm_ss}";

    private readonly ILogger _fakeLogger = A.Fake<ILogger>();

    private readonly string ENV = $"test";
    private const int TIMEOUT = 1000 * 20;

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="EndToEndRedisTests" /> class.
    /// </summary>
    /// <param name="outputHelper">The output helper.</param>
    public EndToEndRedisTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        A.CallTo(() => _subscriber.BookVacationAsync(
                            A<ConsumerMetadata>.Ignored,
                            A<User>.Ignored,
                            A<Hotel>.Ignored,
                            A<Flight>.Ignored))
            .ReturnsLazily((ConsumerMetadata meta, User user, Hotel hotel, Flight flight) =>
            {
                _outputHelper.WriteLine($"{meta.Metadata.Operation}, {user.name}, {hotel.name}");
                return ValueTask.CompletedTask;
            });

        #region  A.CallTo(() => _fakeLogger...)

        A.CallTo(() => _fakeLogger.Log<string>(
            A<LogLevel>.Ignored,
            A<EventId>.Ignored,
            A<string>.Ignored,
            A<Exception>.Ignored,
            A<Func<string, Exception, string>>.Ignored
            ))
            .Invokes<object, LogLevel, EventId, string, Exception, Func<string, Exception, string>>(
                (level, id, msg, ex, fn) =>
                   _outputHelper.WriteLine($"Info: {fn(msg, ex)}"));

        #endregion //  A.CallTo(() => _fakeLogger...)
    }

    #endregion // Ctor

    #region OnSucceed_ACK_Test

    [Fact(Timeout = TIMEOUT)]
    public async Task OnSucceed_ACK_Test()
    {
        _outputHelper.WriteLine("Don't forget to start the docker compose environment");
        _outputHelper.WriteLine(@"  cd ./dockers/compose");
        _outputHelper.WriteLine(@"  compose up -d");

        IJobOfferProducer producer = RedisProducerBuilder.Create()
                                        .Environment(ENV)
                                        .Uri(URI)
                                        .BuildJobOfferProducer();
        for (int i = 0; i < 2; i++)
        {
            await producer.BookVacationAsync(
                    new User(1, $"user-{i}", new Address("USA", "CA", "best-city", "wonderful street", 43 + i)),
                    new Hotel("sunny nights", new Address("Italy", null, "Rome", "pantheon", 43 + i)),
                    new Flight("2882", "Italian-airline", DateTimeOffset.UtcNow.AddDays(7)));
        }
        var cts = new CancellationTokenSource();
        IConsumerLifetime subscription = RedisConsumerBuilder.Create()
                        .WithOptions(o => o with { MaxMessages = 2 })
                        .WithCancellation(cts.Token)
                        .Environment(ENV)
                        .Uri(URI)
                        .SubscribeJobOfferConsumer(_subscriber);

        await subscription.Completion;

        // validation
        A.CallTo(() => _subscriber.BookVacationAsync(
                            A<ConsumerMetadata>.That.Matches(
                                        m => m.Metadata.Operation == nameof(IJobOfferConsumer.BookVacationAsync)),
                            A<User>.Ignored,
                            A<Hotel>.Ignored,
                            A<Flight>.Ignored))
                        .MustHaveHappenedTwiceExactly();
    }

    #endregion // OnSucceed_ACK_Test

    #region Dispose pattern

    ~EndToEndRedisTests()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        string key = URI;
        IConnectionMultiplexer conn = RedisClientFactory.CreateProviderAsync(
                                                logger: _fakeLogger,
                                                configurationHook: cfg => cfg.AllowAdmin = true).Result;
        IDatabaseAsync db = conn.GetDatabase();

        db.KeyDeleteAsync(key, CommandFlags.DemandMaster).Wait();
    }

    #endregion // Dispose pattern
}
