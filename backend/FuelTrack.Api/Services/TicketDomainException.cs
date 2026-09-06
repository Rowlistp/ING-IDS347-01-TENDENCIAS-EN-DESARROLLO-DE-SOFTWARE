namespace FuelTrack.Api.Services;

public sealed class TicketDomainException : Exception
{
    public TicketDomainException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }
    public string Code { get; }
}
