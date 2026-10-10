using System.Collections.Generic;
using SomeGame.Track;
using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// Keeps a car on the right level where the road crosses itself: in a tunnel the car moves to the
    /// Underground physics layer (no collisions with cars above) and is drawn under the hill; on a bridge
    /// over a tunnel it is drawn above the hill.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(TrackSensor))]
    public class CarLevel : MonoBehaviour
    {
        [Tooltip("Sorting order added to the car's sprites and effects while it is on a bridge (above the hill).")]
        [SerializeField] int bridgeSortingOffset = 30;

        static int _underground = -1;
        TrackSensor _sensor;
        readonly List<(Renderer renderer, int order)> _renderers = new();
        bool _onBridge, _inTunnel;
        int _defaultLayer;

        public bool InTunnel => _inTunnel;

        void Awake()
        {
            _sensor = GetComponent<TrackSensor>();
            _defaultLayer = gameObject.layer;
            if (_underground < 0)
            {
                _underground = LayerMask.NameToLayer("Underground");
                if (_underground >= 0)
                {
                    // Underground cars only meet each other.
                    for (int i = 0; i < 32; i++) Physics2D.IgnoreLayerCollision(_underground, i, i != _underground);
                }
            }
        }

        void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) _renderers.Add((r, r.sortingOrder));
        }

        void FixedUpdate()
        {
            var track = _sensor.Track;
            if (track == null || track.Layout.tunnels.Count == 0) return;
            float d = _sensor.Current.Distance;
            bool tunnel = track.InTunnel(d);
            bool bridge = !tunnel && track.OnBridge(d);
            if (tunnel != _inTunnel)
            {
                _inTunnel = tunnel;
                if (_underground >= 0) gameObject.layer = tunnel ? _underground : _defaultLayer;
            }
            if (bridge != _onBridge)
            {
                _onBridge = bridge;
                // Effects created later (sparks, smoke) are picked up on the next change.
                if (_renderers.Count == 0) Start();
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    int i = _renderers.FindIndex(x => x.renderer == r);
                    if (i < 0) { _renderers.Add((r, r.sortingOrder)); i = _renderers.Count - 1; }
                    r.sortingOrder = _renderers[i].order + (bridge ? bridgeSortingOffset : 0);
                }
            }
        }
    }
}
