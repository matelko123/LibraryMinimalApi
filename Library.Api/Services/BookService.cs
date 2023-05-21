﻿using Dapper;
using Library.Api.Models;
using Library.Api.Properties.Data;

namespace Library.Api.Services;

public class BookService : IBookService
{
    private IDbConnectionFactory _connectionFactory;

    public BookService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(Book book)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.ExecuteAsync(
            @"INSERT INTO Books (Isbn, Title, Author, ShortDescription, PageCount, ReleaseDate) 
                    values (@Isbn, @Title, @Author, @ShortDescription, @PageCount, @ReleaseDate)", book);
        return result > 0;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryAsync<Book>("SELECT * FROM Books");
    }

    public async Task<IEnumerable<Book>> SearchByTitleAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateAsync(Book book)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteAsync(string isbn)
    {
        throw new NotImplementedException();
    }
}