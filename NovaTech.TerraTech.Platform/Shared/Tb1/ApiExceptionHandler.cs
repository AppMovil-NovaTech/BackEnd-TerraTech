using Microsoft.AspNetCore.Mvc;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;
public class ApiExceptionHandler : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception is ApiFailure f ? f.Status : exception is ArgumentException ? 400 : exception is Microsoft.EntityFrameworkCore.DbUpdateException && DatabaseErrors.IsConstraintViolation(exception) ? 409 : 500;
        var code = exception is ApiFailure a ? a.Code : status == 409 ? "DATA_CONFLICT" : status == 400 ? "INVALID_INPUT" : "INTERNAL_ERROR";
        context.Response.StatusCode = status;
        await Results.Problem(statusCode: status, title: status == 500 ? "An internal error occurred." : exception is ApiFailure ? exception.Message : "The request could not be completed.", extensions: new Dictionary<string, object?> { ["code"] = code }).ExecuteAsync(context);
        return true;
    }
}
