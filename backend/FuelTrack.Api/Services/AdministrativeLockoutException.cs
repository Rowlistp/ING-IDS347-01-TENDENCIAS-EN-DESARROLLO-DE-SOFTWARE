namespace FuelTrack.Api.Services;

public sealed class AdministrativeLockoutException : InvalidOperationException
{
    public AdministrativeLockoutException(string message) : base(message)
    {
    }
}
