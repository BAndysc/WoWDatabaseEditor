using System;
using WDE.Common.Database;

namespace WDE.MapSpawns.Models.SpawnGroups;

/// <summary>
/// The CMaNGOS formation slot layout, ported 1:1 from FormationData::FixSlotsPositions
/// (src/game/Maps/SpawnGroup.cpp) so the editor's ghost preview matches what the core will do
/// in game. Every follower slot resolves to a polar offset (angle relative to the leader's
/// heading, distance) - the leader itself is always at angle 0, distance 0.
/// </summary>
public static class SpawnGroupFormationMath
{
    /// <summary>The core's guess for member collision width; only affects the Random shape's
    /// inter-member spacing (the core uses the widest member's real collision width).</summary>
    public const float DefaultModelWidth = 1.5f;

    /// <summary>Polar offset of the follower occupying the given slot.
    /// <paramref name="followerIndex"/> is 1-based (slot id; slot 0 is the leader),
    /// <paramref name="totalFollowers"/> excludes the leader.</summary>
    public static (float angle, float dist) FollowerOffset(FormationShape shape, int followerIndex,
        int totalFollowers, float spread, float modelWidth = DefaultModelWidth)
    {
        if (followerIndex <= 0 || totalFollowers <= 0)
            return (0, 0);

        int i = followerIndex;
        float pi = MathF.PI;

        switch (shape)
        {
            case FormationShape.Formation0: // random
            {
                float interSpread = modelWidth * 3.0f;
                switch (i)
                {
                    case 1: return BackDiagonal(spread + interSpread, interSpread, left: true);
                    case 2: return BackDiagonal(spread + interSpread, interSpread, left: false);
                    case 3: return (pi, spread + interSpread * 2.0f);
                    case 4: return (pi, spread + interSpread * 3.0f);
                    case 5: return BackDiagonal(spread + interSpread * 2.0f, interSpread, left: true);
                    case 6: return BackDiagonal(spread + interSpread * 2.0f, interSpread, left: false);
                    case 7: return BackDiagonal(spread + interSpread * 3.0f, interSpread, left: true);
                    case 8: return BackDiagonal(spread + interSpread * 3.0f, interSpread, left: false);
                    case 9: return (pi, spread + interSpread);
                    default:
                    {
                        // extra members beside the leader
                        float angle = (i & 1) == 0 ? pi / 2.0f + pi : pi / 2.0f;
                        float dist = interSpread * (((i - 10) / 2) + 1);
                        return (angle, dist);
                    }
                }
            }

            case FormationShape.Formation1: // single file
                return (pi, spread * i);

            case FormationShape.Formation2: // side by side
            {
                float angle = (i & 1) == 0 ? pi / 2.0f + pi : pi / 2.0f;
                return (angle, spread * (((i - 1) / 2) + 1));
            }

            case FormationShape.Formation3: // like geese
            {
                float angle = (i & 1) == 0 ? pi + pi / 4.0f : pi - pi / 4.0f;
                return (angle, spread * (((i - 1) / 2) + 1));
            }

            case FormationShape.Formation4: // fanned out behind
                return (pi / 2.0f + (pi / totalFollowers) * (i - 1), spread);

            case FormationShape.Formation5: // fanned out in front
            {
                float angle = pi + pi / 2.0f + (pi / totalFollowers) * (i - 1);
                if (angle > pi * 2.0f)
                    angle -= pi * 2.0f;
                return (angle, spread);
            }

            case FormationShape.Formation6: // circle the leader
                return ((pi * 2.0f / totalFollowers) * (i - 1), spread);

            default:
                return (0, 0);
        }
    }

    // the Random shape's "behind, offset sideways" pairs: hyp/asin construction from the core
    private static (float angle, float dist) BackDiagonal(float distBack, float side, bool left)
    {
        float hyp = MathF.Sqrt(distBack * distBack + side * side);
        float angle = left
            ? MathF.PI - MathF.Asin(side / hyp)
            : MathF.PI + MathF.Asin(side / hyp);
        return (angle, hyp);
    }

    public static string ShapeName(FormationShape shape) => shape switch
    {
        FormationShape.Formation0 => "Random",
        FormationShape.Formation1 => "Single file",
        FormationShape.Formation2 => "Side by side",
        FormationShape.Formation3 => "Like geese",
        FormationShape.Formation4 => "Fanned out behind",
        FormationShape.Formation5 => "Fanned out in front",
        FormationShape.Formation6 => "Circle the leader",
        _ => shape.ToString(),
    };

    public static string MovementTypeName(MovementType type) => type switch
    {
        MovementType.Idle => "Idle",
        MovementType.Random => "Random wander",
        MovementType.Waypoint => "Waypoint path",
        MovementType.SplinePath => "Spline path",
        MovementType.LinearPath => "Linear path",
        _ => type.ToString(),
    };
}
