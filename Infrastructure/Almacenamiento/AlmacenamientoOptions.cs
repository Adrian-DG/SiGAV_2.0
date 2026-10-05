namespace Infrastructure.Almacenamiento;

/// <summary>Sección "Almacenamiento" de appsettings.</summary>
public sealed class AlmacenamientoOptions
{
    public const string Seccion = "Almacenamiento";

    /// <summary>"Local" por ahora. Pendiente de definir: "AzureBlob" o "Cloudinary".</summary>
    public string Proveedor { get; set; } = ProveedoresAlmacenamiento.Local;

    public LocalOptions Local { get; set; } = new();

    public sealed class LocalOptions
    {
        /// <summary>Carpeta raíz de los archivos; relativa a la carpeta de la API si no es absoluta.</summary>
        public string Ruta { get; set; } = "App_Data/archivos";
    }
}

public static class ProveedoresAlmacenamiento
{
    public const string Local = "Local";
}
