namespace CMS.Application.Features.Contents;

public class ListContentsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "id";
    public string SortDirection { get; set; } = "asc";
}
