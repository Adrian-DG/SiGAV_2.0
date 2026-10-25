using System.Globalization;
using Application.Common;
using Application.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Reportes;

/// <summary>
/// Formulario de asistencia de las unidades de protección y asistencia vial (APVI-FOR-003), en el
/// formato del documento institucional: encabezado con el logo del MOPC, secciones con barra azul
/// y cada dato en su casilla. Se genera con QuestPDF (licencia Community).
/// </summary>
public class ReporteAsistenciaPdf(TimeProvider timeProvider) : IReporteAsistencia
{
    // Datos de control del formulario institucional
    private const string Codigo = "APVI-FOR-003";
    private const string Version = "Versión: 1";
    private const string FechaActualizacion = "01/03/2024";

    private const string AzulSeccion = "#4680B5";
    private const string ColorBorde = "#1F2937";
    private const float GrosorBorde = 0.6f;

    private static readonly CultureInfo Formato = CultureInfo.InvariantCulture;
    private static readonly byte[] Logo = CargarLogo();

    static ReporteAsistenciaPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generar(ReporteAsistenciaDatos d)
    {
        var generado = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, ZonaHorariaOperativa.Zona);

        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(28);
            pagina.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Black));

            if (d.Anulado)
                pagina.Foreground().AlignCenter().AlignMiddle().Rotate(-30)
                    .Text("ANULADO").FontSize(96).Bold().FontColor("#40D62828");

            pagina.Content().Column(col =>
            {
                col.Spacing(4);
                Encabezado(col.Item());

                col.Item().PaddingVertical(4).Row(r =>
                {
                    r.Spacing(10);
                    r.RelativeItem(3).Element(c => Campo(c, "Asistencia No.", d.NumeroAsistencia.ToString(Formato)));
                    r.RelativeItem(2);
                    r.RelativeItem(3).Element(c => Campo(c, "Fecha", Fecha(d.FechaHoraReporte)));
                    r.RelativeItem(3).Element(c => Campo(c, "Hora", Hora(d.FechaHoraReporte)));
                });

                Seccion(col, "DATOS DEL AFECTADO Y UBICACIÓN");
                Fila(col, ("Nombre(S)", d.Nombre, 1), ("Apellido(S)", d.Apellido, 1));
                Fila(col, ("Cédula o Pasaporte", d.Identificacion, 4), ("Cantidad personas asistidas", d.CantidadPersonas.ToString(Formato), 4), ("Teléfono", d.Telefono, 3));
                Fila(col, ("Ubicación asistencia", d.UbicacionAsistencia, 1), ("Provincia", d.Provincia, 1));
                Fila(col, ("Municipio", d.Municipio, 1), ("Tramo", d.Tramo, 1));

                Seccion(col, "DATOS DEL VEHÍCULO ASISTIDO");
                Fila(col, ("Marca", d.Marca, 1), ("Modelo", d.Modelo, 1), ("Tipo", d.TipoVehiculo, 1));
                Fila(col, ("Color", d.Color, 1), ("Placa", d.Placa, 1), ("Motivo asistencia", d.MotivoAsistencia, 2));

                Seccion(col, "DATOS UNIDAD PATRULLERA");
                Fila(col, ("Denominación", d.Denominacion, 3), ("Ficha unidad patrullera", d.FichaUnidad, 2));
                Fila(col, ("Ordenada por", d.OrdenadaPor, 1));
                Fila(col, ("Reportado por", d.ReportadoPor, 1), ("Tipo de cierre", d.TipoCierre, 1));
                Fila(col, ("Hora: Despacho", Hora(d.HoraDespacho), 1), ("Llegada", Hora(d.HoraLlegada), 1), ("Retirada", Hora(d.HoraRetirada), 1));

                if (d.OtrosInvolucrados.Count > 0)
                {
                    Seccion(col, "OTROS INVOLUCRADOS EN EL EVENTO");
                    foreach (var linea in d.OtrosInvolucrados)
                        col.Item().PaddingLeft(4).Text($"• {linea}");
                }

                Seccion(col, "FIRMAS RESPONSABLES");
                Fila(col, ("Usuario", d.Usuario, 1));
                Fila(col, ("Observaciones", d.Observaciones, 1));

                col.Item().PaddingTop(6).Row(r =>
                {
                    r.Spacing(12);
                    r.RelativeItem().Element(c => Firma(c, "Firma Ciudadano", d.FirmaCiudadano));
                    r.RelativeItem().Element(c => Firma(c, "Firma Agente", d.FirmaAgente));
                });
            });

            pagina.Footer().Row(r =>
            {
                r.RelativeItem().Text($"Generado por SiGAV el {Fecha(generado)} a las {Hora(generado)}").FontSize(7).FontColor(Colors.Grey.Darken1);
                r.AutoItem().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Encabezado(IContainer contenedor) => contenedor.Row(r =>
    {
        r.ConstantItem(125).Column(logo =>
        {
            logo.Item().Height(38).AlignCenter().Image(Logo).FitHeight();
            logo.Item().PaddingTop(2).Text("Ministerio de Obras\nPúblicas y Comunicaciones").FontSize(7.5f).AlignCenter();
        });

        r.RelativeItem().AlignMiddle().PaddingHorizontal(10)
            .Text("FORMULARIO DE ASISTENCIA DE LAS UNIDADES DE PROTECCIÓN Y ASISTENCIA VIAL")
            .FontSize(11.5f).Bold().AlignCenter();

        r.ConstantItem(92).AlignTop().Border(GrosorBorde).BorderColor(ColorBorde).Column(control =>
        {
            control.Item().BorderBottom(GrosorBorde).BorderColor(ColorBorde).Padding(1.5f).Text(Codigo).FontSize(8).AlignCenter();
            control.Item().BorderBottom(GrosorBorde).BorderColor(ColorBorde).Padding(1.5f).Text(Version).FontSize(8).AlignCenter();
            control.Item().PaddingTop(1.5f).Text("Fecha Actualización").FontSize(8).AlignCenter();
            control.Item().PaddingBottom(1.5f).Text(FechaActualizacion).FontSize(8).AlignCenter();
        });
    });

    private static void Seccion(ColumnDescriptor col, string titulo)
        => col.Item().PaddingTop(4).Background(AzulSeccion).PaddingVertical(2.5f)
            .Text(titulo).FontSize(10).Bold().FontColor(Colors.White).AlignCenter();

    /// <summary>Una fila de campos; el peso reparte el ancho entre ellos.</summary>
    private static void Fila(ColumnDescriptor col, params (string Etiqueta, string? Valor, float Peso)[] campos)
        => col.Item().Row(r =>
        {
            r.Spacing(10);
            foreach (var (etiqueta, valor, peso) in campos)
                r.RelativeItem(peso).Element(c => Campo(c, etiqueta, valor));
        });

    /// <summary>Etiqueta y, a su derecha, el valor en una casilla con borde.</summary>
    private static void Campo(IContainer contenedor, string etiqueta, string? valor) => contenedor.Row(r =>
    {
        r.Spacing(3);
        r.AutoItem().AlignMiddle().Text(etiqueta);
        r.RelativeItem().Border(GrosorBorde).BorderColor(ColorBorde).PaddingHorizontal(3).PaddingVertical(1.5f)
            .Text(string.IsNullOrWhiteSpace(valor) ? " " : valor.Trim());
    });

    private static void Firma(IContainer contenedor, string titulo, byte[]? imagen) => contenedor.Column(c =>
    {
        var recuadro = c.Item().Height(105).Border(GrosorBorde).BorderColor(ColorBorde).Padding(4);
        if (imagen is not null) recuadro.AlignCenter().AlignMiddle().Image(imagen).FitArea();
        c.Item().PaddingTop(8).Text(titulo).FontSize(10).Bold().AlignCenter();
    });

    private static string Fecha(DateTime fecha) => fecha.ToString("dd-MM-yyyy", Formato);

    private static string? Hora(DateTime? fecha) => fecha?.ToString("hh:mm:ss tt", Formato);

    private static byte[] CargarLogo()
    {
        using var recurso = typeof(ReporteAsistenciaPdf).Assembly.GetManifestResourceStream("Reportes.mopc-logo.png")
            ?? throw new InvalidOperationException("Falta el recurso Reportes.mopc-logo.png (logo del formulario de asistencia).");
        using var memoria = new MemoryStream();
        recurso.CopyTo(memoria);
        return memoria.ToArray();
    }
}
