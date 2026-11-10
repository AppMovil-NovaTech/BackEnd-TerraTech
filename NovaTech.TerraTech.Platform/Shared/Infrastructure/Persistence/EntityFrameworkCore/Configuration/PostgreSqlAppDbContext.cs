using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options, IHttpContextAccessor? accessor = null)
    : AppDbContext(options, accessor)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("terratech");
        base.OnModelCreating(builder);

        var offsetConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            value => value.ToUniversalTime(), value => value);
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.GetColumnType() == "longtext")
                    property.SetColumnType("text");
                if (property.GetColumnType() == "double")
                    property.SetColumnType("double precision");
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                    property.SetValueConverter(offsetConverter);
            }
        }
    }
}
