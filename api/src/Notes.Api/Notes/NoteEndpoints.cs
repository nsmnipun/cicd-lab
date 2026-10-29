using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Notes.Api.Notes;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/api/notes");

        notes.MapGet("/", async (NotesDbContext db, CancellationToken ct) =>
        {
            var all = await db.Notes.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync(ct);
            return TypedResults.Ok(all.Select(NoteResponse.From));
        });

        notes.MapGet("/{id:guid}", async Task<Results<Ok<NoteResponse>, NotFound>> (
            Guid id, NotesDbContext db, CancellationToken ct) =>
        {
            var note = await db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
            return note is null ? TypedResults.NotFound() : TypedResults.Ok(NoteResponse.From(note));
        });

        notes.MapPost("/", async Task<Results<Created<NoteResponse>, ValidationProblem>> (
            NoteRequest request, NotesDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            var errors = NoteValidator.Validate(request);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var now = time.GetUtcNow().UtcDateTime;
            var note = new Note
            {
                Id = Guid.CreateVersion7(),
                Title = request.Title!.Trim(),
                Body = request.Body,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Notes.Add(note);
            await db.SaveChangesAsync(ct);

            return TypedResults.Created($"/api/notes/{note.Id}", NoteResponse.From(note));
        });

        notes.MapPut("/{id:guid}", async Task<Results<Ok<NoteResponse>, NotFound, ValidationProblem>> (
            Guid id, NoteRequest request, NotesDbContext db, TimeProvider time, CancellationToken ct) =>
        {
            var errors = NoteValidator.Validate(request);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);
            if (note is null)
            {
                return TypedResults.NotFound();
            }

            note.Title = request.Title!.Trim();
            note.Body = request.Body;
            note.UpdatedAt = time.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);

            return TypedResults.Ok(NoteResponse.From(note));
        });

        notes.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, NotesDbContext db, CancellationToken ct) =>
        {
            var deleted = await db.Notes.Where(n => n.Id == id).ExecuteDeleteAsync(ct);
            return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
        });

        return app;
    }
}
