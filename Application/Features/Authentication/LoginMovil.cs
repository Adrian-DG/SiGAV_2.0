using Application.Contracts.Authentication;
using Application.Exceptions;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Features.Authentication;

// Command: inicio de sesión de un agente con la unidad que va a operar (app móvil)
public record LoginMovilCommand(string Cedula, string Ficha) : IRequest<AuthenticatedResponse>;

// Validator
public class LoginMovilCommandValidator : AbstractValidator<LoginMovilCommand>
{
    public LoginMovilCommandValidator()
    {
        RuleFor(x => x.Cedula)
            .NotEmpty().WithMessage("La cédula es requerida.")
            .Must(c => (c ?? string.Empty).Count(char.IsLetterOrDigit) == 11).WithMessage("La cédula debe tener 11 caracteres (sin guiones).");
        RuleFor(x => x.Ficha).NotEmpty().WithMessage("La ficha es requerida.");
    }
}

// Handler
public class LoginMovilCommandHandler(
    IAgenteRepository agentes,
    IUnidadRepository unidades,
    IJwtBearerHelper jwt) : IRequestHandler<LoginMovilCommand, AuthenticatedResponse>
{
    public async Task<AuthenticatedResponse> Handle(LoginMovilCommand request, CancellationToken cancellationToken)
    {
        var cedula = new string(request.Cedula.Where(char.IsLetterOrDigit).ToArray());
        var agente = await agentes.GetActivoByIdentificacionAsync(cedula, cancellationToken);
        var unidad = await unidades.GetByFichaAsync(request.Ficha.Trim(), cancellationToken);

        // En SiGAV 1.0 estas reglas solo se validaban en la app (unidades/confirm) y el
        // endpoint de login las aceptaba si se llamaba directamente. El agente además debe estar
        // activo y autorizado por front desk.
        if (agente is null || !agente.PuedeIniciarSesion || unidad is null || !unidad.IsActive || !unidad.EstaDisponible || unidad.DenominacionId is null)
            throw new UnauthorizedException("La cédula o la ficha no son válidas, o la unidad no está disponible.");

        return jwt.GenerateMovilToken(new MovilUserIdentity(
            agente.Id,
            agente.Identificacion,
            agente.GetRango ?? string.Empty,
            agente.GetInfo,
            unidad.Id,
            unidad.Ficha));
    }
}
