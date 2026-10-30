using System.ComponentModel.DataAnnotations;
namespace NovaTech.TerraTech.Platform.Iam.Interface.Rest.Resources;

public record ConfirmEmailResource([Required, StringLength(43, MinimumLength = 43)] string Token);
