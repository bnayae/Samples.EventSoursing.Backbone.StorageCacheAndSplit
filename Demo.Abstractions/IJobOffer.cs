#pragma warning disable S1133 // Deprecated code should be removed

using EventSourcing.Backbone;

namespace Demo.Abstractions;

public record Address(string country, string? state, string city, string street, int houseNumber);

public record User (int id, string name, Address address);

public record Hotel (string name, Address address);

public record Flight (string number, string airline, DateTimeOffset date);

/// <summary>
/// Event's schema definition
/// Return type of each method should be  <see cref="System.Threading.Tasks.ValueTask"/>
/// </summary>
[EventsContract(EventsContractType.Producer)]
[EventsContract(EventsContractType.Consumer)]
[Obsolete("Choose either the Producer or Consumer version of this interface.", true)]
public interface IJobOffer
{
    ValueTask BookVacationAsync(User user, Hotel hotel, Flight flight);
}