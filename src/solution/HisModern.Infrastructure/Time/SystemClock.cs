using HisModern.Application.Abstractions;

namespace HisModern.Infrastructure.Time;

/// <summary>以系統時鐘實作 <see cref="IClock"/>。</summary>
public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    public DateTime Now => DateTime.Now;
}
