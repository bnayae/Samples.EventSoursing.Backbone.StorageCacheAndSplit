using Demo.Abstractions;

namespace Demo.Service.Entities;

public record Review(Id id, params string[] notes);
