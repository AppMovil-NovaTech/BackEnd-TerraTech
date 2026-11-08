namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging;

public class AccountEmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = "TerraTech <no-reply@auth.lucemz.com>";
    public string PublicBaseUrl { get; set; } = "https://terratech-api.lucemz.com";
}
