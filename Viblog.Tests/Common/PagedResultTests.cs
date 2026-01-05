using Viblog.Shared.Data.Common;

namespace Viblog.Tests.Common;

public class PagedResultTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyResult()
    {
        // Act
        var result = new PagedResult<string>();

        // Assert
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.PageNumber);
        Assert.Equal(0, result.PageSize);
    }

    [Fact]
    public void Constructor_WithParameters_SetsAllProperties()
    {
        // Arrange
        var items = new[] { "item1", "item2", "item3" };
        var totalCount = 10;
        var pageNumber = 2;
        var pageSize = 3;

        // Act
        var result = new PagedResult<string>(items, totalCount, pageNumber, pageSize);

        // Assert
        Assert.Equal(items, result.Items);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(pageNumber, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
    }

    #endregion

    #region TotalPages Tests

    [Fact]
    public void TotalPages_WithNoItems_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>([], 0, 1, 10);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(0, totalPages);
    }

    [Fact]
    public void TotalPages_WithExactPages_ReturnsCorrectCount()
    {
        // Arrange
        var result = new PagedResult<string>([], 20, 1, 10);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(2, totalPages);
    }

    [Fact]
    public void TotalPages_WithPartialPage_RoundsUp()
    {
        // Arrange
        var result = new PagedResult<string>([], 25, 1, 10);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(3, totalPages);
    }

    [Fact]
    public void TotalPages_WithOneItem_ReturnsOne()
    {
        // Arrange
        var result = new PagedResult<string>([], 1, 1, 10);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(1, totalPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ReturnsZero()
    {
        // Arrange
        var result = new PagedResult<string>([], 10, 1, 0);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(0, totalPages);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(100, 25, 4)]
    [InlineData(99, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(1, 1, 1)]
    public void TotalPages_WithVariousInputs_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var result = new PagedResult<string>([], totalCount, 1, pageSize);

        // Act
        var totalPages = result.TotalPages;

        // Assert
        Assert.Equal(expectedPages, totalPages);
    }

    #endregion

    #region HasPreviousPage Tests

    [Fact]
    public void HasPreviousPage_OnFirstPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>([], 20, 1, 10);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        Assert.False(hasPrevious);
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>([], 20, 2, 10);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        Assert.True(hasPrevious);
    }

    [Fact]
    public void HasPreviousPage_OnLastPage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>([], 25, 3, 10);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        Assert.True(hasPrevious);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(10, true)]
    public void HasPreviousPage_WithVariousPageNumbers_ReturnsExpectedValue(int pageNumber, bool expectedHasPrevious)
    {
        // Arrange
        var result = new PagedResult<string>([], 100, pageNumber, 10);

        // Act
        var hasPrevious = result.HasPreviousPage;

        // Assert
        Assert.Equal(expectedHasPrevious, hasPrevious);
    }

    #endregion

    #region HasNextPage Tests

    [Fact]
    public void HasNextPage_OnFirstPageWithMultiplePages_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>([], 20, 1, 10);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.True(hasNext);
    }

    [Fact]
    public void HasNextPage_OnMiddlePage_ReturnsTrue()
    {
        // Arrange
        var result = new PagedResult<string>([], 30, 2, 10);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.True(hasNext);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>([], 20, 2, 10);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.False(hasNext);
    }

    [Fact]
    public void HasNextPage_OnSinglePage_ReturnsFalse()
    {
        // Arrange
        var result = new PagedResult<string>([], 5, 1, 10);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.False(hasNext);
    }

    [Fact]
    public void HasNextPage_BeyondLastPage_ReturnsFalse()
    {
        // Arrange - 2 total pages, on page 3
        var result = new PagedResult<string>([], 20, 3, 10);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.False(hasNext);
    }

    [Theory]
    [InlineData(1, 30, 10, true)]
    [InlineData(2, 30, 10, true)]
    [InlineData(3, 30, 10, false)]
    [InlineData(1, 10, 10, false)]
    [InlineData(1, 5, 10, false)]
    public void HasNextPage_WithVariousScenarios_ReturnsExpectedValue(
        int pageNumber, 
        int totalCount, 
        int pageSize, 
        bool expectedHasNext)
    {
        // Arrange
        var result = new PagedResult<string>([], totalCount, pageNumber, pageSize);

        // Act
        var hasNext = result.HasNextPage;

        // Assert
        Assert.Equal(expectedHasNext, hasNext);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void PagedResult_WithNullItems_Works()
    {
        // Act
        var result = new PagedResult<string>
        {
            Items = null!,
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        // Assert
        Assert.Null(result.Items);
    }

    [Fact]
    public void PagedResult_WithComplexType_Works()
    {
        // Arrange
        var items = new[]
        {
            new { Id = 1, Name = "Item 1" },
            new { Id = 2, Name = "Item 2" }
        };

        // Act
        var result = new PagedResult<object>(items, 10, 1, 2);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public void PagedResult_NavigationPropertiesWithEmptyResult_WorkCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>([], 0, 1, 10);

        // Assert
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_LargeDataset_CalculatesCorrectly()
    {
        // Arrange
        var result = new PagedResult<string>([], 1000000, 5000, 200);

        // Act & Assert
        Assert.Equal(5000, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    #endregion

    #region Full Pagination Scenario Test

    [Fact]
    public void PagedResult_CompletePaginationScenario_WorksCorrectly()
    {
        // Scenario: 25 total items, 10 items per page = 3 pages

        // Page 1
        var page1 = new PagedResult<int>(
            Enumerable.Range(1, 10),
            25, 1, 10);
        
        Assert.Equal(3, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);

        // Page 2
        var page2 = new PagedResult<int>(
            Enumerable.Range(11, 10),
            25, 2, 10);
        
        Assert.Equal(3, page2.TotalPages);
        Assert.True(page2.HasPreviousPage);
        Assert.True(page2.HasNextPage);

        // Page 3 (partial page with 5 items)
        var page3 = new PagedResult<int>(
            Enumerable.Range(21, 5),
            25, 3, 10);
        
        Assert.Equal(3, page3.TotalPages);
        Assert.True(page3.HasPreviousPage);
        Assert.False(page3.HasNextPage);
    }

    #endregion
}
