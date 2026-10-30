using System.ComponentModel.DataAnnotations;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;

public record ResetPasswordResource([Required, StringLength(43, MinimumLength = 43)] string Token,
    [Required, MinLength(6), MaxLength(128)] string Password, [Required] string ConfirmPassword);
