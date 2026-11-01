using CMS.Application.Features.CmsEvents;
using CMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CmsController(
    IEventService eventService
) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> PostEvents(PostEventsRequest request, CancellationToken cancellationToken)
    {
        var result = await eventService.ProcessAsync(request, cancellationToken);

        if (result.IsFailed)
            return BadRequest(result.Errors);

        return Created();
    }
}