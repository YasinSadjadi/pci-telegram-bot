namespace VocabularyBot.Infrastructure.Abstractions.Results;

public sealed record Result(bool IsSuccessful, string Message)
{
    public static Result Success(string message)
    {
        return new Result(true, message);
    }

    public static Result Failure(string message)
    {
        return new Result(false, message);
    }
}