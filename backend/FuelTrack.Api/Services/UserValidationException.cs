namespace FuelTrack.Api.Services;

public sealed class UserValidationException : Exception
{
    public UserValidationException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
