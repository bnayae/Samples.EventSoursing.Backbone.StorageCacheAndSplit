using Demo.Abstractions;

namespace Demo.Service.Entities;

public record TestRequest(Id id, params string[] notes);
