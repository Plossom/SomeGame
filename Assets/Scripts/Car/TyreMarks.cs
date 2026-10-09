using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Leaves skid marks from the rear wheels while the car slides. Each skid is its own
    /// TrailRenderer that is left behind to fade out when the slide ends.
    /// </summary>
    [RequireComponent(typeof(CarMovement))]
    public class TyreMarks : MonoBehaviour
    {
        [SerializeField] Material material;
        [Tooltip("Rear wheel positions in local space (car faces +Y).")]
        [SerializeField] Vector2[] wheels = { new(-0.38f, -0.55f), new(0.38f, -0.55f) };
        [SerializeField, Min(0.01f)] float width = 0.2f;
        [SerializeField, Min(0.1f)] float fadeSeconds = 4f;
        [SerializeField] Color color = new(0.12f, 0.12f, 0.12f, 0.55f);
        [SerializeField] int sortingOrder = -40;

        CarMovement _car;
        TrailRenderer[] _active;

        void Awake()
        {
            _car = GetComponent<CarMovement>();
            _active = new TrailRenderer[wheels.Length];
        }

        void LateUpdate()
        {
            bool sliding = _car.IsSliding && _car.Body.linearVelocity.sqrMagnitude > 4f;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (sliding && _active[i] == null) _active[i] = StartMark(wheels[i]);
                else if (!sliding && _active[i] != null) EndMark(i);
            }
        }

        void OnDisable()
        {
            if (_active == null) return;
            // Re-parenting is not allowed while the car is being deactivated, so marks that are still
            // attached just stop here and go away with the car.
            for (int i = 0; i < _active.Length; i++)
            {
                if (_active[i] != null) _active[i].emitting = false;
                _active[i] = null;
            }
        }

        TrailRenderer StartMark(Vector2 localPosition)
        {
            var go = new GameObject("TyreMark");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = fadeSeconds;
            trail.widthMultiplier = width;
            trail.minVertexDistance = 0.15f;
            trail.numCapVertices = 0;
            trail.sortingOrder = sortingOrder;
            trail.alignment = LineAlignment.TransformZ;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = fade;
            return trail;
        }

        void EndMark(int index)
        {
            var trail = _active[index];
            _active[index] = null;
            trail.emitting = false;
            trail.autodestruct = true;
            trail.transform.SetParent(null, true);
        }
    }
}
