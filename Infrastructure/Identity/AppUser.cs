using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entities.Misc;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class AppUser : IdentityUser<int>
{
    public required string Identificacion { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public SexoEnum Sexo { get; set; }

    public InstitucionEnum Institucion { get; set; }

    [ForeignKey(nameof(Rango))]
    public int RangoId { get; set; }
    public virtual Rango? Rango { get; set; }

    [ForeignKey(nameof(Departamento))]
    public int DepartamentoId { get; set; }
    public virtual Departamento? Departamento { get; set; }
    
    private string? RangoOficial => Institucion == InstitucionEnum.ARD ? Rango?.NombreArmada : Rango?.Nombre ?? string.Empty;
    public string UsuarioInfo => $"{RangoOficial}, {Apellido} {Nombre}".Trim();
}