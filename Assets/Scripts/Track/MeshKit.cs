using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Building blocks for the procedural vertex-colour meshes of the high-mountain set pieces (thin ice, cornices,
    /// rope bridges, the ski hill...): quads, triangles, discs and rings into lists, a mesh from the lists, a renderer
    /// for it, and a one-shot particle burst. Colours are given as seen on screen and stored linear.
    /// </summary>
    public sealed class MeshKit
    {
        public readonly List<Vector3> V = new();
        public readonly List<Color> C = new();
        public readonly List<int> T = new();

        public int Count => V.Count;

        public void Clear()
        {
            V.Clear(); C.Clear(); T.Clear();
        }

        /// <summary>Screen colour to the linear vertex colour the project renders with.</summary>
        public static Color Lin(Color c)
        {
            var l = c.linear;
            l.a = c.a;
            return l;
        }

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = alpha;
            return Lin(c);
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color) => Quad(a, b, c, d, color, color, color, color);

        /// <summary>A quad with a colour per corner (both faces, so winding never hides it).</summary>
        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            C.Add(ca); C.Add(cb); C.Add(cc); C.Add(cd);
            T.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color color) => Tri(a, b, c, color, color, color);

        public void Tri(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            C.Add(ca); C.Add(cb); C.Add(cc);
            T.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 1 });
        }

        /// <summary>A line segment as a thin quad.</summary>
        public void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 n = (b - a).normalized;
            n = new Vector2(-n.y, n.x) * (width * 0.5f);
            Quad(a - n, b - n, b + n, a + n, color);
        }

        /// <summary>A filled disc (an ellipse with <paramref name="radii"/>, turned by <paramref name="angle"/> degrees), centre and edge colours.</summary>
        public void Disc(Vector2 centre, Vector2 radii, float angle, Color inner, Color outer, int segments = 24)
        {
            float a = angle * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            Vector2 At(int k)
            {
                float u = k * Mathf.PI * 2f / segments;
                Vector2 l = new(Mathf.Cos(u) * radii.x, Mathf.Sin(u) * radii.y);
                return centre + new Vector2(l.x * cs - l.y * sn, l.x * sn + l.y * cs);
            }
            for (int k = 0; k < segments; k++) Tri(centre, At(k), At(k + 1), inner, outer, outer);
        }

        public void Disc(Vector2 centre, float radius, Color color, int segments = 16) => Disc(centre, Vector2.one * radius, 0f, color, color, segments);

        /// <summary>A ring between two radii.</summary>
        public void Ring(Vector2 centre, float inner, float outer, Color color, int segments = 24)
        {
            for (int k = 0; k < segments; k++)
            {
                float u0 = k * Mathf.PI * 2f / segments, u1 = (k + 1) * Mathf.PI * 2f / segments;
                Vector2 d0 = new(Mathf.Cos(u0), Mathf.Sin(u0)), d1 = new(Mathf.Cos(u1), Mathf.Sin(u1));
                Quad(centre + d0 * inner, centre + d0 * outer, centre + d1 * outer, centre + d1 * inner, color);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(V);
            mesh.SetColors(C);
            mesh.SetTriangles(T, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A child object drawing a mesh with a material at a sorting order.</summary>
        public static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, int order, out MeshFilter filter)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            filter = go.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>A particle system for bursts (Emit), world space.</summary>
        public static ParticleSystem Burst(Transform parent, string name, Material material, int order, Color color,
            Vector2 speed, Vector2 size, float lifetime, float spread = 0.5f, int max = 200, float gravity = 0f)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.gravityModifier = gravity;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = spread;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.3f;
            limit.dampen = 0.1f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var col = system.colorOverLifetime;
            col.enabled = true;
            col.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            system.Play();
            return system;
        }

        /// <summary>Emits <paramref name="count"/> particles of a burst at a point.</summary>
        public static void Emit(ParticleSystem system, Vector2 at, int count)
        {
            if (system == null) return;
            system.transform.position = at;
            system.Emit(count);
        }

        /// <summary>True if a point is near enough the camera for its sound to be heard.</summary>
        public static bool Heard(Vector2 at, float margin = 6f)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector2 c = cam.transform.position;
            float h = cam.orthographicSize + margin, w = cam.orthographicSize * cam.aspect + margin;
            return Mathf.Abs(at.x - c.x) < w && Mathf.Abs(at.y - c.y) < h;
        }

        /// <summary>A deterministic 0..1 hash of two numbers.</summary>
        public static float Hash(float a, float b)
        {
            uint h = (uint)(Mathf.RoundToInt(a * 97.13f) * 73856093) ^ (uint)(Mathf.RoundToInt(b * 31.7f) * 19349663);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f;
        }
    }
}
