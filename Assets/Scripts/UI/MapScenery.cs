using System.Collections.Generic;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Where things stand in the painted map landscapes (houses, lakes, the ski lift), shared by the
    /// painters that bake them and by <see cref="MapAmbience"/>, which animates on top of them (chimney
    /// smoke, glints on the water, lift chairs). Positions are in painting space: canvas units with the
    /// height not yet stretched by <see cref="Stretch"/>.
    /// </summary>
    public static class MapScenery
    {
        /// <summary>
        /// The landscapes are painted in an unstretched space and stretched upward by this much, so the land
        /// reaches higher up the screen (the horizon sits just below the header).
        /// </summary>
        public const float Stretch = 1.15f;

        public static Vector2 ToCanvas(Vector2 paint) => new(paint.x, paint.y * Stretch);

        /// <summary>Things get smaller further up the picture (further away).</summary>
        public static float SizeAt(float paintY) => Mathf.Lerp(1.15f, 0.42f, Mathf.InverseLerp(200f, 1480f, paintY));

        // ------------------------------------------------------------------ valley

        public static readonly Vector2 ValleyLake = new(240f, 1110f);
        public static readonly Vector2 ValleyLakeRadii = new(210f, 62f);

        /// <summary>The village by the lake, the farm and the houses up the valley.</summary>
        public static readonly (Vector2 p, bool church)[] ValleyHouses =
        {
            (new(90, 1215), false), (new(200, 1236), false), (new(330, 1222), true),
            (new(1000, 560), false), (new(1080, 600), false), (new(980, 1290), false), (new(860, 1355), false),
        };

        public static float ValleyHouseScale(Vector2 p) => SizeAt(p.y) * 1.6f;

        /// <summary>Top of a chalet's chimney.</summary>
        public static Vector2 ChaletChimney(Vector2 p)
        {
            float s = ValleyHouseScale(p);
            return p + new Vector2(52f * s * 0.2f, 34f * s + 30f * s * 0.7f + 8f * s);
        }

        // ------------------------------------------------------------------ winter

        public static readonly Vector2 IceLake = new(290f, 478f);
        public static readonly Vector2 IceLakeRadii = new(205f, 60f);

        /// <summary>The village of log cabins on the right and two more up the hill.</summary>
        public static readonly Vector2[] Cabins =
        {
            new(1100, 700), new(990, 730), new(1075, 830), new(955, 860), new(230, 1095), new(100, 1110),
        };

        public static float CabinScale(Vector2 p) => SizeAt(p.y) * 1.6f;

        /// <summary>Top of a cabin's chimney.</summary>
        public static Vector2 CabinChimney(Vector2 p)
        {
            float s = CabinScale(p);
            return p + new Vector2(52f * s * 0.25f, 30f * s + 28f * s * 0.65f + 9f * s);
        }

        public static readonly Vector2 LiftLow = new(20f, 1150f), LiftHigh = new(160f, 1370f);
        public static float LiftScale => SizeAt(LiftLow.y) * 1.2f;

        /// <summary>The lift cable runs between the tops of its pylons.</summary>
        public static void LiftCable(out Vector2 low, out Vector2 high)
        {
            var up = new Vector2(0f, 46f * LiftScale);
            low = LiftLow + up;
            high = LiftHigh + up;
        }

        /// <summary>The chimneys (painting space) and the size of their houses, for the smoke.</summary>
        public static IEnumerable<(Vector2 top, float scale)> Chimneys(CupLook look)
        {
            if (look == CupLook.Winter)
                foreach (var p in Cabins) yield return (CabinChimney(p), CabinScale(p));
            else
                foreach (var (p, church) in ValleyHouses)
                    if (!church) yield return (ChaletChimney(p), ValleyHouseScale(p));
        }
    }
}
