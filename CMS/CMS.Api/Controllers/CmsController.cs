using CMS.Application.Features.CmsEvents;
using CMS.Application.Services;
using CMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("cms")]
[Authorize(AuthenticationSchemes = AuthConstants.ServiceClient)]
public class CmsController(
    IEventService eventService
) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> PostEvents(PostEventsRequest request, CancellationToken cancellationToken)
    {
        var result = await eventService.IngestEventsAsync(request, cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Created();
    }
}