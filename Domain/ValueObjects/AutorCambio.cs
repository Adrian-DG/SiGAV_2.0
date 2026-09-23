using Domain.Exceptions;

namespace Domain.ValueObjects;

/// <summary>
/// Quién, cuándo y por qué se cambia la denominación de una unidad. Todo cambio debe tener
/// un usuario responsable de la aplicación web (front desk).
/// </summary>
public sealed record AutorCambio
{
    public const int ObservacionMaxLength = 250;

    public int UsuarioId { get; }
    public DateTime FechaUtc { get; }
    public string? Observacion { get; }

    public AutorCambio(int usuarioId, DateTime fechaUtc, string? observacion = null)
    {
        if (usuarioId <= 0)
            throw new DomainException("Todo cambio de denominación debe tener un usuario responsable.");

        var nota = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        if (nota?.Length > ObservacionMaxLength)
            throw new DomainException($"La observación no puede exceder {ObservacionMaxLength} caracteres.");

        UsuarioId = usuarioId;
        FechaUtc = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);
        Observacion = nota;
    }
}
