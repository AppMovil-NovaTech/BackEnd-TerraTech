
using Microsoft.EntityFrameworkCore;
using NovaTech.TerraTech.Platform.Monitoring.Domain.Model.Aggregates;
using NovaTech.TerraTech.Platform.Monitoring.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace NovaTech.TerraTech.Platform.Monitoring.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyMonitoringConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Field>(field =>
        {
            field.HasKey(f => f.Id);

            field.Property(f => f.Boundary)
                .HasConversion(new FieldBoundaryConverter())
                .HasColumnType("longtext")
                .Metadata.SetValueComparer(new FieldBoundaryComparer());

            field.Property(f => f.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();

            field.OwnsOne(f => f.ProfileId, profile =>
            {
                profile.WithOwner().HasForeignKey("Id");
                profile.Property(p => p.Value)
                    .HasColumnName("ProfileId")
                    .IsRequired();
            });

            field.OwnsOne(f => f.Name, name =>
            {
                name.WithOwner().HasForeignKey("Id");
                name.Property(n => n.Value)
                    .HasColumnName("Name")
                    .HasMaxLength(100)
                    .IsRequired();
            });

            field.OwnsOne(f => f.SizeM2, size =>
            {
                size.WithOwner().HasForeignKey("Id");
                size.Property(s => s.Value)
                    .HasColumnName("SizeM2")
                    .IsRequired();
            });

            field.OwnsOne(f => f.SoilType, soil =>
            {
                soil.WithOwner().HasForeignKey("Id");
                soil.Property(s => s.Value)
                    .HasColumnName("SoilType")
                    .HasMaxLength(50)
                    .IsRequired();
            });

            field.OwnsOne(f => f.LocationLatLong, loc =>
            {
                loc.WithOwner().HasForeignKey("Id");
                loc.Property(l => l.Latitude)
                    .HasColumnName("Latitude")
                    .IsRequired();
                loc.Property(l => l.Longitude)
                    .HasColumnName("Longitude")
                    .IsRequired();
            });
        });

        builder.Entity<Device>(device =>
        {
            device.HasKey(d => d.Id);

            device.Property(d => d.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();

            device.Ignore(d => d.MacAddress);

            device.OwnsOne(d => d.FieldId, fieldId =>
            {
                fieldId.WithOwner().HasForeignKey("Id");
                fieldId.Property(f => f.Value)
                    .HasColumnName("FieldId")
                    .IsRequired();
            });

            device.OwnsOne(d => d.MacAddress, mac =>
            {
                mac.WithOwner().HasForeignKey("Id");
                mac.Property(m => m.Value)
                    .HasColumnName("MacAddress")
                    .HasMaxLength(17)
                    .IsRequired();
            });

            device.OwnsOne(d => d.Status, status =>
            {
                status.WithOwner().HasForeignKey("Id");
                status.Property(s => s.Value)
                    .HasColumnName("Status")
                    .HasMaxLength(20)
                    .IsRequired();
            });

            device.OwnsOne(d => d.LastSync, lastSync =>
            {
                lastSync.WithOwner().HasForeignKey("Id");
                lastSync.Property(l => l.Value)
                    .HasColumnName("LastSync")
                    .IsRequired();
            });
        });

        // US13: Configuration of geographic fertility zones
        builder.Entity<FertilityZone>(zone =>
        {
            zone.ToTable("FertilityZones");

            zone.HasKey(z => z.Id);

            zone.Property(z => z.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();

            zone.Property(z => z.FieldId)
                .IsRequired();

            zone.Property(z => z.DeviceId)
                .IsRequired();

            zone.Property(z => z.Name)
                .HasMaxLength(100)
                .IsRequired();

            zone.Property(z => z.Boundary)
                .HasConversion(new FieldBoundaryConverter())
                .HasColumnType("longtext")
                .IsRequired()
                .Metadata.SetValueComparer(new FieldBoundaryComparer());

            zone.HasOne<Field>()
                .WithMany()
                .HasForeignKey(z => z.FieldId)
                .OnDelete(DeleteBehavior.Cascade);

            zone.HasOne<Device>()
                .WithMany()
                .HasForeignKey(z => z.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            zone.HasIndex(z => z.FieldId);
            zone.HasIndex(z => z.DeviceId);

            // US18: Custom alert thresholds per fertility zone
            zone.Property(z => z.MoistureMin)
                .IsRequired()
                .HasDefaultValue(25.0);

            zone.Property(z => z.MoistureMax)
                .IsRequired()
                .HasDefaultValue(75.0);

            zone.Property(z => z.NitrogenMin)
                .IsRequired()
                .HasDefaultValue(20.0);

            zone.Property(z => z.NitrogenMax)
                .IsRequired()
                .HasDefaultValue(40.0);

            zone.Property(z => z.PhosphorusMin)
                .IsRequired()
                .HasDefaultValue(15.0);

            zone.Property(z => z.PhosphorusMax)
                .IsRequired()
                .HasDefaultValue(30.0);

            zone.Property(z => z.PotassiumMin)
                .IsRequired()
                .HasDefaultValue(30.0);

            zone.Property(z => z.PotassiumMax)
                .IsRequired()
                .HasDefaultValue(50.0);
        });
    }
}
