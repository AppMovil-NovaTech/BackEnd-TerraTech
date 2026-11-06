using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
public class OrderRepository(AppDbContext context) : BaseRepository<Order>(context), IOrderRepository
{
    public async Task<IEnumerable<Order>> FindByProfileIdAsync(int profileId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Order>().Where(o => o.UserId == profileId).OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> FindByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Order>().Where(o => o.Status == status).OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }
}
