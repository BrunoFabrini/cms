namespace CMS.Application.Models;

public class ProcessEventsArgs
{
    public int BatchSize { get; set; }
    public float ErrorEventsRatio { get; set; }
    public int ErrorCutoffTimeInMinutes { get; set; } 
}
