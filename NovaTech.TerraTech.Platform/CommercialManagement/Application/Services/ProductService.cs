using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.NotificationManagement.Application.Services;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Application.Model;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Tb1;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
public class ProductService(IProductRepository productRepository, IInventoryRepository inventoryRepository, INotificationService notificationService, IUnitOfWork unitOfWork, IFavoriteProductService favoriteService, IProductPreferenceService preferenceService, IHttpContextAccessor httpContextAccessor, ILogger<ProductService> logger) : IProductService
{
    public async Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = new Product(command);
            await productRepository.AddAsync(product, cancellationToken);
            var publishingUserId = httpContextAccessor.HttpContext?.User.UserId() ?? 0;
            await preferenceService.QueueNewProductAsync(
                product.Name, product.Type, publishingUserId > 0 ? publishingUserId : null, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Product>.Success(product);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid arguments while creating product");
            return Result<Product>.Failure(CommercialError.InvalidProductId, "The provided product data is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating product");
            return Result<Product>.Failure(CommercialError.UnexpectedError, "An unexpected error occurred while creating the product");
        }
    }

    public async Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        return await productRepository.ListAsync(cancellationToken);
    }

    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await productRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<Result<Product>> Handle(RegisterProductConsultationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ProductId <= 0)
            return Result<Product>.Failure(CommercialError.InvalidProductId, "Invalid product identifier");

        try
        {
            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
                return Result<Product>.Failure(CommercialError.NotFound, "Product not found");

            product.RegisterConsultation(DateTimeOffset.UtcNow);

            if (product.ShouldNotifyHighInterest())
            {
                var inventory = await inventoryRepository.FindByProductIdAsync(product.Id, cancellationToken);
                if (inventory != null && inventory.ProfileId > 0)
                {
                    product.MarkHighInterestAlertSent();
                    productRepository.Update(product);

                    var notification = await notificationService.Handle(
                        new CreateNotificationCommand(
                            inventory.ProfileId,
                            "Interes alto en producto",
                            $"El producto '{product.Name}' ha recibido {product.MonthlyConsultations} consultas este mes. Revisa su disponibilidad.",
                            true),
                        cancellationToken);

                    if (!notification.IsSuccess)
                        return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not create the product interest alert");
                }
            }

            productRepository.Update(product);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering consultation for product {Id}", command.ProductId);
            return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not register the consultation");
        }
    }

    public async Task<Result<Product>> Handle(ConfigureProductInterestThresholdCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ProductId <= 0 || command.Threshold <= 0)
            return Result<Product>.Failure(CommercialError.InvalidProductId, "Invalid product or threshold");

        try
        {
            var inventory = await inventoryRepository.FindByProductIdAsync(command.ProductId, cancellationToken);
            if (command.UserId <= 0 || inventory == null || inventory.OwnerUserId != command.UserId)
                return Result<Product>.Failure(CommercialError.NotFound, "Product inventory not found for this user");

            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
                return Result<Product>.Failure(CommercialError.NotFound, "Product not found");

            product.ConfigureInterestThreshold(command.Threshold);
            productRepository.Update(product);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error configuring interest threshold for product {Id}", command.ProductId);
            return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not configure the threshold");
        }
    }

    public async Task<Result<Product>> Handle(GetProductInterestQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var inventory = await inventoryRepository.FindByProductIdAsync(query.ProductId, cancellationToken);
            if (query.UserId <= 0 || inventory == null || inventory.OwnerUserId != query.UserId)
                return Result<Product>.Failure(CommercialError.NotFound, "Product inventory not found for this user");

            var product = await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
            if (product == null)
                return Result<Product>.Failure(CommercialError.NotFound, "Product not found");

            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading interest for product {Id}", query.ProductId);
            return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not load product interest");
        }
    }

    public async Task<Result<Product>> Handle(CheckProductInactivityCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ProductId <= 0 || command.UserId <= 0)
            return Result<Product>.Failure(CommercialError.NotFound, "Product inventory not found for this user");

        try
        {
            var inventory = await inventoryRepository.FindByProductIdAsync(command.ProductId, cancellationToken);
            if (inventory == null || inventory.OwnerUserId != command.UserId || inventory.ProfileId <= 0)
                return Result<Product>.Failure(CommercialError.NotFound, "Product inventory not found for this user");

            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product == null)
                return Result<Product>.Failure(CommercialError.NotFound, "Product not found");

            if (!product.ShouldNotifyInactivity(DateTimeOffset.UtcNow))
                return Result<Product>.Success(product);

            product.MarkInactivityAlertSent();
            productRepository.Update(product);

            var notification = await notificationService.Handle(
                new CreateNotificationCommand(
                    inventory.ProfileId,
                    "Producto sin consultas",
                    $"El producto '{product.Name}' no ha recibido consultas durante los ultimos 30 dias. Revisa su publicacion o promueve el producto.",
                    true),
                cancellationToken);

            if (!notification.IsSuccess)
                return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not create the product inactivity alert");

            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking inactivity for product {Id}", command.ProductId);
            return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not check product inactivity");
        }
    }
    public async Task<Result<Product>> Handle(UpdateProductPriceCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ProductId <= 0 || command.UserId <= 0 || command.Price <= 0)
            return Result<Product>.Failure(CommercialError.InvalidProductId, "Invalid product or price");
        try
        {
            var inventory = await inventoryRepository.FindByProductIdAsync(command.ProductId, cancellationToken);
            if (inventory is null || inventory.OwnerUserId != command.UserId)
                return Result<Product>.Failure(CommercialError.NotFound, "Product inventory not found for the current user");
            var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
            if (product is null)
                return Result<Product>.Failure(CommercialError.NotFound, "Product not found");
            var previousPrice = product.Price;
            if (previousPrice == command.Price && inventory.Price == command.Price)
                return Result<Product>.Success(product);
            product.UpdatePrice(command.Price);
            inventory.UpdatePrice(command.Price);
            productRepository.Update(product);
            inventoryRepository.Update(inventory);
            await favoriteService.QueuePriceChangeAsync(product.Id, product.Name, previousPrice, command.Price, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed updating price for product {ProductId}", command.ProductId);
            return Result<Product>.Failure(CommercialError.UnexpectedError, "Could not update product price");
        }
    }
}
