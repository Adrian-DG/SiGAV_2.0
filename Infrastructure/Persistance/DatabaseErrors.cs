using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance;

/// <summary>
/// Reconoce violaciones de restricciones en SQLite y SQL Server. SQL Server se detecta por nombre de
/// tipo para no depender de Microsoft.Data.SqlClient mientras el proyecto use SQLite.
/// </summary>
internal static class DatabaseErrors
{
    private const int SqliteConstraintUnique = 2067;
    private const int SqliteConstraintPrimaryKey = 1555;
    private const int SqliteConstraintForeignKey = 787;

    private static readonly int[] SqlServerUnique = [2601, 2627];
    private const int SqlServerForeignKey = 547;

    public static bool IsUniqueViolation(DbUpdateException exception) => exception.InnerException switch
    {
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode is SqliteConstraintUnique or SqliteConstraintPrimaryKey,
        { } other => SqlServerUnique.Contains(SqlServerNumber(other)),
        _ => false
    };

    public static bool IsForeignKeyViolation(DbUpdateException exception) => exception.InnerException switch
    {
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode == SqliteConstraintForeignKey,
        { } other => SqlServerNumber(other) == SqlServerForeignKey,
        _ => false
    };

    private static int SqlServerNumber(Exception exception)
        => exception.GetType().FullName == "Microsoft.Data.SqlClient.SqlException"
           && exception.GetType().GetProperty("Number")?.GetValue(exception) is int number
            ? number
            : -1;
}
