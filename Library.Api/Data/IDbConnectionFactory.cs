using System.Data;

namespace Library.Api.Properties.Data;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}