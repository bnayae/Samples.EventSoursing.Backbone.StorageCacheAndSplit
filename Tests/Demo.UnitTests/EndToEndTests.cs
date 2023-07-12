using System.Threading.Channels;

using EventSourcing.Backbone;
using EventSourcing.Backbone.Building;

using FakeItEasy;

using Microsoft.Extensions.Logging;

using Xunit;
using Xunit.Abstractions;
using Demo.Abstractions;
using System;

#pragma warning disable HAA0301 // Closure Allocation Source


namespace Demo.Service.UnitTests;

[Trait("test-type", "unit")]
public sealed class EndToEndTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly IJobOfferConsumer _subscriber = A.Fake<IJobOfferConsumer>();
    private readonly Channel<Announcement> _channel =  Channel.CreateUnbounded<Announcement>();

    public EndToEndTests(ITestOutputHelper outputHelper)
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
    }

    [Fact]
    public async Task End2End_Test()
    {
        string uri = $"{JobOfferConstants.URI}-test";

        IJobOfferProducer producer =
            ProducerBuilder.Empty.UseChannel(_ => new ProducerTestChannel(_channel))
                    .Uri(uri)
                    .BuildJobOfferProducer();

        await producer.BookVacationAsync(
                new User(1, "joe", new Address("USA", "CA", "best-city", "wonderful street", 43)),
                new Hotel("sunny nights", new Address("Italy", null, "Rome", "pantheon", 43)),
                new Flight("2882", "Italian-airline", DateTimeOffset.UtcNow.AddDays(7)));

        var cts = new CancellationTokenSource();
        IAsyncDisposable subscription =
             ConsumerBuilder.Empty.UseChannel(_ => new ConsumerTestChannel(_channel))
                        .WithOptions(o =>  o with { MaxMessages = 2 })
                        .WithCancellation(cts.Token)
                        .Uri(uri)
                        .SubscribeJobOfferConsumer(_subscriber);

        await subscription.DisposeAsync();
        _channel.Writer.Complete();
        await _channel.Reader.Completion;

        // validation
        A.CallTo(() => _subscriber.BookVacationAsync(
                            A<ConsumerMetadata>.That.Matches(
                                        m => m.Metadata.Operation == nameof(IJobOfferConsumer.BookVacationAsync)),
                            A<User>.Ignored,
                            A<Hotel>.Ignored,
                            A<Flight>.Ignored))
                        .MustHaveHappenedOnceExactly();
    }
}
