using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.CommunityManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.ValueObjects;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST;
using NovaTech.TerraTech.Platform.CommunityManagement.Interfaces.REST.Resources;
using NovaTech.TerraTech.Platform.Iam.Interface.Acl;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.Shared.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;
using Xunit;

namespace TerraTech.IntegrationTests;

public class ProductReviewsAndReputationTests
{
    #region Test Fakes

    private class FakeCommentRepository : ICommentRepository
    {
        private readonly List<Comment> _comments = [];

        public void Seed(IEnumerable<Comment> comments) => _comments.AddRange(comments);

        public Task AddAsync(Comment entity, CancellationToken cancellationToken = default)
        {
            _comments.Add(entity);
            return Task.CompletedTask;
        }

        public Task<Comment?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_comments.FirstOrDefault(c => c.Id == id));
        }

        public void Update(Comment entity) { }

        public void Remove(Comment entity) => _comments.Remove(entity);

        public Task<IEnumerable<Comment>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<Comment>>(_comments.ToList());
        }

        public Task<IEnumerable<Comment>> FindByTargetProfileIdAsync(int targetProfileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<Comment>>(
                _comments.Where(c => c.TargetProfileId == targetProfileId).ToList());
        }

        public Task<IEnumerable<Comment>> FindByProductIdAsync(int productId, int? rating = null, CancellationToken cancellationToken = default)
        {
            var query = _comments.Where(c => c.ProductId == productId);
            if (rating.HasValue)
            {
                query = query.Where(c => c.Rating == rating.Value);
            }
            var sorted = query.OrderByDescending(c => c.CreatedAt).ToList();
            return Task.FromResult<IEnumerable<Comment>>(sorted);
        }

        public Task<ProductRatingSummary> GetRatingSummaryByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var ratings = _comments
                .Where(c => c.ProductId == productId)
                .Select(c => c.Rating)
                .ToList();

            if (ratings.Count == 0)
            {
                return Task.FromResult(new ProductRatingSummary(productId, 0.0, 0, 0, 0, 0, 0, 0));
            }

            var averageRating = Math.Round(ratings.Average(), 1);
            var totalReviews = ratings.Count;
            var oneStar = ratings.Count(r => r == 1);
            var twoStars = ratings.Count(r => r == 2);
            var threeStars = ratings.Count(r => r == 3);
            var fourStars = ratings.Count(r => r == 4);
            var fiveStars = ratings.Count(r => r == 5);

            var summary = new ProductRatingSummary(
                productId,
                averageRating,
                totalReviews,
                oneStar,
                twoStars,
                threeStars,
                fourStars,
                fiveStars);

            return Task.FromResult(summary);
        }
    }

    private class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<int, Product> _products = new();

        public void Seed(Product product) => _products[product.Id] = product;

        public Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            _products[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task<Product?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public Task<Product?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Values.FirstOrDefault(p => p.Name == name));
        }

        public void Update(Product entity) { }
        public void Remove(Product entity) => _products.Remove(entity.Id);
        public Task<IEnumerable<Product>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Product>>(_products.Values.ToList());
    }

    private class FakeIamContextFacade : IIamContextFacade
    {
        private readonly Dictionary<int, string> _names = new();

        public void SeedUser(int userId, string fullName) => _names[userId] = fullName;

        public Task<int> CreateUser(string email, string password, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<int> FetchUserByEmail(string email, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<string> FetchEmailByUserId(int userId, CancellationToken cancellationToken) => Task.FromResult(string.Empty);

        public Task<string?> FetchFullNameByUserId(int userId, CancellationToken cancellationToken = default)
        {
            _names.TryGetValue(userId, out var name);
            return Task.FromResult(name);
        }

        public Task<IDictionary<int, string>> FetchFullNamesByUserIds(IEnumerable<int> userIds, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<int, string>();
            foreach (var id in userIds)
            {
                if (_names.TryGetValue(id, out var name))
                {
                    dict[id] = name;
                }
            }
            return Task.FromResult<IDictionary<int, string>>(dict);
        }
    }

    private class FakeInventoryRepository : IInventoryRepository
    {
        public Task AddAsync(Inventory entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Inventory?> FindByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Inventory?>(null);
        public void Update(Inventory entity) { }
        public void Remove(Inventory entity) { }
        public Task<IEnumerable<Inventory>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Inventory>>([]);
        public Task<Inventory?> FindByProductIdAsync(int productId, CancellationToken cancellationToken = default) => Task.FromResult<Inventory?>(null);
        public Task<IEnumerable<Inventory>> FindByFieldIdAsync(int fieldId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Inventory>>([]);
    }

    private class FakeProfileRepository : IProfileRepository
    {
        public Task AddAsync(Profile entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Profile?> FindByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Profile?>(null);
        public void Update(Profile entity) { }
        public void Remove(Profile entity) { }
        public Task<IEnumerable<Profile>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Profile>>([]);
        public Task<Profile?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<Profile?>(null);
    }

    private class FakeUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public Task CompleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IUnitOfWorkTransaction>(new FakeUnitOfWorkTransaction());
    }

    #endregion

    #region Helpers

    private static Comment CreateCommentEntity(int id, int userId, int productId, int targetProfileId, string content, int rating, DateTimeOffset createdAt)
    {
        var comment = new Comment(new CreateCommentCommand(userId, productId, targetProfileId, content, rating));
        typeof(Comment).GetProperty(nameof(Comment.Id))!.SetValue(comment, id);
        comment.CreatedAt = createdAt;
        return comment;
    }

    private static Product CreateProductEntity(int id, string name = "Fertilizante Orgánico")
    {
        var product = new Product(new CreateProductCommand(name, "Descripción del fertilizante", 25.50m, "FERTILIZER", "http://example.com/item.png"));
        typeof(Product).GetProperty(nameof(Product.Id))!.SetValue(product, id);
        return product;
    }

    private (CommentsController controller, FakeCommentRepository comments, FakeProductRepository products, FakeIamContextFacade iam) CreateSystemUnderTest()
    {
        var commentRepo = new FakeCommentRepository();
        var productRepo = new FakeProductRepository();
        var iamFacade = new FakeIamContextFacade();
        var inventoryRepo = new FakeInventoryRepository();
        var profileRepo = new FakeProfileRepository();
        var unitOfWork = new FakeUnitOfWork();

        var commentService = new CommentService(
            commentRepo,
            productRepo,
            inventoryRepo,
            profileRepo,
            unitOfWork,
            NullLogger<CommentService>.Instance);

        var controller = new CommentsController(
            commentService,
            iamFacade,
            NullLogger<CommentsController>.Instance);

        return (controller, commentRepo, productRepo, iamFacade);
    }

    #endregion

    #region Test Cases

    [Fact]
    public async Task GetCommentsByProductId_ShouldReturnCommentsInDescendingChronologicalOrder()
    {
        // Arrange
        var (controller, commentRepo, productRepo, iamFacade) = CreateSystemUnderTest();
        const int productId = 100;
        productRepo.Seed(CreateProductEntity(productId));

        iamFacade.SeedUser(1, "Carlos Santana");
        iamFacade.SeedUser(2, "Mariana Ruiz");

        var c1 = CreateCommentEntity(1, 1, productId, 10, "Comentario antiguo", 4, DateTimeOffset.UtcNow.AddDays(-5));
        var c2 = CreateCommentEntity(2, 2, productId, 10, "Comentario más reciente", 5, DateTimeOffset.UtcNow.AddDays(-1));
        var c3 = CreateCommentEntity(3, 1, productId, 10, "Comentario intermedio", 3, DateTimeOffset.UtcNow.AddDays(-3));
        commentRepo.Seed([c1, c2, c3]);

        // Act
        var response = await controller.GetCommentsByProductId(productId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var resources = Assert.IsAssignableFrom<IEnumerable<CommentResource>>(okResult.Value).ToList();

        Assert.Equal(3, resources.Count);
        // Debe estar ordenado de más reciente a más antiguo: c2 (-1 día), c3 (-3 días), c1 (-5 días)
        Assert.Equal(2, resources[0].Id);
        Assert.Equal("Mariana Ruiz", resources[0].AuthorName);

        Assert.Equal(3, resources[1].Id);
        Assert.Equal("Carlos Santana", resources[1].AuthorName);

        Assert.Equal(1, resources[2].Id);
        Assert.Equal("Carlos Santana", resources[2].AuthorName);
    }

    [Fact]
    public async Task GetCommentsByProductId_WithRatingFilter_ShouldReturnOnlyFilteredRatings()
    {
        // Arrange
        var (controller, commentRepo, productRepo, iamFacade) = CreateSystemUnderTest();
        const int productId = 101;
        productRepo.Seed(CreateProductEntity(productId));

        iamFacade.SeedUser(1, "Usuario 1");
        iamFacade.SeedUser(2, "Usuario 2");

        var c1 = CreateCommentEntity(1, 1, productId, 10, "Excelente", 5, DateTimeOffset.UtcNow.AddHours(-1));
        var c2 = CreateCommentEntity(2, 2, productId, 10, "Regular", 3, DateTimeOffset.UtcNow.AddHours(-2));
        var c3 = CreateCommentEntity(3, 1, productId, 10, "Magnífico producto", 5, DateTimeOffset.UtcNow.AddHours(-3));
        var c4 = CreateCommentEntity(4, 2, productId, 10, "Bueno", 4, DateTimeOffset.UtcNow.AddHours(-4));
        commentRepo.Seed([c1, c2, c3, c4]);

        // Act
        var response = await controller.GetCommentsByProductId(productId, 5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var resources = Assert.IsAssignableFrom<IEnumerable<CommentResource>>(okResult.Value).ToList();

        Assert.Equal(2, resources.Count);
        Assert.All(resources, r => Assert.Equal(5, r.Rating));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task GetCommentsByProductId_WithInvalidRating_ShouldReturnBadRequest(int invalidRating)
    {
        // Arrange
        var (controller, _, productRepo, _) = CreateSystemUnderTest();
        const int productId = 102;
        productRepo.Seed(CreateProductEntity(productId));

        // Act
        var response = await controller.GetCommentsByProductId(productId, invalidRating, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Rating filter must be between 1 and 5", badRequest.Value);
    }

    [Fact]
    public async Task GetCommentsByProductId_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (controller, _, _, _) = CreateSystemUnderTest();
        const int nonExistentProductId = 999;

        // Act
        var response = await controller.GetCommentsByProductId(nonExistentProductId, null, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("Product not found", notFound.Value);
    }

    [Fact]
    public async Task GetCommentsByProductId_WhenNoCommentsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var (controller, _, productRepo, _) = CreateSystemUnderTest();
        const int productId = 103;
        productRepo.Seed(CreateProductEntity(productId));

        // Act
        var response = await controller.GetCommentsByProductId(productId, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var resources = Assert.IsAssignableFrom<IEnumerable<CommentResource>>(okResult.Value);
        Assert.Empty(resources);
    }

    [Fact]
    public async Task GetProductRatingSummary_WithReviews_ShouldCalculateAccurateAverageAndBreakdown()
    {
        // Arrange
        var (controller, commentRepo, productRepo, _) = CreateSystemUnderTest();
        const int productId = 104;
        productRepo.Seed(CreateProductEntity(productId));

        // Ratings: 5, 5, 4, 3, 1 -> total 5, suma 18 -> promedio 3.6
        var c1 = CreateCommentEntity(1, 1, productId, 10, "5 estrellas A", 5, DateTimeOffset.UtcNow);
        var c2 = CreateCommentEntity(2, 2, productId, 10, "5 estrellas B", 5, DateTimeOffset.UtcNow);
        var c3 = CreateCommentEntity(3, 3, productId, 10, "4 estrellas", 4, DateTimeOffset.UtcNow);
        var c4 = CreateCommentEntity(4, 4, productId, 10, "3 estrellas", 3, DateTimeOffset.UtcNow);
        var c5 = CreateCommentEntity(5, 5, productId, 10, "1 estrella", 1, DateTimeOffset.UtcNow);
        commentRepo.Seed([c1, c2, c3, c4, c5]);

        // Act
        var response = await controller.GetProductRatingSummary(productId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var summary = Assert.IsType<ProductRatingSummaryResource>(okResult.Value);

        Assert.Equal(productId, summary.ProductId);
        Assert.Equal(3.6, summary.AverageRating);
        Assert.Equal(5, summary.TotalReviews);

        Assert.Equal(1, summary.RatingBreakdown.OneStar);
        Assert.Equal(0, summary.RatingBreakdown.TwoStars);
        Assert.Equal(1, summary.RatingBreakdown.ThreeStars);
        Assert.Equal(1, summary.RatingBreakdown.FourStars);
        Assert.Equal(2, summary.RatingBreakdown.FiveStars);
    }

    [Fact]
    public async Task GetProductRatingSummary_WithZeroReviews_ShouldReturnZeroDefaults()
    {
        // Arrange
        var (controller, _, productRepo, _) = CreateSystemUnderTest();
        const int productId = 105;
        productRepo.Seed(CreateProductEntity(productId));

        // Act
        var response = await controller.GetProductRatingSummary(productId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var summary = Assert.IsType<ProductRatingSummaryResource>(okResult.Value);

        Assert.Equal(productId, summary.ProductId);
        Assert.Equal(0.0, summary.AverageRating);
        Assert.Equal(0, summary.TotalReviews);

        Assert.Equal(0, summary.RatingBreakdown.OneStar);
        Assert.Equal(0, summary.RatingBreakdown.TwoStars);
        Assert.Equal(0, summary.RatingBreakdown.ThreeStars);
        Assert.Equal(0, summary.RatingBreakdown.FourStars);
        Assert.Equal(0, summary.RatingBreakdown.FiveStars);
    }

    [Fact]
    public async Task GetProductRatingSummary_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (controller, _, _, _) = CreateSystemUnderTest();
        const int nonExistentProductId = 999;

        // Act
        var response = await controller.GetProductRatingSummary(nonExistentProductId, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("Product not found", notFound.Value);
    }

    #endregion
}
