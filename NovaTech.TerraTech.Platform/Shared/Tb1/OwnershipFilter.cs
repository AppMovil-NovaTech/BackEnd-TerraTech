using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.AnalyticsManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.NotificationManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.CommunityManagement.Domain.Model.Aggregates;

namespace NovaTech.TerraTech.Platform.Shared.Tb1;
public class OwnershipFilter(AppDbContext db) : IAsyncActionFilter, IAlwaysRunResultFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext c, ActionExecutionDelegate next)
    {
        var controller = c.RouteData.Values["controller"]?.ToString();
        var method = c.HttpContext.Request.Method;
        var write = method != "GET";
        var ct = c.HttpContext.RequestAborted;
        var route = c.RouteData.Values;
        foreach (var key in new[]
        {
            "fieldId",
            "profileId",
            "targetProfileId"
        }

        )
        {
            if (!route.TryGetValue(key, out var value) || !int.TryParse(value?.ToString(), out var id))
                continue;
            if (controller == "CommunityProfiles" && !write)
                continue;
            var found = key == "fieldId" ? await db.Set<Field>().AnyAsync(x => x.Id == id, ct) : key == "targetProfileId" && !write ? await db.Set<Profile>().IgnoreQueryFilters().AnyAsync(x => x.Id == id, ct) : await db.Set<Profile>().AnyAsync(x => x.Id == id, ct);
            if (!found)
            {
                c.Result = ApiFailure.Result(404, "RESOURCE_NOT_FOUND", "Resource was not found.");
                return;
            }
        }

        if (write)
        {
            foreach (var resource in c.ActionArguments.Values.Where(x => x != null && x.GetType().Name.EndsWith("Resource")))
            {
                if (resource!.GetType().GetProperty("TargetProfileId")?.GetValue(resource)is int target && !await db.Set<Profile>().IgnoreQueryFilters().AnyAsync(x => x.Id == target, ct))
                {
                    c.Result = ApiFailure.Result(404, "RESOURCE_NOT_FOUND", "Target profile was not found.");
                    return;
                }

                foreach (var key in new[]
                {
                    "UserId",
                    "ProfileId",
                    "AuthorProfileId",
                    "FieldId",
                    "DeviceId"
                }

                )
                {
                    if (resource!.GetType().GetProperty(key)?.GetValue(resource)is not int id)
                        continue;
                    var found = key switch
                    {
                        "UserId" => id == db.CurrentUserId,
                        "FieldId" => await db.Set<Field>().AnyAsync(x => x.Id == id, ct),
                        "DeviceId" => await db.Set<Device>().AnyAsync(x => x.Id == id, ct),
                        _ => await db.Set<Profile>().AnyAsync(x => x.Id == id, ct)};
                    if (!found)
                    {
                        c.Result = ApiFailure.Result(404, "RESOURCE_NOT_FOUND", "Resource was not found.");
                        return;
                    }
                }
            }
        }

        if (route.TryGetValue("id", out var v) || route.TryGetValue("notificationId", out v))
        {
            if (int.TryParse(v?.ToString(), out var id))
            {
                bool found = controller switch
                {
                    "Fields" => await db.Set<Field>().AnyAsync(x => x.Id == id, ct),
                    "Devices" or "SensorReadings" => await db.Set<Device>().AnyAsync(x => x.Id == id, ct),
                    "Reports" => await db.Set<Report>().AnyAsync(x => x.Id == id, ct),
                    "Orders" => await db.Set<Order>().AnyAsync(x => x.Id == id, ct),
                    "Inventories" => await db.Set<Inventory>().AnyAsync(x => x.Id == id, ct),
                    "Notifications" => await db.Set<Notification>().AnyAsync(x => x.Id == id, ct),
                    "CommunityProfiles" when write => await db.Set<CommunityProfile>().AnyAsync(x => x.Id == id && db.Set<Profile>().Any(p => p.Id == x.ProfileId), ct),
                    "Comments" when write => await db.Set<Comment>().AnyAsync(x => x.Id == id && x.UserId == db.CurrentUserId, ct),
                    _ => true
                };
                if (!found)
                {
                    c.Result = ApiFailure.Result(404, "RESOURCE_NOT_FOUND", "Resource was not found.");
                    return;
                }

                if (method == "DELETE")
                {
                    bool dependencies = controller switch
                    {
                        "Fields" => await db.Set<Device>().IgnoreQueryFilters().AnyAsync(x => x.FieldId.Value == id, ct),
                        "Devices" => await db.Set<SensorReading>().IgnoreQueryFilters().AnyAsync(x => x.DeviceId == id, ct) || await db.Set<Report>().IgnoreQueryFilters().AnyAsync(x => x.DeviceId.Value == id, ct),
                        _ => false
                    };
                    if (dependencies)
                    {
                        c.Result = ApiFailure.Result(409, "DEPENDENT_RESOURCES", "Resource has dependent records.");
                        return;
                    }
                }
            }
        }

        if (method == "DELETE" && controller == "Profiles" && int.TryParse(route["profileId"]?.ToString(), out var profileId))
        {
            var dependencies = await db.Set<Field>().IgnoreQueryFilters().AnyAsync(x => x.ProfileId.Value == profileId, ct) || await db.Set<Notification>().IgnoreQueryFilters().AnyAsync(x => x.ProfileId == profileId, ct) || await db.Set<CommunityProfile>().IgnoreQueryFilters().AnyAsync(x => x.ProfileId == profileId, ct) || await db.Set<Comment>().IgnoreQueryFilters().AnyAsync(x => x.TargetProfileId == profileId, ct);
            if (dependencies)
            {
                c.Result = ApiFailure.Result(409, "DEPENDENT_RESOURCES", "Profile has dependent records.");
                return;
            }
        }

        await next();
    }

    public void OnResultExecuting(ResultExecutingContext c)
    {
        var status = c.Result switch
        {
            ObjectResult o => o.StatusCode ?? 200,
            StatusCodeResult s => s.StatusCode,
            _ => 200
        };
        if (status >= 400 && c.Result is not ObjectResult { Value: ProblemDetails })
            c.Result = ApiFailure.Result(status, status == 404 ? "RESOURCE_NOT_FOUND" : status == 409 ? "DATA_CONFLICT" : status >= 500 ? "INTERNAL_ERROR" : "INVALID_INPUT", status >= 500 ? "An internal error occurred." : "The request could not be completed.");
        else if (status >= 500 && c.Result is ObjectResult { Value: ProblemDetails pd })
        {
            pd.Detail = null;
            pd.Title = "An internal error occurred.";
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
