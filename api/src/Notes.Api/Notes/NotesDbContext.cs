using Microsoft.EntityFrameworkCore;

namespace Notes.Api.Notes;

public class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>(note =>
        {
            note.HasKey(n => n.Id);
            note.Property(n => n.Title).IsRequired().HasMaxLength(NoteValidator.TitleMaxLength);
            note.Property(n => n.Body).HasMaxLength(NoteValidator.BodyMaxLength);
            note.HasIndex(n => n.CreatedAt);
        });
    }
}
