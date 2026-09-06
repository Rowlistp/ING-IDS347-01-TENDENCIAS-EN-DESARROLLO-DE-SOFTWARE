namespace FuelTrack.Api.Security;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    public bool Enabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string IdentityClaim { get; set; } = "preferred_username";
    public bool RequireHttpsMetadata { get; set; } = true;
}
