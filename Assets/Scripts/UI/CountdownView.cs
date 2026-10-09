using SomeGame.Race;
using TMPro;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>Big 3-2-1-GO in the middle of the screen.</summary>
    public class CountdownView : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] TMP_Text label;
        [SerializeField, Min(0f)] float goVisibleSeconds = 0.8f;
        [Tooltip("Soft glow behind the number, tinted with it.")]
        [SerializeField] UnityEngine.UI.Image glow;

        float _hideAt = -1f;

        void OnEnable() => race.CountdownTick += Show;
        void OnDisable() => race.CountdownTick -= Show;

        void Show(int n)
        {
            label.gameObject.SetActive(true);
            label.text = n > 0 ? n.ToString() : "GO!";
            label.color = n > 0 ? NeonTheme.Text : NeonTheme.Lime;
            if (glow != null) glow.color = NeonTheme.WithAlpha(n > 0 ? NeonTheme.Cyan : NeonTheme.Lime, 0.55f);
            label.transform.localScale = Vector3.one * 1.4f;
            _hideAt = n > 0 ? -1f : Time.time + goVisibleSeconds;
        }

        void Update()
        {
            if (!label.gameObject.activeSelf) return;
            // Quick "pop" toward normal size after each number.
            label.transform.localScale = Vector3.Lerp(label.transform.localScale, Vector3.one, Time.deltaTime * 10f);
            if (_hideAt > 0f && Time.time >= _hideAt) label.gameObject.SetActive(false);
        }
    }
}
