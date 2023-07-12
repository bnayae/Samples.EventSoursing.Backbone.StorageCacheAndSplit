using Demo.Abstractions;
using Demo.Service.Entities;
using EventSourcing.Backbone;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[ApiController]
[Route("[controller]")]
public class JobOfferProducerController : ControllerBase
{
    private readonly ILogger<JobOfferProducerController> _logger;
    private readonly IJobOfferProducer _producer;

    public JobOfferProducerController(
        ILogger<JobOfferProducerController> logger,
        IKeyed<IJobOfferProducer> producerKeyed)
    {
        _logger = logger;
        if (!producerKeyed.TryGet(JobOfferConstants.URI, out var producer)) 
            throw new EventSourcingException($"Producer's registration found under the [{JobOfferConstants.URI}] key.");
        _producer = producer;
    }

    /// <summary>
    /// Post order state.
    /// </summary>
    /// <param name="name">The payload.</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    //[AllowAnonymous]
    public async Task<string> SendEventAsync(string name)
    {
        _logger.LogDebug("Sending event");
        var key = await _producer.BookVacationAsync(
                new User(name.GetHashCode(), name, new Address("USA", "CA", "best-city", "wonderful street", 43)),
                new Hotel("sunny nights", new Address("Italy", null, "Rome", "pantheon", 43)),
                new Flight("2882", "Italian-airline", DateTimeOffset.UtcNow.AddDays(7)));
        return key;
    }
}