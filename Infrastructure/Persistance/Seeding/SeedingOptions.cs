namespace Infrastructure.Persistance.Seeding;

/// <summary>Sección "Seed" de la configuración.</summary>
public sealed class SeedingOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Crea o actualiza el esquema al arrancar (aplica las migraciones; mientras no existan,
    /// crea la base a partir del modelo). Recomendado solo en desarrollo.
    /// </summary>
    public bool ApplyMigrations { get; set; }

    /// <summary>Carga los catálogos y el administrador inicial.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Carga además los datos de prueba (<see cref="SeedCategoria.Demo"/>).</summary>
    public bool DemoData { get; set; }

    public AdminSeedOptions Admin { get; set; } = new();
}

/// <summary>
/// Administrador inicial de la web. Sin contraseña no se crea: en producción debe venir de una
/// variable de entorno (Seed__Admin__Password), nunca del appsettings.
/// </summary>
public sealed class AdminSeedOptions
{
    public string UserName { get; set; } = "admin";
    public string? Password { get; set; }
    public string Identificacion { get; set; } = "00000000000";
    public string Nombre { get; set; } = "Administrador";
    public string Apellido { get; set; } = "SiGAV";
}
