namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;

public record AuthenticatedUserResource(int Id, string EmailAddress, string? Token, string? FullName = null, DateTime? ExpiresAt = null, bool EmailVerificationRequired = false);
