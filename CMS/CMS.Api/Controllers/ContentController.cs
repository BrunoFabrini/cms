using CMS.Application.Features.Contents;
using CMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMS.Infrastructure.Authentication;
using CMS.Application.Models.Content;

namespace CMS.Api.Controllers;

[ApiController]
[Route("content")]
[Authorize(AuthenticationSchemes = AuthConstants.UserClient)]
public class ContentController(
    IContentQueryService contentQueryService,
    IContentAdministrationService contentAdministrationService
) : ControllerBase
{
    private readonly IContentQueryService _contentQueryService = contentQueryService;
    private readonly IContentAdministrationService _contentAdministrationService = contentAdministrationService;

    [HttpGet("list")]
    public async Task<ActionResult<ListContentsRespose>> Get(
        [FromQuery] ListContentsRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await _contentQueryService.GetContentsAsync(request, cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContentModel>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _contentQueryService.GetContentByIdAsync(id, cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/disable")]
    [Authorize(Policy = AuthConstants.AdministratorPolicy)]
    public async Task<IActionResult> Disable(string id, CancellationToken cancellationToken)
    {
        var disabled = await _contentAdministrationService.DisableContentAsync(id, cancellationToken);

        if (disabled.IsFailed)
            return BadRequest(disabled.Errors);

        return Ok();
    }
}
