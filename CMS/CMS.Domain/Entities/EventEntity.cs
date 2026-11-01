using CMS.Domain.Entities.Base;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities;

public class EventEntity : EntityBase
{
    public string Payload { get; set; } = null!;
    public EventStatus Status { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset? LastProcessedAtDateTime { get; set; }
}
