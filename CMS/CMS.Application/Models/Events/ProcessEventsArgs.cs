namespace CMS.Application.Models.Events;

public class ProcessEventsArgs
{
    public int BatchSize { get; set; }
    public float ErrorEventsRatio { get; set; }
    public int ErrorCutoffTimeInMinutes { get; set; } 
    public int MaxProcessingAttempts { get; set; }
}
