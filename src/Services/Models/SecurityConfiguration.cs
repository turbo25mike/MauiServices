namespace Turbo.Maui.Services.Models;

public class SecurityConfiguration
{
    public int? RequestAccessCodeReg { get; set; }
    public int? ChallengeReg { get; set; }
    public uint MaxAttempts { get; set; }
}