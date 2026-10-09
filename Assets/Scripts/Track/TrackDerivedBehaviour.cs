using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// Base for components that generate something from the <see cref="Track"/> data. Rebuilds on enable
    /// and whenever the layout changes (also in edit mode), and destroys the meshes it created.
    /// Generated meshes are never saved; the scene only stores the renderers they are assigned to.
    /// </summary>
    [ExecuteAlways]
    public abstract class TrackDerivedBehaviour : MonoBehaviour
    {
        [SerializeField] protected Track track;

        readonly List<Mesh> _meshes = new();

        protected abstract void Build(TrackPath path, TrackLayout layout);

        protected virtual void OnEnable()
        {
            if (track == null) return;
            track.Rebuilt += ScheduleRebuild;
            Rebuild();
        }

        protected virtual void OnDisable()
        {
            if (track != null) track.Rebuilt -= ScheduleRebuild;
            DestroyMeshes();
        }

        void ScheduleRebuild()
        {
            if (Application.isPlaying)
            {
                Rebuild(); // runtime layout switch (level loading): rebuild right away
                return;
            }
#if UNITY_EDITOR
            // Layout edits arrive from OnValidate, where rebuilding directly is not allowed.
            UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Rebuild(); };
#else
            Rebuild();
#endif
        }

        void Rebuild()
        {
            DestroyMeshes();
            if (track.Layout != null) Build(track.Path, track.Layout);
        }

        /// <summary>Assigns a generated mesh to a filter and takes ownership of it.</summary>
        protected void Assign(MeshFilter filter, Mesh mesh)
        {
            _meshes.Add(mesh);
            if (filter != null) filter.sharedMesh = mesh;
        }

        void DestroyMeshes()
        {
            foreach (var mesh in _meshes)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            }
            _meshes.Clear();
        }
    }
}
