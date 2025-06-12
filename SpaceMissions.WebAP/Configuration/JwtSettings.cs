namespace SpaceMissions.WebAP.Configuration;

public class JwtSettings
{
    private static readonly string _section = "Jwt";
    private readonly IConfiguration _configuration;

    public string? Key => _configuration.GetSection(_section).GetValue<string>("Key");
    public string? Issuer => _configuration.GetSection(_section).GetValue<string>("Issuer");
    public string? Audience => _configuration.GetSection(_section).GetValue<string>("Audience");

    public JwtSettings(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}