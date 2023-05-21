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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("books", async (Book book, IBookService bookService) =>
{
    var created = await bookService.CreateAsync(book);
    return created
        ? Results.Created($"/books/{book.Isbn}", book)
        : Results.BadRequest("A book with that Isbn already exists.");
});


// Db init
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();