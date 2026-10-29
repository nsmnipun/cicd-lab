using Notes.Api.Notes;

namespace Notes.Api.Tests;

public class NoteValidatorTests
{
    [Fact]
    public void Valid_request_has_no_errors()
    {
        var errors = NoteValidator.Validate(new NoteRequest("Groceries", "Milk, eggs"));

        Assert.Empty(errors);
    }

    [Fact]
    public void Body_is_optional()
    {
        var errors = NoteValidator.Validate(new NoteRequest("Groceries", null));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_is_required(string? title)
    {
        var errors = NoteValidator.Validate(new NoteRequest(title, null));

        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void Title_at_max_length_is_valid()
    {
        var errors = NoteValidator.Validate(new NoteRequest(new string('a', 200), null));

        Assert.Empty(errors);
    }

    [Fact]
    public void Title_over_max_length_is_invalid()
    {
        var errors = NoteValidator.Validate(new NoteRequest(new string('a', 201), null));

        Assert.Contains("title", errors.Keys);
    }

    [Fact]
    public void Body_at_max_length_is_valid()
    {
        var errors = NoteValidator.Validate(new NoteRequest("Title", new string('a', 10000)));

        Assert.Empty(errors);
    }

    [Fact]
    public void Body_over_max_length_is_invalid()
    {
        var errors = NoteValidator.Validate(new NoteRequest("Title", new string('a', 10001)));

        Assert.Contains("body", errors.Keys);
    }

    [Fact]
    public void Reports_all_errors_at_once()
    {
        var errors = NoteValidator.Validate(new NoteRequest(null, new string('a', 10001)));

        Assert.Equal(["body", "title"], errors.Keys.Order());
    }
}
