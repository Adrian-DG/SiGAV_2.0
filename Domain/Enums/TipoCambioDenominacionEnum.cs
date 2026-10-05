namespace Domain.Enums;

public enum TipoCambioDenominacionEnum
{
    /// <summary>La unidad no tenía denominación y se le asignó una.</summary>
    Asignacion = 1,

    /// <summary>La unidad cambió de una denominación a otra.</summary>
    Reasignacion = 2,

    /// <summary>La unidad perdió su denominación porque otra unidad la tomó.</summary>
    Liberacion = 3,

    /// <summary>Se registró la unidad (desde el front desk). Si se le asigna una denominación, va en otro registro.</summary>
    Alta = 4
}
