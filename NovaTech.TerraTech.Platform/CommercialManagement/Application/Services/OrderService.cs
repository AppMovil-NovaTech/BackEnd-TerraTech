using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
public class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (command.UserId <= 0 || command.Quantity <= 0)
                return Result<Order>.Failure(CommercialError.InvalidOrderStatus, "The provided order data is invalid");

            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
                return Result<Order>.Failure(CommercialError.InvalidProductId, "The specified product was not found");

            var inventory = await inventoryRepository.FindByProductIdAsync(command.ProductId, cancellationToken);
            if (inventory == null || inventory.StockQuantity < command.Quantity)
                return Result<Order>.Failure(CommercialError.InsufficientStock, "Insufficient stock available for the requested product");

            inventory.DiscountStock(command.Quantity);
            inventoryRepository.Update(inventory);

            var order = new Order(command, product);
            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Order>.Success(order);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrent stock change while creating order for ProductId {ProductId}", command.ProductId);
            return Result<Order>.Failure(CommercialError.StockConflict, "Stock changed concurrently; please retry the order");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating order for ProductId {ProductId}", command.ProductId);
            return Result<Order>.Failure(CommercialError.InvalidOrderStatus, "The provided order data is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating order for ProductId {ProductId}", command.ProductId);
            return Result<Order>.Failure(CommercialError.UnexpectedError, "An unexpected error occurred while creating the order");
        }
    }

    public async Task<Result<Order>> Handle(ValidateOrderCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
            if (order == null)
                return Result<Order>.Failure(CommercialError.NotFound, "The order was not found");
            order.Validate();
            orderRepository.Update(order);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Order>.Success(order);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Validation failed for order {OrderId}", command.OrderId);
            return Result<Order>.Failure(CommercialError.OrderValidationFailed, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error validating order {OrderId}", command.OrderId);
            return Result<Order>.Failure(CommercialError.UnexpectedError, "An unexpected error occurred while validating the order");
        }
    }

    public async Task<Result<Order>> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
            if (order == null)
                return Result<Order>.Failure(CommercialError.NotFound, "The order was not found");

            var previousStatus = order.Status;
            order.UpdateStatus(command.NewStatus);

            if (previousStatus != OrderStatus.Cancelled && command.NewStatus == OrderStatus.Cancelled)
            {
                var inventory = await inventoryRepository.FindByProductIdAsync(order.ProductId, cancellationToken);
                if (inventory != null)
                {
                    inventory.RestoreStock(order.Quantity);
                    inventoryRepository.Update(inventory);
                }
            }

            orderRepository.Update(order);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Order>.Success(order);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrent stock change while updating order {OrderId}", command.OrderId);
            return Result<Order>.Failure(CommercialError.StockConflict, "Stock changed concurrently; please retry the status update");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid status update for order {OrderId}", command.OrderId);
            return Result<Order>.Failure(CommercialError.InvalidOrderStatus, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating order status {OrderId}", command.OrderId);
            return Result<Order>.Failure(CommercialError.UnexpectedError, "An unexpected error occurred while updating the order status");
        }
    }

    public async Task<IEnumerable<Order>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken = default)
    {
        return await orderRepository.ListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> Handle(GetOrdersByProfileQuery query, CancellationToken cancellationToken = default)
    {
        return await orderRepository.FindByProfileIdAsync(query.ProfileId, cancellationToken);
    }

    public async Task<Order?> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await orderRepository.FindByIdAsync(query.Id, cancellationToken);
    }
}
