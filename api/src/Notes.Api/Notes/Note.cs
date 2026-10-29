namespace Notes.Api.Notes;

public class Note
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public record NoteRequest(string? Title, string? Body);

public record NoteResponse(Guid Id, string Title, string? Body, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static NoteResponse From(Note note) =>
        new(note.Id, note.Title, note.Body, note.CreatedAt, note.UpdatedAt);
}
