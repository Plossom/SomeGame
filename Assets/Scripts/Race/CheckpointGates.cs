using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Race
{
    /// <summary>
    /// Makes the checkpoints visible as a pair of posts, one at each kerb; cars have to drive between
    /// them. The checkpoint the player has to pass next is highlighted and pulses.
    /// </summary>
    public class CheckpointGates : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] SomeGame.Track.Track track;
        [SerializeField] Sprite postSprite;
        [SerializeField, Min(0.1f)] float postSize = 1.1f;
        [SerializeField] Color idleColor = new(1f, 1f, 1f, 0.45f);
        [SerializeField] Color nextColor = new(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField, Min(0f)] float pulseSpeed = 4f;
        [SerializeField] int postSortingOrder = 5;

        readonly List<SpriteRenderer[]> _gates = new();
        int _highlighted = -1;

        void Start()
        {
            for (int k = 0; k < track.CheckpointCount; k++)
            {
                float d = track.CheckpointDistance(k);
                float halfSpan = track.OffRoadAt(d); // posts sit on the outer kerb edge
                Vector2 centre = track.Path.PointAt(d), normal = track.Path.NormalAt(d);
                var gate = new GameObject($"Checkpoint{k}").transform;
                gate.SetParent(transform, false);

                var renderers = new List<SpriteRenderer>
                {
                    Create(gate, postSprite, centre + normal * halfSpan, Vector2.one * postSize, 0f, postSortingOrder),
                    Create(gate, postSprite, centre - normal * halfSpan, Vector2.one * postSize, 0f, postSortingOrder),
                };
                _gates.Add(renderers.ToArray());
            }
            SetAll(idleColor);
        }

        void Update()
        {
            if (race.Player == null) return;
            int next = race.State == RaceState.Racing || race.State == RaceState.Countdown
                ? race.Player.NextCheckpoint
                : -1;
            if (next != _highlighted)
            {
                SetAll(idleColor);
                _highlighted = next;
            }
            if (_highlighted < 0 || _highlighted >= _gates.Count) return;

            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            var color = Color.Lerp(nextColor, Color.white, pulse * 0.35f);
            color.a = nextColor.a * Mathf.Lerp(0.65f, 1f, pulse);
            foreach (var r in _gates[_highlighted]) r.color = color;
        }

        void SetAll(Color color)
        {
            foreach (var gate in _gates)
                foreach (var r in gate) r.color = color;
        }

        static SpriteRenderer Create(Transform parent, Sprite sprite, Vector2 position, Vector2 size, float angle, int order)
        {
            var go = new GameObject(sprite.name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            Vector2 spriteSize = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
