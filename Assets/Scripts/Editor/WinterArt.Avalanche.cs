using UnityEditor;
using UnityEngine;
using static SomeGame.EditorTools.Sdf;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// The avalanche race's scenery atlas (24 cells of 256 px, 4 x 6) for Winter Cup race 7: the mountain (pines,
    /// rocks, spires, crevasses, avalanche fences, a hut, signposts), the warnings and the rescuers (an avalanche
    /// sign, the ski patrol hut, a TV helicopter, a snow cannon, a snapped tree) and the finish stadium (grandstand,
    /// flags, scoreboard, the jump's lines, the judges' tower, the camera crane, crowds, skiers, the podium).
    /// Also a standing pine and a fallen one (the trees that crash across the road). Row 0 at the bottom.
    /// </summary>
    public static partial class WinterArt
    {
        public static readonly string[] AvalancheCells =
        {
            "SnowPine", "FrostTree", "SnowRock", "Boulders",
            "RockSpire", "Crevasse", "SnowDrift", "AvalancheFence",
            "MountainHut", "Signpost", "AvalancheSign", "PatrolHut",
            "Grandstand", "Flags", "Scoreboard", "JumpLines",
            "JudgeTower", "CameraCrane", "WinterCrowd", "Skiers",
            "Helicopter", "SnowCannon", "BrokenTree", "Podium",
        };

        const int AvalancheRows = 6;

        /// <summary>UV rect of an avalanche scenery cell by name (row 0 at the bottom).</summary>
        public static Rect AvalancheCell(string name)
        {
            int i = System.Array.IndexOf(AvalancheCells, name);
            return new Rect((i % 4) * 0.25f, (i / 4) / (float)AvalancheRows, 0.25f, 1f / AvalancheRows);
        }

        [MenuItem("SomeGame/Generate Avalanche Art")]
        public static void GenerateAvalanche()
        {
            Write(ArtGenerator.Folder, "SceneryAvalanche", 1024, 256 * AvalancheRows, AvalanchePixel, 128, false);
            ArtGenerator.Material("SceneryAvalanche", "SceneryAvalanche");
            Write(ArtGenerator.Folder, "StandingPine", 256, 256, SnowPine, 256, false);
            Write(ArtGenerator.Folder, "FallenPine", 128, 512, FallenPinePixel, 128, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Color AvalanchePixel(float x, float y)
        {
            int col = (int)(x / 256), row = (int)(y / 256);
            float lx = x - col * 256, ly = y - row * 256;
            return AvalancheCells[row * 4 + col] switch
            {
                "SnowPine" => SnowPine(lx, ly),
                "FrostTree" => FrostTree(lx, ly),
                "SnowRock" => SnowRock(lx, ly),
                "Boulders" => Boulders(lx, ly),
                "RockSpire" => RockSpire(lx, ly),
                "Crevasse" => Crevasse(lx, ly),
                "SnowDrift" => SnowDrift(lx, ly),
                "AvalancheFence" => AvalancheFence(lx, ly),
                "MountainHut" => MountainHut(lx, ly),
                "Signpost" => Signpost(lx, ly),
                "AvalancheSign" => AvalancheSign(lx, ly),
                "PatrolHut" => PatrolHut(lx, ly),
                "Grandstand" => Grandstand(lx, ly),
                "Flags" => Flags(lx, ly),
                "Scoreboard" => Scoreboard(lx, ly),
                "JumpLines" => JumpLines(lx, ly),
                "JudgeTower" => JudgeTower(lx, ly),
                "CameraCrane" => CameraCrane(lx, ly),
                "WinterCrowd" => WinterCrowd(lx, ly),
                "Skiers" => Skiers(lx, ly),
                "Helicopter" => Helicopter(lx, ly),
                "SnowCannon" => SnowCannon(lx, ly),
                "BrokenTree" => BrokenTree(lx, ly),
                _ => Podium(lx, ly),
            };
        }

        // A yellow warning sign on a post: a diamond with a black avalanche picture (a slope, tumbling snow).
        static Color AvalancheSign(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Circle(x, y, 140, 108, 22), 6f));
            c = Over(c, Hex("6B6F78"), Fill(Circle(x, y, 128, 118, 10)));
            var q = Local(x, y, 128, 150, 45f, 1f);
            float sign = RoundRect(q.x, q.y, 0, 0, 62, 62, 8);
            c = Over(c, Shadow, Soft(RoundRect(q.x - 8, q.y + 8, 0, 0, 62, 62, 8), 8f));
            c = Over(c, Hex("F5C842"), Fill(sign));
            c = Over(c, Outline, Stroke(sign, 5f));
            c = Over(c, Outline, Stroke(RoundRect(q.x, q.y, 0, 0, 52, 52, 6), 3f));
            // The picture (upright, in the sign's own frame turned back).
            var p = Local(x, y, 128, 150, 0f, 1f);
            float slope = Triangle(p.x, p.y, new Vector2(-40, -26), new Vector2(40, -26), new Vector2(-40, 26));
            c = Over(c, Outline, Fill(slope) * Fill(sign));
            foreach (var (bx, by, r) in new[] { (6f, 6f, 9f), (20f, -6f, 7f), (-6f, 20f, 6f), (26f, 12f, 5f) })
                c = Over(c, Outline, Fill(Circle(p.x, p.y, bx, by, r)) * Fill(sign));
            return c;
        }

        // The ski patrol hut: a red cabin with a white cross on its snowy roof, a rescue sled and a dog beside it.
        static Color PatrolHut(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            float hut = RoundRect(x, y, 112, 140, 76, 64, 8);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 126, 124, 76, 64, 8), 12f));
            c = Over(c, Hex("C0392B"), Fill(hut));
            float roof = RoundRect(x, y, 112, 140, 70, 58, 6);
            c = Over(c, Snow, Fill(roof));
            c = Over(c, SnowShade, Fill(roof) * Mathf.Clamp01((x - y + 40f) / 120f) * 0.6f);
            float cross = Mathf.Min(RoundRect(x, y, 112, 140, 30, 9, 2), RoundRect(x, y, 112, 140, 9, 30, 2));
            c = Over(c, Hex("E2393F"), Fill(cross));
            c = Over(c, Outline, Stroke(hut, 4f));
            // The rescue sled (orange) and the dog.
            float sled = RoundRect(x, y, 214, 70, 16, 44, 8);
            c = Over(c, Hex("F28C28"), Fill(sled));
            c = Over(c, Outline, Stroke(sled, 3f));
            float dog = SMin(Ellipse(x, y, 60, 46, 22, 12), Circle(x, y, 86, 50, 9), 6f);
            c = Over(c, Hex("6B4426"), Fill(dog));
            c = Over(c, Hex("E8D3B0"), Fill(Ellipse(x, y, 56, 44, 10, 6)) * 0.8f);
            c = Over(c, Hex("E2393F"), Fill(RoundRect(x, y, 66, 46, 8, 10, 2)));
            return Over(c, Outline, Stroke(dog, 2.5f));
        }

        // A TV helicopter from above: a red body with a bubble cockpit, a long tail, the rotor's blur over it.
        static Color Helicopter(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, new Color(0f, 0.05f, 0.15f, 0.25f), Fill(Ellipse(x, y, 150, 100, 40, 70)));
            float tail = Capsule(x, y, 128, 120, 128, 30, 8);
            float body = Ellipse(x, y, 128, 160, 34, 52);
            c = Over(c, Hex("C0392B"), Fill(Mathf.Min(tail, body)));
            c = Over(c, Hex("9BD3F0"), Fill(Ellipse(x, y, 128, 190, 24, 22)));
            c = Over(c, Color.white, Fill(Ellipse(x, y, 120, 196, 8, 6)) * 0.7f);
            c = Over(c, Color.white, Fill(RoundRect(x, y, 128, 150, 18, 8, 3)));
            c = Over(c, Outline, Stroke(Mathf.Min(tail, body), 3f));
            float tailRotor = RoundRect(x, y, 128, 30, 26, 4, 2);
            c = Over(c, Hex("2A2F38"), Fill(tailRotor));
            // The rotor: a soft disc of blur with two blades.
            c = Over(c, new Color(0.15f, 0.17f, 0.2f, 0.18f), Fill(Circle(x, y, 128, 160, 112)));
            foreach (float a in new[] { 0.3f, 0.3f + Mathf.PI * 0.5f })
            {
                Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
                c = Over(c, Hex("2A2F38"), Fill(SegDist(new Vector2(x, y), new Vector2(128, 160) - d * 108f, new Vector2(128, 160) + d * 108f) - 3f));
            }
            return Over(c, Hex("555C68"), Fill(Circle(x, y, 128, 160, 8)));
        }

        // A snow cannon on its stand: a fat barrel with a fan at the back, spraying a plume of snow.
        static Color SnowCannon(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            // The plume of snow (soft, white).
            for (int k = 0; k < 8; k++)
            {
                float px = 150 + k * 12, py = 150 + k * 9;
                c = Over(c, new Color(1f, 1f, 1f, 0.55f - k * 0.05f), Fill(Circle(x, y, px, py, 14 + k * 4)));
            }
            c = Over(c, Shadow, Soft(Circle(x, y, 108, 92, 46), 10f));
            float barrel = RoundRect(Local(x, y, 112, 120, 40f, 1f).x, Local(x, y, 112, 120, 40f, 1f).y, 0, 0, 30, 58, 14);
            c = Over(c, Hex("E8E8EA"), Fill(barrel));
            c = Over(c, Hex("2E9A57"), Fill(barrel + 6f) * Fill(-(barrel + 14f)));
            c = Over(c, Outline, Stroke(barrel, 3.5f));
            float fan = Circle(x, y, 84, 88, 22);
            c = Over(c, Hex("2A2F38"), Fill(fan));
            float a = Mathf.Atan2(y - 88, x - 84);
            if (Mathf.Repeat(a * 3f / Mathf.PI, 1f) < 0.45f) c = Over(c, Hex("6B6F78"), Fill(fan + 4f));
            return Over(c, Outline, Stroke(fan, 3f));
        }

        // A pine snapped by an avalanche: the stump, the broken trunk lying beside it, branches heavy with snow.
        static Color BrokenTree(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(Capsule(x, y, 70, 50, 210, 200, 18), 10f));
            float trunk = Capsule(x, y, 60, 64, 200, 210, 13);
            c = Over(c, Hex("6B4426"), Fill(trunk));
            c = Over(c, Hex("8A5A34"), Fill(Capsule(x, y, 58, 68, 198, 214, 5)));
            for (int k = 1; k < 6; k++)
            {
                Vector2 on = Vector2.Lerp(new Vector2(60, 64), new Vector2(200, 210), k / 6f);
                foreach (float s in new[] { -1f, 1f })
                {
                    float br = Capsule(x, y, on.x, on.y, on.x + s * 30f - 12f, on.y - s * 26f + 10f, 6);
                    c = Over(c, Hex("2D5A44"), Fill(br));
                    c = Over(c, Snow, Fill(br + 2f) * 0.7f);
                }
            }
            c = Over(c, Outline, Stroke(trunk, 3f));
            float stump = Circle(x, y, 52, 56, 18);
            c = Over(c, Hex("C9A27A"), Fill(stump));
            for (int r = 4; r < 18; r += 5) c = Over(c, Hex("8A5A34"), Fill(Mathf.Abs(Dist(x, y, 52, 56) - r) - 0.8f) * Fill(stump));
            return Over(c, Outline, Stroke(stump, 3f));
        }

        // A pine lying across the road (along the picture, its foot at the bottom): the trunk, branches either side
        // weighed down with snow, the broken root plate at the foot.
        static Color FallenPinePixel(float x, float y)
        {
            Color c = new(0, 0, 0, 0);
            c = Over(c, Shadow, Soft(RoundRect(x, y, 72, 256, 40, 236, 20), 10f));
            for (int k = 0; k < 14; k++)
            {
                float by = 70 + k * 30, spread = Mathf.Lerp(54f, 18f, k / 13f);
                foreach (float s in new[] { -1f, 1f })
                {
                    float br = Triangle(x, y, new Vector2(64, by), new Vector2(64 + s * spread, by - 22), new Vector2(64 + s * spread * 0.7f, by + 10));
                    c = Over(c, Hex("2D5A44"), Fill(br));
                    c = Over(c, Snow, Fill(br + 3f) * Fill(Circle(x, y, 64 + s * spread * 0.5f, by - 4, spread * 0.5f)) * 0.8f);
                    c = Over(c, Outline, Stroke(br, 2f) * 0.6f);
                }
            }
            float trunk = Capsule(x, y, 64, 40, 64, 470, 10);
            c = Over(c, Hex("6B4426"), Fill(trunk));
            c = Over(c, Hex("8A5A34"), Fill(Capsule(x, y, 61, 40, 61, 470, 3)));
            c = Over(c, Outline, Stroke(trunk, 2.5f));
            float roots = Ellipse(x, y, 64, 30, 34, 18);
            c = Over(c, Hex("5A3E2B"), Fill(roots));
            c = Over(c, Hex("8A7A6A"), Fill(Ellipse(x, y, 64, 26, 24, 9)) * 0.7f);
            return Over(c, Outline, Stroke(roots, 3f));
        }
    }
}
