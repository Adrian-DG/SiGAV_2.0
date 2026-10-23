using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Application.Contracts;

namespace Infrastructure.HojasCalculo;

/// <summary>
/// Lee y escribe .xlsx (Office Open XML) directamente sobre el zip, sin librerías externas.
/// Solo maneja valores como texto: celdas de texto compartido, en línea, fórmulas (su último
/// valor calculado) y números tal como están guardados.
/// </summary>
public class HojaCalculoXlsx : IHojaCalculo
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelDoc = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace RelPkg = "http://schemas.openxmlformats.org/package/2006/relationships";

    // Protección ante archivos manipulados (zip bomb): ninguna parte que se lee puede pasar de esto descomprimida
    private const long MaxBytesParte = 50L * 1024 * 1024;

    public IReadOnlyList<FilaHoja> LeerPrimeraHoja(Stream contenido)
    {
        try
        {
            using var zip = new ZipArchive(contenido, ZipArchiveMode.Read, leaveOpen: true);
            var compartidos = LeerTextosCompartidos(zip);
            var hoja = CargarXml(zip, RutaPrimeraHoja(zip))
                ?? throw new InvalidDataException("El libro no tiene hojas.");

            var filas = new List<FilaHoja>();
            var numeroAnterior = 0;
            foreach (var row in hoja.Descendants(Main + "row"))
            {
                var numero = int.TryParse((string?)row.Attribute("r"), out var r) ? r : numeroAnterior + 1;
                numeroAnterior = numero;

                var celdas = new List<string>();
                foreach (var c in row.Elements(Main + "c"))
                {
                    var columna = IndiceColumna((string?)c.Attribute("r")) ?? celdas.Count;
                    while (celdas.Count < columna) celdas.Add(string.Empty);
                    var valor = ValorCelda(c, compartidos);
                    if (columna < celdas.Count) celdas[columna] = valor;
                    else celdas.Add(valor);
                }

                if (celdas.Any(v => !string.IsNullOrWhiteSpace(v)))
                    filas.Add(new FilaHoja(numero, celdas.Select(v => v.Trim()).ToList()));
            }

            return filas;
        }
        catch (Exception ex) when (ex is InvalidDataException or XmlException or IOException)
        {
            throw new InvalidDataException("El archivo no es un libro de Excel (.xlsx) válido.", ex);
        }
    }

    public byte[] Escribir(IReadOnlyList<HojaNueva> hojas)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var sb = new StringBuilder();

            sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");
            for (var i = 1; i <= hojas.Count; i++)
                sb.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
            sb.Append("</Types>");
            EscribirParte(zip, "[Content_Types].xml", sb);

            sb.Clear().Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            EscribirParte(zip, "_rels/.rels", sb);

            sb.Clear().Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>""");
            for (var i = 1; i <= hojas.Count; i++)
                sb.Append($"""<sheet name="{Escapar(NombreHoja(hojas[i - 1].Nombre))}" sheetId="{i}" r:id="rId{i}"/>""");
            sb.Append("</sheets></workbook>");
            EscribirParte(zip, "xl/workbook.xml", sb);

            sb.Clear().Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
            for (var i = 1; i <= hojas.Count; i++)
                sb.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
            sb.Append($"""<Relationship Id="rId{hojas.Count + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");
            EscribirParte(zip, "xl/_rels/workbook.xml.rels", sb);

            // Estilo 0: normal; estilo 1: negrita (encabezados)
            sb.Clear().Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs></styleSheet>""");
            EscribirParte(zip, "xl/styles.xml", sb);

            for (var i = 1; i <= hojas.Count; i++)
                EscribirParte(zip, $"xl/worksheets/sheet{i}.xml", XmlHoja(sb.Clear(), hojas[i - 1]));
        }

        return memoria.ToArray();
    }

    private static StringBuilder XmlHoja(StringBuilder sb, HojaNueva hoja)
    {
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        var columnas = hoja.Filas.Count == 0 ? 0 : hoja.Filas.Max(f => f.Count);
        if (columnas > 0)
        {
            sb.Append("<cols>");
            for (var c = 0; c < columnas; c++)
            {
                var largo = hoja.Filas.Max(f => c < f.Count ? f[c].Length : 0);
                var ancho = Math.Clamp(largo + 4, 12, 60);
                sb.Append(CultureInfo.InvariantCulture, $"""<col min="{c + 1}" max="{c + 1}" width="{ancho}" customWidth="1"/>""");
            }
            sb.Append("</cols>");
        }

        sb.Append("<sheetData>");
        for (var r = 0; r < hoja.Filas.Count; r++)
        {
            sb.Append($"""<row r="{r + 1}">""");
            var fila = hoja.Filas[r];
            for (var c = 0; c < fila.Count; c++)
            {
                if (string.IsNullOrEmpty(fila[c])) continue;
                var estilo = r == 0 ? " s=\"1\" " : " ";
                sb.Append($"""<c r="{LetrasColumna(c)}{r + 1}"{estilo}t="inlineStr"><is><t xml:space="preserve">{Escapar(fila[c])}</t></is></c>""");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb;
    }

    private static void EscribirParte(ZipArchive zip, string ruta, StringBuilder contenido)
    {
        using var escritor = new StreamWriter(zip.CreateEntry(ruta, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
        escritor.Write(contenido);
    }

    private static string RutaPrimeraHoja(ZipArchive zip)
    {
        const string porDefecto = "xl/worksheets/sheet1.xml";

        var libro = CargarXml(zip, "xl/workbook.xml");
        var idRelacion = (string?)libro?.Descendants(Main + "sheet").FirstOrDefault()?.Attribute(RelDoc + "id");
        if (idRelacion is null) return porDefecto;

        var destino = (string?)CargarXml(zip, "xl/_rels/workbook.xml.rels")?
            .Descendants(RelPkg + "Relationship")
            .FirstOrDefault(r => (string?)r.Attribute("Id") == idRelacion)?
            .Attribute("Target");
        if (string.IsNullOrEmpty(destino)) return porDefecto;

        // Relativo a xl/ salvo que sea absoluto dentro del paquete ("/xl/worksheets/sheet1.xml")
        return destino.StartsWith('/') ? destino.TrimStart('/') : "xl/" + destino;
    }

    private static List<string> LeerTextosCompartidos(ZipArchive zip)
    {
        var xml = CargarXml(zip, "xl/sharedStrings.xml");
        if (xml is null) return [];

        // Texto con formato (<r><t>…</t></r>) se concatena; las guías fonéticas (<rPh>) se omiten
        return xml.Root!.Elements(Main + "si")
            .Select(si => string.Concat(si.Descendants(Main + "t")
                .Where(t => t.Parent?.Name != Main + "rPh")
                .Select(t => t.Value)))
            .ToList();
    }

    private static string ValorCelda(XElement c, List<string> compartidos)
    {
        var tipo = (string?)c.Attribute("t");
        if (tipo == "inlineStr")
            return string.Concat(c.Element(Main + "is")?.Descendants(Main + "t").Select(t => t.Value) ?? []);

        var valor = c.Element(Main + "v")?.Value ?? string.Empty;
        return tipo switch
        {
            "s" => int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < compartidos.Count
                ? compartidos[i]
                : string.Empty,
            "b" => valor == "1" ? "VERDADERO" : "FALSO",
            _ => valor
        };
    }

    private static XDocument? CargarXml(ZipArchive zip, string ruta)
    {
        var entrada = zip.GetEntry(ruta);
        if (entrada is null) return null;
        if (entrada.Length > MaxBytesParte)
            throw new InvalidDataException("El archivo es demasiado grande.");

        using var flujo = entrada.Open();
        using var lector = XmlReader.Create(flujo, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        return XDocument.Load(lector);
    }

    /// <summary>"C12" → 2 (base 0).</summary>
    private static int? IndiceColumna(string? referencia)
    {
        if (string.IsNullOrEmpty(referencia)) return null;

        var indice = 0;
        var letras = 0;
        foreach (var ch in referencia)
        {
            if (ch is < 'A' or > 'Z') break;
            indice = indice * 26 + (ch - 'A' + 1);
            letras++;
        }
        return letras is 0 or > 3 ? null : indice - 1;
    }

    /// <summary>2 → "C".</summary>
    private static string LetrasColumna(int indice)
    {
        var letras = string.Empty;
        for (var n = indice + 1; n > 0; n = (n - 1) / 26)
            letras = (char)('A' + (n - 1) % 26) + letras;
        return letras;
    }

    /// <summary>Excel limita el nombre de la hoja a 31 caracteres y prohíbe : \ / ? * [ ].</summary>
    private static string NombreHoja(string nombre)
    {
        var limpio = new string(nombre.Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray()).Trim();
        if (limpio.Length == 0) limpio = "Hoja";
        return limpio.Length > 31 ? limpio[..31] : limpio;
    }

    private static string Escapar(string texto)
    {
        // Caracteres de control no permitidos en XML 1.0
        var valido = new string(texto.Where(ch => ch is '\t' or '\n' or '\r' || ch >= ' ').ToArray());
        return SecurityElement.Escape(valido);
    }
}
