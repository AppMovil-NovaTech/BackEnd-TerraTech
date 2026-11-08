using System.Data;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;
public static class MigrationPreflight
{
    public static async Task Check(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            async Task<long> Count(string sql)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                return Convert.ToInt64(await command.ExecuteScalarAsync());
            }

            if (await Count("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'users'") == 0)
                return;
            var ordersUseUserId = await Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'orders' AND column_name = 'user_id'") > 0;
            var commentsUseUserId = await Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'comments' AND column_name = 'user_id'") > 0;
            var checks = new Dictionary<string, string>
            {
                ["profiles without users"] = "SELECT COUNT(*) FROM profiles p LEFT JOIN users u ON u.id=p.user_id WHERE u.id IS NULL",
                ["duplicate profiles per user"] = "SELECT COUNT(*) FROM (SELECT user_id FROM profiles GROUP BY user_id HAVING COUNT(*)>1) d",
                ["fields without profiles"] = "SELECT COUNT(*) FROM fields f LEFT JOIN profiles p ON p.id=f.profile_id WHERE p.id IS NULL",
                ["devices without fields"] = "SELECT COUNT(*) FROM devices d LEFT JOIN fields f ON f.id=d.field_id WHERE f.id IS NULL",
                ["duplicate normalized device MACs"] = "SELECT COUNT(*) FROM (SELECT UPPER(REPLACE(mac_address,'-',':')) FROM devices GROUP BY UPPER(REPLACE(mac_address,'-',':')) HAVING COUNT(*)>1) d",
                ["device IDs outside sensor code range"] = "SELECT COUNT(*) FROM devices WHERE id >= 2176782336",
                ["reports without devices"] = "SELECT COUNT(*) FROM reports r LEFT JOIN devices d ON d.id=r.device_id WHERE d.id IS NULL",
                ["orders without users"] = ordersUseUserId
                    ? "SELECT COUNT(*) FROM orders o LEFT JOIN users u ON u.id=o.user_id WHERE u.id IS NULL"
                    : "SELECT COUNT(*) FROM orders o LEFT JOIN profiles p ON p.id=o.profile_id WHERE p.id IS NULL",
                ["notifications without profiles"] = "SELECT COUNT(*) FROM notifications n LEFT JOIN profiles p ON p.id=n.profile_id WHERE p.id IS NULL",
                ["community profiles without profiles"] = "SELECT COUNT(*) FROM community_profiles c LEFT JOIN profiles p ON p.id=c.profile_id WHERE p.id IS NULL",
                ["comments without users"] = commentsUseUserId
                    ? "SELECT COUNT(*) FROM comments c LEFT JOIN users u ON u.id=c.user_id LEFT JOIN profiles t ON t.id=c.target_profile_id WHERE u.id IS NULL OR t.id IS NULL"
                    : "SELECT COUNT(*) FROM comments c LEFT JOIN profiles a ON a.id=c.author_profile_id LEFT JOIN profiles t ON t.id=c.target_profile_id WHERE a.id IS NULL OR t.id IS NULL"
            };
            var failures = new List<string>();
            foreach (var(label, sql)in checks)
            {
                var count = await Count(sql);
                if (count > 0)
                    failures.Add($"{label}: {count}");
            }

            if (failures.Count > 0)
                throw new InvalidOperationException("Migration stopped. Resolve existing data manually, without deleting information automatically: " + string.Join("; ", failures));
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
