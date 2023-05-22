using FluentValidation;
using FluentValidation.Results;
using Library.Api.Auth;
using Library.Api.Models;
using Library.Api.Properties.Data;
using Library.Api.Services;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(ApiKeySchemeConstants.SchemeName)
    .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(ApiKeySchemeConstants.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetValue<string>("Database:ConnectionString") 
                       ?? throw new InvalidOperationException();
builder.Services.AddSingleton<IDbConnectionFactory>(_ => 
    new SqliteConnectionFactory(connectionString));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapPost("books", 
    [Authorize(AuthenticationSchemes = ApiKeySchemeConstants.SchemeName)]
    async (Book book, 
    IBookService bookService, IValidator<Book> validator) =>
{
    var validationResult = await validator.ValidateAsync(book);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }
    
    var created = await bookService.CreateAsync(book);
    return created
        ? Results.CreatedAtRoute("GetBook", new {isbn = book.Isbn})
        : Results.BadRequest(new List<ValidationFailure>
        {
            new ("Isbn", "A book with that Isbn already exists.")
        });
}).WithName("CreateBook")
    .WithTags("Books")
    .Accepts<Book>("application/json")
    .Produces<Book>(StatusCodes.Status201Created)
    .Produces<IEnumerable<ValidationFailure>>(StatusCodes.Status400BadRequest);

app.MapPut("books/{isbn}", async (string isbn, Book book, 
    IBookService bookService, IValidator<Book> validator) =>
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
}).WithName("UpdateBook")
    .WithTags("Books")
    .Accepts<Book>("application/json")
    .Produces<Book>(StatusCodes.Status200OK)
    .Produces<IEnumerable<ValidationFailure>>(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
{
    if (!string.IsNullOrEmpty(searchTerm))
    {
        var matchedBooks = await bookService.SearchByTitleAsync(searchTerm);
        return Results.Ok(matchedBooks);
    }
    
    var books = await bookService.GetAllAsync();
    return Results.Ok(books);
}).WithName("GetBooks")
    .WithTags("Books")
    .Produces<IEnumerable<Book>>(StatusCodes.Status200OK);

app.MapGet("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var book = await bookService.GetByIsbnAsync(isbn);
    return book is not null
        ? Results.Ok(book)
        : Results.NotFound();
}).WithName("GetBook")
    .WithTags("Books")
    .Produces<Book>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapDelete("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var deleted = await bookService.DeleteAsync(isbn);
    return deleted
        ? Results.NoContent()
        : Results.NotFound();
}).WithName("DeleteBook")
    .WithTags("Books")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);

// Db init
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();