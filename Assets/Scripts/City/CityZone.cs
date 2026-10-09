using SomeGame.Race;
using UnityEngine;

namespace SomeGame.City
{
    public enum CityStyle { Downtown, Harbor, Summit, Underground, Skyline }

    /// <summary>
    /// One district's area of the city map (on the XZ plane) and how it is built. The generator fills
    /// it; the city screen uses it for taps, labels and camera focus.
    /// </summary>
    public class CityZone : MonoBehaviour
    {
        [SerializeField] DistrictDefinition district;
        [SerializeField] CityStyle style;
        [Tooltip("Area on the ground: x/y = min corner (world X, Z), width/height = size.")]
        [SerializeField] Rect area = new(-40, -40, 80, 60);
        [Tooltip("Landmark position (world X, Z). The district label floats above it.")]
        [SerializeField] Vector2 landmark;
        [SerializeField] int seed = 1;

        public DistrictDefinition District => district;
        public CityStyle Style => style;
        public Rect Area => area;
        public Vector2 Landmark => landmark;
        public int Seed => seed;
        public Vector3 Center => new(area.center.x, 0f, area.center.y);
        public Vector3 LabelPoint => new(landmark.x, LabelHeight, landmark.y);
        public float LabelHeight { get; set; } = 30f;

        public bool Contains(Vector3 world) => area.Contains(new Vector2(world.x, world.z));

        void OnDrawGizmos()
        {
            Gizmos.color = district != null ? district.accent : Color.white;
            Vector3 c = Center, s = new(area.width, 0.1f, area.height);
            Gizmos.DrawWireCube(c, s);
            Gizmos.DrawWireSphere(new Vector3(landmark.x, 0, landmark.y), 2f);
        }
    }
}
