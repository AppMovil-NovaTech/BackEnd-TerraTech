namespace NovaTech.TerraTech.Platform.ProfileManagement.Interfaces.REST.Resources;

public record MyProfileResource(int Id, int UserId, string? FullName, string EmailAddress, string FundoName, string ContactPhone, string? Location, double? SizeM2, double MoistureThreshold, double TempThreshold, DateTimeOffset? RegisteredAt = null);
