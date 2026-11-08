using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Application.Errors;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.StockManagement.Application.Services;
public class StockService(
    IInventoryRepository inventoryRepository,
    IProductService productService,
    IUnitOfWork unitOfWork,
    ILogger<StockService> logger) : IStockService
{
    public async Task<Result<Inventory>> Handle(CreateInventoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command.Name) ||
                command.FieldId <= 0 ||
                command.ProfileId <= 0 ||
                command.Quantity <= 0 ||
                string.IsNullOrWhiteSpace(command.UnitType) ||
                command.AverageWeightPerUnitKg <= 0 ||
                command.Price <= 0 ||
                string.IsNullOrWhiteSpace(command.ImageUrl))
            {
                return Result<Inventory>.Failure(StockError.InvalidStockQuantity, "The provided inventory data is invalid");
            }

            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            var createProductCommand = new CreateProductCommand(
                command.Name.Trim(),
                $"Agricultural harvest of {command.Name.Trim()} ({command.Quantity} {command.UnitType.Trim()})",
                command.Price,
                command.UnitType.Trim(),
                command.ImageUrl.Trim());

            var productResult = await productService.Handle(createProductCommand, cancellationToken);
            if (!productResult.IsSuccess || productResult.Value is null)
            {
                return Result<Inventory>.Failure(StockError.InvalidProductId, "Failed to create marketplace product for inventory");
            }

            var inventory = new Inventory(command, productResult.Value.Id);
            await inventoryRepository.AddAsync(inventory, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<Inventory>.Success(inventory);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating inventory for Product {Name}", command.Name);
            return Result<Inventory>.Failure(StockError.InvalidProductId, "The provided product data is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating inventory for Product {Name}", command.Name);
            return Result<Inventory>.Failure(StockError.UnexpectedError, "An unexpected error occurred while creating the inventory");
        }
    }

    public async Task<Result<Inventory>> Handle(UpdateInventoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var inventory = await inventoryRepository.FindByIdAsync(command.Id, cancellationToken);
            if (inventory == null)
                return Result<Inventory>.Failure(StockError.NotFound, "The inventory item was not found");
            if (command.StockQuantity < 0)
                return Result<Inventory>.Failure(StockError.InvalidStockQuantity, "Stock quantity cannot be negative");
            inventory.UpdateStock(command.StockQuantity);
            inventoryRepository.Update(inventory);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Inventory>.Success(inventory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating inventory {Id}", command.Id);
            return Result<Inventory>.Failure(StockError.UnexpectedError, "An unexpected error occurred while updating the inventory");
        }
    }

    public async Task<IEnumerable<Inventory>> Handle(GetAllInventoryQuery query, CancellationToken cancellationToken = default)
    {
        return await inventoryRepository.ListAsync(cancellationToken);
    }

    public async Task<Inventory?> Handle(GetInventoryByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await inventoryRepository.FindByIdAsync(query.Id, cancellationToken);
    }
}
