using Application.Contracts;
using Application.Features.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Authentication;

[Route("api/authentication")]
public class AuthenticationController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
    {
        var response = await mediator.Send(request);
        return Ok(response);
    }
}