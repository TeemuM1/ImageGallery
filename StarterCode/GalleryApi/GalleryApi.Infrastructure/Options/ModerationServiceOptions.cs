namespace GalleryApi.Infrastructure.Options;

public class ModerationServiceOptions
{
    public const string SectionName = "ModerationService";

    /// <summary>
    /// API-avain sisällöntarkistuspalveluun. EI saa olla kovakoodattu!
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.moderation-example.com";
}
