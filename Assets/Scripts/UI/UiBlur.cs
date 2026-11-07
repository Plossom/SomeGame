using System.Collections.Generic;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// Smoothly blurred copies of pictures, made on the graphics card: reduced to a quarter of the size (averaging
    /// the pixels), then blurred across and down a few times with a Gaussian blur.
    /// </summary>
    public static class UiBlur
    {
        static Material _blur;
        static readonly Dictionary<Texture, RenderTexture> _cache = new();

        public static void BlurInto(Texture source, ref RenderTexture blurred)
        {
            if (_blur == null)
            {
                var shader = Resources.Load<Shader>("CupBlur");
                if (shader != null) _blur = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            int w = Mathf.Max(1, source.width / 4), h = Mathf.Max(1, source.height / 4);
            if (blurred == null || blurred.width != w || blurred.height != h)
            {
                if (blurred != null) { blurred.Release(); Object.Destroy(blurred); }
                blurred = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "CupBlur", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                blurred.Create();
            }
            RenderTexture Temp(int tw, int th)
            {
                var t = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                return t;
            }
            var half = Temp(Mathf.Max(1, source.width / 2), Mathf.Max(1, source.height / 2));
            Graphics.Blit(source, half);
            Graphics.Blit(half, blurred);
            RenderTexture.ReleaseTemporary(half);
            if (_blur == null) return;
            var other = Temp(w, h);
            for (int pass = 0; pass < 3; pass++)
            {
                _blur.SetFloat("_Spread", 1.5f);
                _blur.SetVector("_Direction", new Vector4(1f, 0f, 0f, 0f));
                Graphics.Blit(blurred, other, _blur);
                _blur.SetVector("_Direction", new Vector4(0f, 1f, 0f, 0f));
                Graphics.Blit(other, blurred, _blur);
            }
            RenderTexture.ReleaseTemporary(other);
        }

        /// <summary>A blurred copy of a picture, made once and kept (the track previews of the race cards).</summary>
        public static Texture Blurred(Texture source)
        {
            if (source == null) return null;
            if (_cache.TryGetValue(source, out var blurred) && blurred != null && blurred.IsCreated()) return blurred;
            BlurInto(source, ref blurred);
            _cache[source] = blurred;
            return blurred;
        }
    }
}
