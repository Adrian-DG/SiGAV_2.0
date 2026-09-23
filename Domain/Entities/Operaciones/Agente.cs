using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.Operaciones;

/// <summary>
/// Miembro de las instituciones que opera las unidades en campo (Miembro en SiGAV 1.0).
/// Solo un agente activo y autorizado puede iniciar sesión en la app móvil.
/// </summary>
public class Agente : PersonMetadata, IAuditableMetadata
{
    public const int EspecialidadMaxLength = 100;

    public InstitucionEnum Institucion { get; private set; }

    public int RangoId { get; private set; }
    public virtual Rango? Rango { get; private set; }

    public AreaOperativaEnum AreaOperativa { get; private set; }

    /// <summary>En la app: acceso a todos los formularios, sin importar su área.</summary>
    public bool AccesoTotal { get; private set; }

    public string? Especialidad { get; private set; }

    /// <summary>
    /// Aprobación de front desk. Un agente que se registra desde la app queda pendiente
    /// (false) hasta que se autorice.
    /// </summary>
    public bool Autorizado { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Requerido por EF Core
    private Agente() { }

    // La Armada (ARD) usa su propia nomenclatura de rangos. Requiere que Rango esté cargado.
    public string GetRango => (Institucion == InstitucionEnum.ARD ? Rango?.NombreArmada : Rango?.Nombre) ?? string.Empty;

    public string GetInfo => $"{GetRango}, {Apellido} {Nombre}, {Institucion.ToString()}";

    public bool PuedeIniciarSesion => IsActive && Autorizado;

    public static Agente Crear(
        string identificacion,
        string nombre,
        string apellido,
        SexoEnum sexo,
        InstitucionEnum institucion,
        int rangoId,
        AreaOperativaEnum areaOperativa,
        bool accesoTotal,
        string? especialidad,
        bool autorizado)
    {
        var agente = new Agente
        {
            Identificacion = NormalizarIdentificacion(identificacion),
            Nombre = NormalizarNombre(nombre, "nombre", NombreMaxLength),
            Apellido = NormalizarNombre(apellido, "apellido", ApellidoMaxLength),
            IsActive = true,
            Autorizado = autorizado
        };

        agente.AsignarDatosInstitucionales(sexo, institucion, rangoId, areaOperativa, accesoTotal, especialidad);
        return agente;
    }

    public void ActualizarDatos(
        string identificacion,
        string nombre,
        string apellido,
        SexoEnum sexo,
        InstitucionEnum institucion,
        int rangoId,
        AreaOperativaEnum areaOperativa,
        bool accesoTotal,
        string? especialidad)
    {
        AsegurarActivo();
        Identificacion = NormalizarIdentificacion(identificacion);
        Nombre = NormalizarNombre(nombre, "nombre", NombreMaxLength);
        Apellido = NormalizarNombre(apellido, "apellido", ApellidoMaxLength);
        AsignarDatosInstitucionales(sexo, institucion, rangoId, areaOperativa, accesoTotal, especialidad);
    }

    public void Autorizar()
    {
        AsegurarActivo();
        Autorizado = true;
    }

    public void RevocarAutorizacion() => Autorizado = false;

    /// <summary>Baja lógica: el agente deja de poder iniciar sesión.</summary>
    public void Desactivar()
    {
        IsActive = false;
        Autorizado = false;
    }

    /// <summary>Quita guiones y espacios: 001-1234567-8 → 00112345678.</summary>
    public static string NormalizarIdentificacion(string identificacion)
    {
        var normalizada = new string((identificacion ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());

        if (normalizada.Length is < IdentificacionMinLength or > IdentificacionMaxLength)
            throw new DomainException($"La cédula debe tener entre {IdentificacionMinLength} y {IdentificacionMaxLength} caracteres.");

        return normalizada;
    }

    private void AsignarDatosInstitucionales(
        SexoEnum sexo,
        InstitucionEnum institucion,
        int rangoId,
        AreaOperativaEnum areaOperativa,
        bool accesoTotal,
        string? especialidad)
    {
        if (!Enum.IsDefined(sexo)) throw new DomainException("El sexo no es válido.");
        if (!Enum.IsDefined(institucion) || institucion == InstitucionEnum.NONE) throw new DomainException("La institución no es válida.");
        if (!Enum.IsDefined(areaOperativa)) throw new DomainException("El área operativa no es válida.");
        if (rangoId <= 0) throw new DomainException("El rango no es válido.");

        var especialidadNormalizada = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();
        if (especialidadNormalizada?.Length > EspecialidadMaxLength)
            throw new DomainException($"La especialidad no puede exceder {EspecialidadMaxLength} caracteres.");

        Sexo = sexo;
        Institucion = institucion;
        RangoId = rangoId;
        // Si cambia el rango, la navegación cargada deja de corresponder
        if (Rango?.Id != rangoId) Rango = null;
        AreaOperativa = areaOperativa;
        AccesoTotal = accesoTotal;
        Especialidad = especialidadNormalizada;
    }

    private static string NormalizarNombre(string valor, string campo, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new DomainException($"El {campo} es requerido.");

        var normalizado = valor.Trim();
        if (normalizado.Length > maxLength)
            throw new DomainException($"El {campo} no puede exceder {maxLength} caracteres.");

        return normalizado;
    }

    private void AsegurarActivo()
    {
        if (!IsActive) throw new DomainException($"El agente '{Identificacion}' está desactivado.");
    }
}
