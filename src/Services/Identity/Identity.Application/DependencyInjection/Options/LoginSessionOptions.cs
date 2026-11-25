using System.ComponentModel.DataAnnotations;

namespace Identity.Application.DependencyInjection.Options;

public sealed class LoginSessionOptions
{
    [Range(1, 100)]
    public int MaxConcurrentSessions { get; set; } = 5;
}
