using Dapper;

namespace Library.Api.Properties.Data;

public class DatabaseInitializer
{
    private IDbConnectionFactory _connectionFactory;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Books (
                Isbn TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                ShortDescription TEXT NOT NULL,
                PageCount TEXT NOT NULL,
                ReleaseDate TEXT NOT NULL)");
    }
}