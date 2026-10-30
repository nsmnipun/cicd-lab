using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Notes.Api.Notes;

namespace Notes.Api.Tests;

// Tests share one database per class, so each test only asserts on notes it created itself.
public class NoteEndpointsTests(NotesApiFactory factory) : IClassFixture<NotesApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<NoteResponse> CreateNoteAsync(string title = "Title", string? body = "Body")
    {
        var response = await _client.PostAsJsonAsync("/api/notes", new NoteRequest(title, body));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NoteResponse>())!;
    }

    [Fact]
    public async Task Post_creates_note()
    {
        var response = await _client.PostAsJsonAsync("/api/notes", new NoteRequest("  Groceries  ", "Milk"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.NotNull(note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal("Groceries", note.Title);
        Assert.Equal("Milk", note.Body);
        Assert.Equal(note.CreatedAt, note.UpdatedAt);
        Assert.Equal($"/api/notes/{note.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_with_invalid_input_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/notes", new NoteRequest("", new string('a', 10001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("title", problem.Errors.Keys);
        Assert.Contains("body", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_all_returns_created_notes()
    {
        var first = await CreateNoteAsync("First");
        var second = await CreateNoteAsync("Second");

        var notes = await _client.GetFromJsonAsync<List<NoteResponse>>("/api/notes");

        Assert.NotNull(notes);
        Assert.Contains(notes, n => n.Id == first.Id);
        Assert.Contains(notes, n => n.Id == second.Id);
    }

    [Fact]
    public async Task Get_by_id_returns_note()
    {
        var created = await CreateNoteAsync("Read me", "Some text");

        var note = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");

        Assert.NotNull(note);
        Assert.Equal(created.Id, note.Id);
        Assert.Equal("Read me", note.Title);
        Assert.Equal("Some text", note.Body);
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_missing_note()
    {
        var response = await _client.GetAsync($"/api/notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_updates_note()
    {
        var created = await CreateNoteAsync("Old title", "Old body");

        var response = await _client.PutAsJsonAsync($"/api/notes/{created.Id}", new NoteRequest("New title", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.NotNull(updated);
        Assert.Equal("New title", updated.Title);
        Assert.Null(updated.Body);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);

        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Equal("New title", fetched!.Title);
    }

    [Fact]
    public async Task Put_with_invalid_input_returns_validation_problem()
    {
        var created = await CreateNoteAsync();

        var response = await _client.PutAsJsonAsync($"/api/notes/{created.Id}", new NoteRequest(new string('a', 201), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("title", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Put_returns_404_for_missing_note()
    {
        var response = await _client.PutAsJsonAsync($"/api/notes/{Guid.NewGuid()}", new NoteRequest("Title", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_note()
    {
        var created = await CreateNoteAsync();

        var response = await _client.DeleteAsync($"/api/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/notes/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_returns_404_for_missing_note()
    {
        var response = await _client.DeleteAsync($"/api/notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_live_returns_healthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
