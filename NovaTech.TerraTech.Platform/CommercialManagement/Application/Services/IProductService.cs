using NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Queries;
using NovaTech.TerraTech.Platform.Shared.Application.Model;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
public interface IProductService
{
    Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> Handle(GetAllProductsQuery query, CancellationToken cancellationToken = default);
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken = default);
    Task<Result<Product>> Handle(RegisterProductConsultationCommand command, CancellationToken cancellationToken = default);
    Task<Result<Product>> Handle(ConfigureProductInterestThresholdCommand command, CancellationToken cancellationToken = default);
    Task<Result<Product>> Handle(GetProductInterestQuery query, CancellationToken cancellationToken = default);

    Task<Result<Product>> Handle(CheckProductInactivityCommand command, CancellationToken cancellationToken = default);
}
