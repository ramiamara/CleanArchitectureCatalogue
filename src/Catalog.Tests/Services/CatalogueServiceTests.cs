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
    private readonly Mock<IRepository<Catalogue>> _repoMock  = new();
    private readonly Mock<ICacheService>          _cacheMock = new();
    private readonly ICatalogueService            _service;

    public CatalogueServiceTests()
    {
        _service = new CatalogueService(_repoMock.Object, _cacheMock.Object);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsCachedResult_WhenCacheHit()
    {
        var cached = new PagedResult<CatalogueDto>(
            new[] { new CatalogueDto { Name = "Cached" } }, 1, 10, 1);

        _cacheMock.Setup(c => c.Get<PagedResult<CatalogueDto>>(It.IsAny<string>()))
                  .Returns(cached);

        var result = await _service.GetAllAsync(1, 10);

        result.Should().BeEquivalentTo(cached);
        _repoMock.Verify(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_QueriesRepo_WhenCacheMiss()
    {
        _cacheMock.Setup(c => c.Get<PagedResult<CatalogueDto>>(It.IsAny<string>()))
                  .Returns((PagedResult<CatalogueDto>?)null);

        var entities = new List<Catalogue>
        {
            new() { Name = "Sport", IsActive = true, CreatedBy = "test" }
        };
        _repoMock.Setup(r => r.GetPagedAsync(1, 10, null))
                 .ReturnsAsync((entities, 1));

        var result = await _service.GetAllAsync(1, 10);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _repoMock.Verify(r => r.GetPagedAsync(1, 10, null), Times.Once);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _cacheMock.Setup(c => c.Get<CatalogueDto>(It.IsAny<string>()))
                  .Returns((CatalogueDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Catalogue?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var id     = Guid.NewGuid();
        var entity = new Catalogue { Name = "Tech", IsActive = true, CreatedBy = "admin" };

        _cacheMock.Setup(c => c.Get<CatalogueDto>(It.IsAny<string>()))
                  .Returns((CatalogueDto?)null);
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Tech");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AddsToRepo_AndInvalidatesCache()
    {
        var request = new CreateCatalogueRequest { Name = "Mode", Description = "Vetements" };

        var dto = await _service.CreateAsync(request, "admin");

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Catalogue>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix("catalogues"), Times.Once);
        dto.Name.Should().Be("Mode");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ThrowsKeyNotFound_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Catalogue?)null);

        var act = async () => await _service.DeleteAsync(Guid.NewGuid(), "admin");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── PagedResult ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(25, 10, 3, true,  false)]
    [InlineData(10, 10, 1, false, false)]
    [InlineData(0,  10, 0, false, false)]
    public void PagedResult_Pagination_IsCorrect(
        int total, int pageSize, int expectedPages, bool hasNext, bool hasPrev)
    {
        var result = new PagedResult<CatalogueDto>(
            Enumerable.Empty<CatalogueDto>(), 1, pageSize, total);

        result.TotalPages.Should().Be(expectedPages);
        result.HasNextPage.Should().Be(hasNext);
        result.HasPreviousPage.Should().Be(hasPrev);
    }
}