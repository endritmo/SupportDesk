using SupportDesk.Api.Models;

namespace SupportDesk.Api.Services;

public static class SlaCalculator
{
    public static TimeSpan GetSla(Priority priority) => priority switch
    {
        Priority.Critical => TimeSpan.FromHours(4),
        Priority.High => TimeSpan.FromDays(1),
        Priority.Normal => TimeSpan.FromDays(3),
        Priority.Low => TimeSpan.FromDays(7),
        _ => throw new ArgumentOutOfRangeException(nameof(priority))
    };
}