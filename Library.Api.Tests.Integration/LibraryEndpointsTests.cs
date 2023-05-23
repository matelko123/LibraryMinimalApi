using System.Net;
using FluentAssertions;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Library.Api.Tests.Integration;

public class LibraryEndpointsTests : IClassFixture<WebApplicationFactory<IApiMarker>>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;

    public LibraryEndpointsTests(WebApplicationFactory<IApiMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBook_CreatesBook_WhenDataIsCorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpClient.PostAsJsonAsync("/books", book);
        var createdBook = await result.Content.ReadFromJsonAsync<Book>();
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        createdBook.Should().BeEquivalentTo(book);
        result.Headers.Location.Should().Be($"{httpClient.BaseAddress}books/{book.Isbn}");
    }

    private Book GenerateBook(string title = "The testing integration book")
        => new Book
        {
            Isbn = GenerateIsbn(),
            Title = title,
            Author = "Mateusz",
            PageCount = 420,
            ShortDescription = "Please work",
            ReleaseDate = new DateTime(2023, 1, 1)
        };

    private string GenerateIsbn()
        => $"{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000000000, 2100999999)}";
}