namespace CMS.Infrastructure.Options;

public class EventIngestionOptions
{
    public const string SectionName = "EventIngestion";
    public int MaxBatchSize { get; set; } = 100;
}
