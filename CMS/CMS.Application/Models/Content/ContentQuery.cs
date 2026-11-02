namespace CMS.Application.Models.Content;

//TODO - Unify to use only ListContentsRequest. Blocker: IsAdmin must come from the service layer, not from the controller layer.
public class ContentQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "id";
    public string SortDirection { get; set; } = "asc";
    public bool IsAdmin { get; set; }
}
