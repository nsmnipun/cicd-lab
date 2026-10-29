namespace Notes.Api.Notes;

public static class NoteValidator
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 10000;

    // Returns an empty dictionary when the request is valid.
    public static Dictionary<string, string[]> Validate(NoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (request.Title.Trim().Length > TitleMaxLength)
        {
            errors["title"] = [$"Title must be at most {TitleMaxLength} characters."];
        }

        if (request.Body is { Length: > BodyMaxLength })
        {
            errors["body"] = [$"Body must be at most {BodyMaxLength} characters."];
        }

        return errors;
    }
}
