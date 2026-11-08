using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.StockManagement.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
public class InventoryRepository(AppDbContext context) : BaseRepository<Inventory>(context), IInventoryRepository
{
    public async Task<Inventory?> FindByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Inventory>().IgnoreQueryFilters().FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IEnumerable<Inventory>> FindByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Inventory>().Where(i => i.FieldId == fieldId).ToListAsync(cancellationToken);
    }
}
