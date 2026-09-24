using HtmlElementProcessor.Models;
using HtmlElementProcessor.Services;
using Microsoft.AspNetCore.Mvc;

namespace HtmlElementProcessor.Controllers;

[ApiController]
[Route("api/process")]
[Produces("application/json")]
public sealed class ProcessController(ElementProcessingService service) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType<ProcessElementsResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProcessElementsResponse>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType<ProcessElementsResponse>(StatusCodes.Status415UnsupportedMediaType)]
	[ProducesResponseType<ProcessElementsResponse>(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<ProcessElementsResponse>> Process([FromBody] ProcessElementsRequest request, CancellationToken cancellationToken)
	{
		var response = await service.ProcessAsync(request, cancellationToken);
		if (response.IsError == 0) return Ok(response);

		return StatusCode(response.ErrorCode == ErrorCodes.ProcessingError
			? StatusCodes.Status500InternalServerError
			: StatusCodes.Status400BadRequest, response);
	}
}
