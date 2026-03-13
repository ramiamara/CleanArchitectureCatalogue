namespace Catalog.Tests.Services;

using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Cache;
using Catalog.Infrastructure.Repositories;
using FluentAssertions;
using Moq;

public class CatalogueServiceTests
{
    private readonly Mock<IRepository<Catalogue>> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();

    private CatalogueService CreateSut() => new(_repoMock.Object, _uowMock.Object, _cacheMock.Object);

    [Fact]
    public async Task GetAllAsync_ReturnsFromCache_WhenCacheHit()
    {
        var cached = new PagedResult<CatalogueDto>(new[] { new CatalogueDto { Name = "Cached" } }, 1, 20, 1);
        _cacheMock.Setup(c => c.Get<PagedResult<CatalogueDto>>("catalogues:page:1:size:20")).Returns(cached);

        var result = await CreateSut().GetAllAsync(1, 20);

        result.TotalCount.Should().Be(1);
        result.Items.First().Name.Should().Be("Cached");
        _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_QueriesRepo_WhenCacheMiss()
    {
        _cacheMock.Setup(c => c.Get<PagedResult<CatalogueDto>>(It.IsAny<string>())).Returns((PagedResult<CatalogueDto>?)null);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Catalogue>
        {
            new() { Name = "Electronique", IsActive = true, CreatedBy = "test" },
            new() { Name = "Sport", IsActive = true, CreatedBy = "test" }
        });

        var result = await CreateSut().GetAllAsync(1, 20);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        _cacheMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<PagedResult<CatalogueDto>>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Theory]
    [InlineData(1, 1, 3, 3, 3)]
    [InlineData(2, 2, 5, 5, 3)]
    [InlineData(1, 20, 8, 8, 1)]
    public async Task GetAllAsync_Paginates_Correctly(int page, int pageSize, int totalItems, int expectedTotal, int expectedTotalPages)
    {
        _cacheMock.Setup(c => c.Get<PagedResult<CatalogueDto>>(It.IsAny<string>())).Returns((PagedResult<CatalogueDto>?)null);
        var entities = Enumerable.Range(1, totalItems).Select(i => new Catalogue { Name = $"Cat{i}", IsActive = true, CreatedBy = "test" }).ToList<Catalogue>();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

        var result = await CreateSut().GetAllAsync(page, pageSize);

        result.TotalCount.Should().Be(expectedTotal);
        result.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _cacheMock.Setup(c => c.Get<CatalogueDto>(It.IsAny<string>())).Returns((CatalogueDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Catalogue?)null);

        var result = await CreateSut().GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var id = Guid.NewGuid();
        var entity = new Catalogue { Id = id, Name = "Sport", IsActive = true, CreatedBy = "test" };
        _cacheMock.Setup(c => c.Get<CatalogueDto>(It.IsAny<string>())).Returns((CatalogueDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

        var result = await CreateSut().GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Sport");
    }

    [Fact]
    public async Task CreateAsync_AddsToRepo_AndInvalidatesCache()
    {
        var request = new CreateCatalogueRequest { Name = "Mode", Description = "Vetements" };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Catalogue>())).Returns(Task.CompletedTask);
        _uowMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(request, "admin");

        result.Name.Should().Be("Mode");
        _cacheMock.Verify(c => c.RemoveByPrefix("catalogues"), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Catalogue?)null);
        var act = async () => await CreateSut().DeleteAsync(Guid.NewGuid(), "admin");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PagedResult_TotalPages_IsCorrect()
    {
        var r1 = new PagedResult<CatalogueDto>(Enumerable.Empty<CatalogueDto>(), 1, 10, 25);
        var r2 = new PagedResult<CatalogueDto>(Enumerable.Empty<CatalogueDto>(), 1, 10, 10);
        var r3 = new PagedResult<CatalogueDto>(Enumerable.Empty<CatalogueDto>(), 1, 10, 0);

        r1.TotalPages.Should().Be(3);
        r2.TotalPages.Should().Be(1);
        r3.TotalPages.Should().Be(0);
        r1.HasNextPage.Should().BeTrue();
        r1.HasPreviousPage.Should().BeFalse();
    }
}
