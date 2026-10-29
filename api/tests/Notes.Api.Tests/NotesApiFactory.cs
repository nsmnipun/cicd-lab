using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Notes.Api.Notes;

namespace Notes.Api.Tests;

// Runs the real app with Postgres swapped for an in-memory SQLite database.
public class NotesApiFactory : WebApplicationFactory<Program>
{
    // The in-memory database lives as long as this connection stays open.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureTestServices(services =>
        {
            var npgsqlRegistrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<NotesDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<NotesDbContext>))
                .ToList();
            foreach (var descriptor in npgsqlRegistrations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<NotesDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
