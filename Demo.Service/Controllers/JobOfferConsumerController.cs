using Demo.Abstractions;
using System.Text.Json;
using EventSourcing.Backbone;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[ApiController]
[Route("[controller]")]
public class JobOfferConsumerController : ControllerBase
{
    private readonly ILogger<JobOfferConsumerController> _logger;
    private readonly IConsumerReceiver _receiver;

    public JobOfferConsumerController(
        ILogger<JobOfferConsumerController> logger,
        IKeyed<IConsumerReadyBuilder> consumerBuilderKeyed)
    {
        _logger = logger;
        if (!consumerBuilderKeyed.TryGet(JobOfferConstants.URI, out var consumerBuilder)) 
            throw new EventSourcingException($"The Consumer's registration found under the [{JobOfferConstants.URI}] key.");
        _receiver = consumerBuilder.BuildReceiver();
    }

    /// <summary>
    /// Gets an event by event key.
    /// </summary>
    /// <param name="eventKey">The event key.</param>
    /// <returns></returns>
    [HttpGet("{eventKey}")]
    public async Task<JsonElement> GetAsync(string eventKey)
    {
        _logger.LogDebug("fetching event [{key}]", eventKey);
        var json = await _receiver.GetJsonByIdAsync(eventKey);
        return json;
    }
}