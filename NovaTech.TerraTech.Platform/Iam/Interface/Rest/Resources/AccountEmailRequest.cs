using System.ComponentModel.DataAnnotations;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;

public record AccountEmailRequest([Required, EmailAddress, MaxLength(255)] string EmailAddress);
