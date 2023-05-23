using FluentValidation;
using FluentValidation.Results;
using Library.Api.Endpoints.Internal;
using Library.Api.Models;
using Library.Api.Services;

namespace Library.Api.Endpoints;

public class LibraryEndpoints : IEndpoints
{
    private const string ContentType = "application/json";
    private const string Tag = "Books";
    private const string BaseRoute = "books";
    
    public static void DefineEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(BaseRoute, CreateBookAsync)
            .WithName("CreateBook")
            .WithTags(Tag)
            .Accepts<Book>(ContentType)
            .Produces<Book>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ValidationFailure>>(StatusCodes.Status400BadRequest);

        app.MapPut($"{BaseRoute}/{{isbn}}", UpdateBookAsync)
            .WithName("UpdateBook")
            .WithTags(Tag)
            .Accepts<Book>(ContentType)
            .Produces<Book>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ValidationFailure>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet(BaseRoute, GetAllBooksAsync)
            .WithName("GetBooks")
            .WithTags(Tag)
            .Produces<IEnumerable<Book>>(StatusCodes.Status200OK);

        app.MapGet($"{BaseRoute}/{{isbn}}", GetBookByIsbn)
            .WithName("GetBook")
            .WithTags(Tag)
            .Produces<Book>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapDelete($"{BaseRoute}/{{isbn}}", DeleteBook)
            .WithName("DeleteBook")
            .WithTags(Tag)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> CreateBookAsync(
        Book book, IBookService bookService, IValidator<Book> validator)
    {
        var validationResult = await validator.ValidateAsync(book);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        var created = await bookService.CreateAsync(book);
        return created
            ? Results.CreatedAtRoute("GetBook", new { isbn = book.Isbn })
            : Results.BadRequest(new List<ValidationFailure>
            {
                new("Isbn", "A book with that Isbn already exists.")
            });
    }

    internal static async Task<IResult> UpdateBookAsync(string isbn, Book book,
        IBookService bookService, IValidator<Book> validator)
    {
        book.Isbn = isbn;
        var validationResult = await validator.ValidateAsync(book);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        var updated = await bookService.UpdateAsync(book);
        return updated
            ? Results.Ok(book)
            : Results.NotFound();
    }

    internal static async Task<IResult> GetAllBooksAsync(
        IBookService bookService, string? searchTerm)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var matchedBooks = await bookService.SearchByTitleAsync(searchTerm);
            return Results.Ok(matchedBooks);
        }

        var books = await bookService.GetAllAsync();
        return Results.Ok(books);
    }

    internal static async Task<IResult> GetBookByIsbn(
        string isbn, IBookService bookService)
    {
        var book = await bookService.GetByIsbnAsync(isbn);
        return book is not null
            ? Results.Ok(book)
            : Results.NotFound();
    }

    internal static async Task<IResult> DeleteBook(
        string isbn, IBookService bookService)
    {
        var deleted = await bookService.DeleteAsync(isbn);
        return deleted
            ? Results.NoContent()
            : Results.NotFound();
    }
    
    public static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IBookService, BookService>();
    }
}