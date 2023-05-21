using FluentValidation;
using FluentValidation.Results;
using Library.Api.Models;
using Library.Api.Properties.Data;
using Library.Api.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.MapPost("books", async (Book book, 
    IBookService bookService, IValidator<Book> validator) =>
{
    var validationResult = await validator.ValidateAsync(book);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }
    
    var created = await bookService.CreateAsync(book);
    return created
        ? Results.Created($"/books/{book.Isbn}", book)
        : Results.BadRequest(new List<ValidationFailure>
        {
            new ("Isbn", "A book with that Isbn already exists.")
        });
});
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
});
app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
{
    if (!string.IsNullOrEmpty(searchTerm))
    {
        var matchedBooks = await bookService.SearchByTitleAsync(searchTerm);
        return Results.Ok(matchedBooks);
    }
    
    var books = await bookService.GetAllAsync();
    return Results.Ok(books);
});

app.MapGet("book/{isbn}", async (string isbn, IBookService bookService) =>
{
    var book = await bookService.GetByIsbnAsync(isbn);
    return book is not null
        ? Results.Ok(book)
        : Results.NotFound();
});

// Db init
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();