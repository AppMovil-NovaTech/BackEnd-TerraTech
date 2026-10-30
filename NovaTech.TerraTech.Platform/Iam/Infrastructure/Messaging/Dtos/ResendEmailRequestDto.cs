using System.Text.Json.Serialization;

namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging.Dtos;

public sealed record ResendEmailRequestDto(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string[] To,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("html")] string Html);
