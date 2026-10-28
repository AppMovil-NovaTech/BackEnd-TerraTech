
namespace NovaTech.TerraTech.Platform.Monitoring.Domain.Model.ValueObjects;

public static class FertilityZoneGeometry
{
    private const double Epsilon = 1e-12;

    public static bool IsWithinField(
        IReadOnlyList<FieldVertex> zone,
        IReadOnlyList<FieldVertex>? field)
    {
        if (field is null || field.Count < 3 || zone.Count < 3)
            return false;

        for (var i = 0; i < zone.Count; i++)
        {
            var start = zone[i];
            var end = zone[(i + 1) % zone.Count];

            if (!IsInsideOrOnBoundary(start, field))
                return false;

            if (!IsSegmentInsideField(start, end, field))
                return false;
        }

        return true;
    }

    private static bool IsSegmentInsideField(
        FieldVertex start,
        FieldVertex end,
        IReadOnlyList<FieldVertex> field)
    {
        var cuts = new List<double> { 0.0, 1.0 };

        var dx = end.Longitude - start.Longitude;
        var dy = end.Latitude - start.Latitude;
        var lengthSquared = dx * dx + dy * dy;

        if (lengthSquared == 0.0)
            return false;

        for (var i = 0; i < field.Count; i++)
        {
            var a = field[i];
            var b = field[(i + 1) % field.Count];

            var sx = b.Longitude - a.Longitude;
            var sy = b.Latitude - a.Latitude;
            var qx = a.Longitude - start.Longitude;
            var qy = a.Latitude - start.Latitude;

            var denominator = dx * sy - dy * sx;
            var cross = qx * dy - qy * dx;

            if (Math.Abs(denominator) > Epsilon)
            {
                var t = (qx * sy - qy * sx) / denominator;
                var u = cross / denominator;

                if (t >= -Epsilon && t <= 1.0 + Epsilon &&
                    u >= -Epsilon && u <= 1.0 + Epsilon)
                {
                    cuts.Add(Math.Clamp(t, 0.0, 1.0));
                }
            }
            else if (Math.Abs(cross) <= Epsilon)
            {
                var t1 = (qx * dx + qy * dy) / lengthSquared;
                var t2 = (
                    (b.Longitude - start.Longitude) * dx +
                    (b.Latitude - start.Latitude) * dy
                ) / lengthSquared;

                cuts.Add(Math.Clamp(t1, 0.0, 1.0));
                cuts.Add(Math.Clamp(t2, 0.0, 1.0));
            }
        }

        cuts.Sort();

        for (var i = 0; i < cuts.Count - 1; i++)
        {
            if (cuts[i + 1] - cuts[i] <= Epsilon)
                continue;

            var middle = (cuts[i] + cuts[i + 1]) / 2.0;

            var point = new FieldVertex(
                start.Latitude + dy * middle,
                start.Longitude + dx * middle
            );

            if (!IsInsideOrOnBoundary(point, field))
                return false;
        }

        return IsInsideOrOnBoundary(end, field);
    }

    private static bool IsInsideOrOnBoundary(
        FieldVertex point,
        IReadOnlyList<FieldVertex> polygon)
    {
        var inside = false;

        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];

            if (IsOnSegment(point, a, b))
                return true;

            if ((a.Latitude > point.Latitude) !=
                (b.Latitude > point.Latitude))
            {
                var longitude = a.Longitude +
                    (point.Latitude - a.Latitude) *
                    (b.Longitude - a.Longitude) /
                    (b.Latitude - a.Latitude);

                if (point.Longitude < longitude)
                    inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsOnSegment(
        FieldVertex point,
        FieldVertex a,
        FieldVertex b)
    {
        var cross =
            (b.Longitude - a.Longitude) *
            (point.Latitude - a.Latitude) -
            (b.Latitude - a.Latitude) *
            (point.Longitude - a.Longitude);

        if (Math.Abs(cross) > Epsilon)
            return false;

        return point.Longitude >= Math.Min(a.Longitude, b.Longitude) - Epsilon &&
               point.Longitude <= Math.Max(a.Longitude, b.Longitude) + Epsilon &&
               point.Latitude >= Math.Min(a.Latitude, b.Latitude) - Epsilon &&
               point.Latitude <= Math.Max(a.Latitude, b.Latitude) + Epsilon;
    }
}
