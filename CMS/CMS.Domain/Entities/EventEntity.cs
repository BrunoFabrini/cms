using CMS.Domain.Entities.Base;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities;

public class EventEntity : EntityBase
{
    public string Payload { get; set; } = default!;
    public EventStatus Status { get; set; }
    public int ProcessingAttempts { get; set; }
    public string ErrorMessage { get; set; } = default!;
    public DateTimeOffset? LastProcessedAtDateTime { get; set; }
}
