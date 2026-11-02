using CMS.Application.Features.Contents;
using CMS.Application.Models;
using CMS.Application.Models.Content;
using CMS.Application.Services;
using CMS.Domain.Entities;
using CMS.Domain.Repositories.Content;
using FluentResults;
using System.Text.Json;

namespace CMS.Infrastructure.Services;

public class ContentQueryService(
    IContentReadRepository contentReadRepository,
    IAuthService authService
) : IContentQueryService
{
    private readonly IContentReadRepository _contentReadRepository = contentReadRepository;
    private readonly IAuthService _authService = authService;

    public async Task<Result<PagedResponse<ContentModel>>> GetContentsAsync(
        ListContentsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateQuery(request);

        if (validationResult.IsFailed)
            return Result.Fail(validationResult.Errors);

        var isAdmin = _authService.UserIsAdmin();

        var countResult = await _contentReadRepository.GetVisibleCountAsync(isAdmin, cancellationToken);

        if (countResult.IsFailed)
            return Result.Fail(countResult.Errors);

        var contentsResult = await _contentReadRepository.GetVisibleContentsAsync(
            includeDisabled: isAdmin,
            page: request.Page,
            pageSize: request.PageSize,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            cancellationToken: cancellationToken
        );

        if (contentsResult.IsFailed)
            return Result.Fail(contentsResult.Errors);

        var contentList = new List<ContentModel>();

        foreach (var content in contentsResult.Value)
        {
            var mappingResult = MapContent(content);

            if (mappingResult.IsFailed)
                return Result.Fail(mappingResult.Errors);

            contentList.Add(mappingResult.Value);
        }

        var totalPages = countResult.Value == 0
            ? 0
            : (int)Math.Ceiling(countResult.Value / (double)request.PageSize);

        return Result.Ok(new PagedResponse<ContentModel>
        {
            Items = contentList,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = countResult.Value,
            TotalPages = totalPages
        });
    }

    public async Task<Result<ContentModel?>> GetContentByIdAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return Result.Fail("Content identifier is required.");

        var isAdmin = _authService.UserIsAdmin();

        var contentResult = await _contentReadRepository.GetVisibleContentByPublicIdAsync(
            publicId,
            isAdmin,
            cancellationToken);

        if (contentResult.IsFailed)
            return Result.Fail(contentResult.Errors);

        if (contentResult.Value is null)
            return Result.Fail("Content not found.");

        var responseResult = MapContent(contentResult.Value);

        return responseResult.IsFailed
            ? Result.Fail(responseResult.Errors)
            : Result.Ok<ContentModel?>(responseResult.Value);
    }

    private static Result ValidateQuery(ListContentsRequest request)
    {
        if (request.Page < 1)
            return Result.Fail("Page must be greater than zero.");
        if (request.PageSize < 1 || request.PageSize > 100)
            return Result.Fail("Page size must be between 1 and 100.");
        if (!IsValidSortField(request.SortBy))
            return Result.Fail("Sort field must be id, version, or timestamp.");
        if (!IsValidSortDirection(request.SortDirection))
            return Result.Fail("Sort direction must be asc or desc.");

        return Result.Ok();
    }

    private static bool IsValidSortField(string? sortBy) =>
        string.Equals(sortBy, "id", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sortBy, "version", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sortBy, "timestamp", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidSortDirection(string? sortDirection) =>
        string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    private static Result<ContentModel> MapContent(ContentEntity content)
    {
        var latestVersion = content.Versions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        if (latestVersion is null || !latestVersion.Published)
            return Result.Fail<ContentModel>($"Content {content.PublicId} has no published latest version.");

        using var document = JsonDocument.Parse(latestVersion.Content);

        return Result.Ok(new ContentModel
        {
            Id = content.PublicId,
            Version = latestVersion.VersionNumber,
            Timestamp = latestVersion.Timestamp,
            Content = document.RootElement.Clone()
        });
    }
}
