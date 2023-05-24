using System.Net;
using FluentAssertions;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Library.Api.Tests.Integration;

public class LibraryEndpointsTests : IClassFixture<WebApplicationFactory<IApiMarker>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly List<string> _createdIsbns = new ();

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
        _createdIsbns.Add(book.Isbn);
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        createdBook.Should().BeEquivalentTo(book);
        result.Headers.Location.Should().Be($"{httpClient.BaseAddress}books/{book.Isbn}");
    }

    [Fact]
    public async Task CreateBook_Fails_WhenIsbnIsInvalid()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        book.Isbn = "Invalid";

        // Act
        var result = await httpClient.PostAsJsonAsync("/books", book);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Isbn");
        error.ErrorMessage.Should().Be("Value was not a valid ISBN-13");
    }
    
    [Fact]
    public async Task CreateBook_Fails_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpClient.PostAsJsonAsync("/books", book);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Isbn");
        error.ErrorMessage.Should().Be("A book with that Isbn already exists.");
    }

    [Fact]
    public async Task GetBook_ReturnsBook_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpClient.GetAsync($"/books/{book.Isbn}");
        var existingBook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        existingBook.Should().BeEquivalentTo(book);
    }
    
    [Fact]
    public async Task GetBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var isbn = GenerateIsbn();

        // Act
        var result = await httpClient.GetAsync($"/books/{isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsAllBooks_WhenBooksExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var books = new List<Book> { book };

        // Act
        var result = await httpClient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<IEnumerable<Book>>();
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }
    
    [Fact]
    public async Task GetAllBooks_ReturnsNoBooks_WhenNoBooksExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();

        // Act
        var result = await httpClient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<IEnumerable<Book>>();
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedBooks.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchBooks_ReturnsBooks_WhenTitleMatches()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var books = new List<Book> { book };

        // Act
        var result = await httpClient.GetAsync($"/books?searchTerm={book.Title}");
        var returnedBooks = await result.Content.ReadFromJsonAsync<IEnumerable<Book>>();
        
        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }

    [Fact]
    public async Task UpdateBook_UpdateBook_WhenDataIsCorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.PageCount = 69;
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
        var updatedBook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedBook.Should().BeEquivalentTo(book);
    }
    
    [Fact]
    public async Task UpdateBook_DoesNotUpdateBook_WhenDataIsIncorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.Title = string.Empty;
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Title");
        error.ErrorMessage.Should().Be("Pole 'Title' nie może być puste.");
    }
    
    [Fact]
    public async Task UpdateBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var isbn = GenerateIsbn();

        // Act
        var result = await httpClient.DeleteAsync($"/books/{isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task DeleteBook_ReturnsNoContent_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpClient.DeleteAsync($"/books/{book.Isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        var httpClient = _factory.CreateClient();
        foreach (string createdIsbn in _createdIsbns)
        {
            await httpClient.DeleteAsync($"books/{createdIsbn}");
        }
    }
}