using NovaTech.TerraTech.Platform.ProfileManagement.Interfaces.REST.Resources;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Iam.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.ProfileManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.ProfileManagement.Interfaces.REST;

[ApiController, Route("api/v1/profiles/me")]
public class MeProfileController(NovaTech.TerraTech.Platform.ProfileManagement.Application.Internal.CommandServices.SelfProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MyProfileResource>> Get(CancellationToken ct)
    {
        var (profile, user) = await profiles.Get(User.UserId(), ct);
        return Resource(profile, user);
    }

    [HttpPut]
    public async Task<ActionResult<MyProfileResource>> Upsert(UpsertMyProfileResource r, CancellationToken ct)
    {
        var (profile, user) = await profiles.Upsert(User.UserId(), r.FullName, r.FundoName, r.ContactPhone, r.Location, r.SizeM2, r.MoistureThreshold, r.TempThreshold, ct);
        return Resource(profile, user);
    }

    private static MyProfileResource Resource(Profile p, User user) => new(p.Id, p.UserId, user.FullName, user.EmailAddress.Value, p.Name.Name, p.Phone.Number, p.Location, p.SizeM2, p.Thresholds.Moisture, p.Thresholds.Temperature, user.CreatedAt);
}
