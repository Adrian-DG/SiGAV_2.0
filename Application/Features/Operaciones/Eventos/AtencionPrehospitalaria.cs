using Application.Common;
using Application.Contracts;
using Domain.Entities.Operaciones;
using Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Eventos;

// Atención prehospitalaria (ambulancia) de un paciente del evento. Viaja dentro de
// CiudadanoEventoRequest (cualquier rol), así reutiliza el registro del evento: idempotencia por
// requestId, cola offline de la app y reemplazo al completar un evento asignado.

/// <summary>Una toma de signos vitales. Todos los valores son opcionales; Glasgow: los tres o ninguno.</summary>
public record SignosVitalesRequest(
    DateTime TomadaUtc,
    int? FrecuenciaCardiaca = null,
    int? FrecuenciaRespiratoria = null,
    int? TensionSistolica = null,
    int? TensionDiastolica = null,
    int? SaturacionOxigeno = null,
    decimal? Temperatura = null,
    int? Glucemia = null,
    LlenadoCapilarEnum? LlenadoCapilar = null,
    int? GlasgowOcular = null,
    int? GlasgowVerbal = null,
    int? GlasgowMotor = null);

/// <summary>
/// Con <see cref="FueTrasladado"/>: <see cref="HospitalId"/> obligatorio; sin traslado:
/// <see cref="MotivoNoTraslado"/> obligatorio. <see cref="NombreEventoEspecial"/> solo con tipo Evento especial.
/// Médico y regulador: agentes activos del área PreHospitalaria. <see cref="RequestId"/>: clave que genera la
/// app para la atención; un reenvío con la misma clave actualiza la atención en vez de duplicar a la persona.
/// </summary>
public record AtencionPrehospitalariaRequest(
    DespachoPrehospitalarioEnum DespachadoPor,
    TipoAtencionPrehospitalariaEnum TipoAtencion,
    CausaAtencionPrehospitalariaEnum Causa,
    bool FueTrasladado,
    int? HospitalId = null,
    string? RecibidoPor = null,
    MotivoNoTrasladoEnum? MotivoNoTraslado = null,
    string? NombreEventoEspecial = null,
    string? Antecedentes = null,
    string? HallazgosPositivos = null,
    string? DiagnosticoPresuntivo = null,
    string? ProcedimientosRealizados = null,
    string? InsumosUtilizados = null,
    int? MedicoAgenteId = null,
    int? ReguladorAgenteId = null,
    IReadOnlyList<SignosVitalesRequest>? SignosVitales = null,
    Guid? RequestId = null)
{
    public AtencionPrehospitalaria ToDominio() => AtencionPrehospitalaria.Registrar(
        DespachadoPor,
        TipoAtencion,
        Causa,
        FueTrasladado,
        HospitalId,
        RecibidoPor,
        MotivoNoTraslado,
        NombreEventoEspecial,
        Antecedentes,
        HallazgosPositivos,
        DiagnosticoPresuntivo,
        ProcedimientosRealizados,
        InsumosUtilizados,
        MedicoAgenteId,
        ReguladorAgenteId,
        (SignosVitales ?? []).Select(s => TomaSignosVitales.Registrar(
            s.TomadaUtc,
            s.FrecuenciaCardiaca,
            s.FrecuenciaRespiratoria,
            s.TensionSistolica,
            s.TensionDiastolica,
            s.SaturacionOxigeno,
            s.Temperatura,
            s.Glucemia,
            s.LlenadoCapilar,
            s.GlasgowOcular,
            s.GlasgowVerbal,
            s.GlasgowMotor)),
        RequestId);
}

/// <summary>
/// Repite las reglas del dominio para responder 400 con el campo exacto (el dominio también las
/// garantiza) y valida lo que solo la aplicación conoce: existencia del hospital y área del personal.
/// </summary>
public class AtencionPrehospitalariaRequestValidator : AbstractValidator<AtencionPrehospitalariaRequest>
{
    public AtencionPrehospitalariaRequestValidator(IReadDbContext db, TimeProvider timeProvider)
    {
        RuleFor(x => x.RequestId).NotEqual(Guid.Empty).WithMessage("La clave de la atención (requestId) no es válida.");
        RuleFor(x => x.DespachadoPor).IsInEnum().WithMessage("Indique quién despachó la ambulancia.");
        RuleFor(x => x.TipoAtencion).IsInEnum().WithMessage("Indique el tipo de atención.");
        RuleFor(x => x.Causa).IsInEnum().WithMessage("Indique si la causa es traumática o clínica.");

        RuleFor(x => x.NombreEventoEspecial)
            .NotEmpty().WithMessage("Indique el nombre del evento especial.")
            .When(x => x.TipoAtencion == TipoAtencionPrehospitalariaEnum.EventoEspecial);
        RuleFor(x => x.NombreEventoEspecial).MaximumLength(AtencionPrehospitalaria.TextoCortoMaxLength);
        RuleFor(x => x.RecibidoPor).MaximumLength(AtencionPrehospitalaria.TextoCortoMaxLength);
        RuleFor(x => x.Antecedentes).MaximumLength(AtencionPrehospitalaria.TextoLargoMaxLength);
        RuleFor(x => x.HallazgosPositivos).MaximumLength(AtencionPrehospitalaria.TextoLargoMaxLength);
        RuleFor(x => x.DiagnosticoPresuntivo).MaximumLength(AtencionPrehospitalaria.TextoLargoMaxLength);
        RuleFor(x => x.ProcedimientosRealizados).MaximumLength(AtencionPrehospitalaria.TextoLargoMaxLength);
        RuleFor(x => x.InsumosUtilizados).MaximumLength(AtencionPrehospitalaria.TextoLargoMaxLength);

        // ---- Traslado
        When(x => x.FueTrasladado, () =>
        {
            RuleFor(x => x.HospitalId)
                .NotNull().WithMessage("Indique el centro de salud al que se trasladó al paciente.");
            RuleFor(x => x.HospitalId!.Value)
                .MustAsync((id, ct) => db.Hospitales.ExisteActivoAsync(id, ct))
                .WithMessage("El centro de salud no existe o está desactivado.")
                .When(x => x.HospitalId.HasValue);
            RuleFor(x => x.MotivoNoTraslado)
                .Null().WithMessage("Un paciente trasladado no tiene motivo de no traslado.");
        }).Otherwise(() =>
        {
            RuleFor(x => x.MotivoNoTraslado)
                .NotNull().WithMessage("Indique por qué el paciente no fue trasladado.")
                .IsInEnum().WithMessage("El motivo de no traslado no es válido.");
            RuleFor(x => x.HospitalId)
                .Null().WithMessage("Un paciente no trasladado no tiene centro de salud.");
        });

        // ---- Personal
        RuleFor(x => x.MedicoAgenteId!.Value)
            .MustAsync((id, ct) => EsPersonalPrehospitalarioAsync(db, id, ct))
            .WithMessage("El médico debe ser un agente activo del área prehospitalaria.")
            .When(x => x.MedicoAgenteId.HasValue);
        RuleFor(x => x.ReguladorAgenteId!.Value)
            .MustAsync((id, ct) => EsPersonalPrehospitalarioAsync(db, id, ct))
            .WithMessage("El regulador debe ser un agente activo del área prehospitalaria.")
            .When(x => x.ReguladorAgenteId.HasValue);

        // ---- Signos vitales
        RuleFor(x => x.SignosVitales)
            .Must(s => s is null || s.Count <= AtencionPrehospitalaria.MaxTomasSignosVitales)
            .WithMessage($"No se pueden registrar más de {AtencionPrehospitalaria.MaxTomasSignosVitales} tomas de signos vitales.");
        RuleForEach(x => x.SignosVitales).ChildRules(s =>
        {
            s.RuleFor(x => x.TomadaUtc)
                .NotEqual(default(DateTime)).WithMessage("Indique la hora de la toma de signos vitales.")
                .Must(t => t.ToUniversalTime() <= timeProvider.GetUtcNow().UtcDateTime + Evento.ToleranciaReloj)
                .WithMessage("La hora de la toma de signos vitales no puede estar en el futuro.");
            s.RuleFor(x => x.FrecuenciaCardiaca).InclusiveBetween(0, TomaSignosVitales.FrecuenciaCardiacaMaxima);
            s.RuleFor(x => x.FrecuenciaRespiratoria).InclusiveBetween(0, TomaSignosVitales.FrecuenciaRespiratoriaMaxima);
            s.RuleFor(x => x.TensionSistolica).InclusiveBetween(0, TomaSignosVitales.TensionSistolicaMaxima);
            s.RuleFor(x => x.TensionDiastolica).InclusiveBetween(0, TomaSignosVitales.TensionDiastolicaMaxima);
            s.RuleFor(x => x.SaturacionOxigeno).InclusiveBetween(0, 100);
            s.RuleFor(x => x.Temperatura).InclusiveBetween(TomaSignosVitales.TemperaturaMinima, TomaSignosVitales.TemperaturaMaxima);
            s.RuleFor(x => x.Glucemia).InclusiveBetween(0, TomaSignosVitales.GlucemiaMaxima);
            s.RuleFor(x => x.LlenadoCapilar).IsInEnum();
            s.RuleFor(x => x.GlasgowOcular).InclusiveBetween(1, TomaSignosVitales.GlasgowOcularMaximo);
            s.RuleFor(x => x.GlasgowVerbal).InclusiveBetween(1, TomaSignosVitales.GlasgowVerbalMaximo);
            s.RuleFor(x => x.GlasgowMotor).InclusiveBetween(1, TomaSignosVitales.GlasgowMotorMaximo);
            s.RuleFor(x => x)
                .Must(x => new[] { x.GlasgowOcular, x.GlasgowVerbal, x.GlasgowMotor }.Count(g => g is null) is 0 or 3)
                .WithMessage("Registre los tres componentes de la escala de Glasgow (ocular, verbal y motor) o ninguno.")
                .Must(x => x.TensionSistolica is not > 0 || x.TensionDiastolica is null || x.TensionSistolica > x.TensionDiastolica)
                .WithMessage("La tensión sistólica debe ser mayor que la diastólica.");
        });
    }

    private static Task<bool> EsPersonalPrehospitalarioAsync(IReadDbContext db, int agenteId, CancellationToken cancellationToken)
        => db.Agentes.AnyAsync(
            a => a.Id == agenteId && a.IsActive && a.AreaOperativa == AreaOperativaEnum.PreHospitalaria,
            cancellationToken);
}

/// <summary>Toma de signos vitales en el detalle del evento. GlasgowTotal: 3-15, o null sin Glasgow.</summary>
public record TomaSignosVitalesViewModel(
    DateTime TomadaUtc,
    int? FrecuenciaCardiaca,
    int? FrecuenciaRespiratoria,
    int? TensionSistolica,
    int? TensionDiastolica,
    int? SaturacionOxigeno,
    decimal? Temperatura,
    int? Glucemia,
    LlenadoCapilarEnum? LlenadoCapilar,
    int? GlasgowOcular,
    int? GlasgowVerbal,
    int? GlasgowMotor,
    int? GlasgowTotal);

/// <summary>Atención prehospitalaria de un paciente, en el detalle del evento (solo para quien puede ver datos clínicos).</summary>
public record AtencionPrehospitalariaViewModel(
    DespachoPrehospitalarioEnum DespachadoPor,
    TipoAtencionPrehospitalariaEnum TipoAtencion,
    string? NombreEventoEspecial,
    CausaAtencionPrehospitalariaEnum Causa,
    bool FueTrasladado,
    int? HospitalId,
    string? Hospital,
    string? RecibidoPor,
    MotivoNoTrasladoEnum? MotivoNoTraslado,
    string? Antecedentes,
    string? HallazgosPositivos,
    string? DiagnosticoPresuntivo,
    string? ProcedimientosRealizados,
    string? InsumosUtilizados,
    int? MedicoAgenteId,
    string? Medico,
    int? ReguladorAgenteId,
    string? Regulador,
    IReadOnlyList<TomaSignosVitalesViewModel> SignosVitales)
{
    /// <summary>Hospital y personal se resuelven aparte (nombres por Id) porque la atención es un tipo owned.</summary>
    public static AtencionPrehospitalariaViewModel Desde(
        AtencionPrehospitalaria a,
        IReadOnlyDictionary<int, string> hospitales,
        IReadOnlyDictionary<int, string> agentes)
        => new(
            a.DespachadoPor,
            a.TipoAtencion,
            a.NombreEventoEspecial,
            a.Causa,
            a.FueTrasladado,
            a.HospitalId,
            a.HospitalId is { } h ? hospitales.GetValueOrDefault(h) : null,
            a.RecibidoPor,
            a.MotivoNoTraslado,
            a.Antecedentes,
            a.HallazgosPositivos,
            a.DiagnosticoPresuntivo,
            a.ProcedimientosRealizados,
            a.InsumosUtilizados,
            a.MedicoAgenteId,
            a.MedicoAgenteId is { } m ? agentes.GetValueOrDefault(m) : null,
            a.ReguladorAgenteId,
            a.ReguladorAgenteId is { } r ? agentes.GetValueOrDefault(r) : null,
            a.SignosVitales
                .OrderBy(t => t.TomadaUtc)
                .Select(t => new TomaSignosVitalesViewModel(
                    t.TomadaUtc, t.FrecuenciaCardiaca, t.FrecuenciaRespiratoria, t.TensionSistolica, t.TensionDiastolica,
                    t.SaturacionOxigeno, t.Temperatura, t.Glucemia, t.LlenadoCapilar,
                    t.GlasgowOcular, t.GlasgowVerbal, t.GlasgowMotor, t.GlasgowTotal))
                .ToList());
}
