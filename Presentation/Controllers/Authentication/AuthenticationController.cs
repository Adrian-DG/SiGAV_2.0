using Application.Contracts.Authentication;
using Application.Features.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Authentication;

[Route("api/authentication")]
public class AuthenticationController(IMediator mediator) : ControllerBase
{
    /// <summary>Front desk: usuario y contraseña (ASP.NET Identity). Emite un token de audiencia web.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(request, cancellationToken));

    /// <summary>App móvil: cédula del agente y ficha de la unidad. Emite un token de audiencia móvil.</summary>
    [AllowAnonymous]
    [HttpPost("movil/login")]
    public async Task<IActionResult> LoginMovil([FromBody] LoginMovilCommand request, CancellationToken cancellationToken)
        => Ok(await mediator.Send(request, cancellationToken));

    /// <summary>Identidad de la sesión actual, para ambos tipos de token.</summary>
    [Authorize(Policy = SesionPolicies.Operativa)]
    [HttpGet("sesion")]
    public async Task<IActionResult> GetSesion(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetSesionActualQuery(), cancellationToken));
}
