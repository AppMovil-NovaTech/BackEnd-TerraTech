using MySql.Data.MySqlClient;
using Npgsql;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;

public static class DatabaseErrors
{
    public static bool IsUniqueViolation(Exception? exception) => exception switch
    {
        MySqlException { Number: 1062 } => true,
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        _ => exception?.InnerException is not null && IsUniqueViolation(exception.InnerException)
    };

    public static bool IsConstraintViolation(Exception? exception) => exception switch
    {
        MySqlException { Number: 1062 or 1451 or 1452 } => true,
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation } => true,
        _ => exception?.InnerException is not null && IsConstraintViolation(exception.InnerException)
    };
}
