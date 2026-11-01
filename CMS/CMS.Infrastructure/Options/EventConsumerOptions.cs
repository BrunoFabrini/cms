namespace CMS.Infrastructure.Options;

public class EventConsumerOptions
{
    public const string SectionName = "BackgroundServices:EventConsumer";
    public int StartupDelayInSeconds { get; set; } = 5;
    public int ExecutionDelay { get; set; } = 15;
    public int ConcurrentWorkers { get; set; } = 1;
    public int BatchSize { get; set; } = 10;
    public float ErrorEventsRatio { get; set; } = 0.2f;
    public int ErrorCutoffDateInMinutes { get; set; } = 10;
}
