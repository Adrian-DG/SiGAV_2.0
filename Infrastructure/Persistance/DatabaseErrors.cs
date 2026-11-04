using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance;

/// <summary>Reconoce violaciones de restricciones de SQL Server.</summary>
internal static class DatabaseErrors
{
    // 2601: índice único; 2627: restricción UNIQUE / PRIMARY KEY
    private static readonly int[] SqlServerUnique = [2601, 2627];
    private const int SqlServerForeignKey = 547;

    public static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sql && SqlServerUnique.Contains(sql.Number);

    public static bool IsForeignKeyViolation(DbUpdateException exception)
        => exception.InnerException is SqlException { Number: SqlServerForeignKey };
}
