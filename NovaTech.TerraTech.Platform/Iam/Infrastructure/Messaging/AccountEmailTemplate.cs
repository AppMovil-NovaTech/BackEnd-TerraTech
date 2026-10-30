namespace NovaTech.TerraTech.Platform.Iam.Infrastructure.Messaging;

internal sealed record AccountEmailTemplate(string Subject, string Action, string Page, string Validity);
