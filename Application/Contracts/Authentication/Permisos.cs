namespace Application.Contracts.Authentication;

/// <summary>
/// Permisos de los usuarios web (front desk). Cada uno es un rol de Identity (AppPermission) y
/// viaja en el JWT web como claim "permission". FrontDesk/src/app/core/permissions/permisos.ts
/// repite estos mismos nombres: al agregar uno aquí, agréguelo también allá.
/// </summary>
public static class Permisos
{
    public const string EventosVer = "eventos.ver";
    public const string EventosAsignar = "eventos.asignar";
    public const string UnidadesNotificar = "unidades.notificar";
    public const string AgentesVer = "agentes.ver";
    public const string AgentesGestionar = "agentes.gestionar";
    public const string UnidadesVer = "unidades.ver";
    public const string ZonasGestionar = "zonas.gestionar";
    public const string UsuariosGestionar = "usuarios.gestionar";
    public const string CalidadEvaluar = "calidad.evaluar";
    public const string CalidadEncuestas = "calidad.encuestas";

    // Módulos: agrupan los permisos en el front desk (Usuarios). El orden de Todos es el de pantalla.
    public const string ModuloEventos = "Eventos";
    public const string ModuloAgentes = "Agentes";
    public const string ModuloUnidades = "Unidades y zonas";
    public const string ModuloCalidad = "Calidad";
    public const string ModuloAdministracion = "Administración";

    /// <summary>Catálogo completo: se crea como roles al arrancar (PermisosSeeder) y genera una política por permiso.</summary>
    public static readonly IReadOnlyList<PermisoInfo> Todos =
    [
        new(EventosVer, ModuloEventos, "Consultar eventos", "Ver asistencias y accidentes reportados por las unidades y el mapa de calor."),
        new(EventosAsignar, ModuloEventos, "Asignar eventos", "Crear eventos desde el call center y asignarlos a unidades disponibles.", Requiere: EventosVer),

        new(AgentesVer, ModuloAgentes, "Consultar agentes", "Ver el personal operativo, su autorización y el reporte de eventos por agente."),
        new(AgentesGestionar, ModuloAgentes, "Gestionar agentes", "Dar de alta, editar, autorizar, desactivar e importar desde Excel al personal operativo.", Requiere: AgentesVer, AlCrearOtorgarA: AgentesVer),

        new(UnidadesVer, ModuloUnidades, "Consultar unidades", "Ver unidades, denominaciones, zonas, tramos y el mapa de unidades."),
        new(ZonasGestionar, ModuloUnidades, "Gestionar zonas y tramos", "Crear zonas dentro de una región y sus tramos, que luego se asignan a las denominaciones.", Requiere: UnidadesVer),
        new(UnidadesNotificar, ModuloUnidades, "Enviar notificaciones", "Enviar mensajes a las unidades en campo, a todas las conectadas o a las elegidas."),

        new(CalidadEvaluar, ModuloCalidad, "Evaluar eventos", "Contactar a los ciudadanos de los eventos completados, registrar la encuesta de calidad y exportar los resultados."),
        new(CalidadEncuestas, ModuloCalidad, "Administrar encuestas", "Crear nuevas versiones de la encuesta de calidad y publicarlas."),

        new(UsuariosGestionar, ModuloAdministracion, "Usuarios y permisos", "Crear usuarios del front desk y asignarles permisos."),
    ];

    public static bool Existe(string permiso) => Todos.Any(p => p.Nombre == permiso);

    /// <summary>Permisos de <paramref name="permisos"/> a los que les falta el que requieren (p. ej. gestionar sin consultar).</summary>
    public static IEnumerable<PermisoInfo> SinRequisito(IEnumerable<string> permisos)
    {
        var conjunto = permisos.ToHashSet();
        return Todos.Where(p => conjunto.Contains(p.Nombre) && p.Requiere is { } r && !conjunto.Contains(r));
    }

    public static string Titulo(string permiso) => Todos.FirstOrDefault(p => p.Nombre == permiso)?.Titulo ?? permiso;
}

/// <param name="Modulo">Módulo del front desk al que pertenece (agrupa los permisos en pantalla).</param>
/// <param name="Titulo">Nombre corto de lo que permite, dentro de su módulo.</param>
/// <param name="Requiere">
/// Permiso sin el cual este no sirve (p. ej. gestionar agentes requiere consultarlos: la pantalla es la misma).
/// </param>
/// <param name="AlCrearOtorgarA">
/// Permiso cuyos usuarios reciben este la primera vez que se crea (al separar un permiso de otro, quienes
/// ya podían hacerlo lo conservan hasta que un administrador lo quite).
/// </param>
public sealed record PermisoInfo(
    string Nombre,
    string Modulo,
    string Titulo,
    string Descripcion,
    string? Requiere = null,
    string? AlCrearOtorgarA = null);
