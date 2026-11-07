using SomeGame.Car;
using SomeGame.Race;
using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The gadget's button in the race HUD (bottom right), shown when you raced with a gadget from the garage: its
    /// icon on a round button, glowing and breathing while a bale is ready, greyed with NEXT LAP once used. Taps on
    /// it don't steer or start a drift.
    /// </summary>
    public class GadgetButton : MonoBehaviour
    {
        [SerializeField] RaceManager race;
        [SerializeField] UnityEngine.UI.Button button;
        [SerializeField] UnityEngine.UI.Image face, icon, glow;
        [SerializeField] TMPro.TMP_Text label;

        HayBaleGadget _gadget;
        float _usedAt = -10f;

        void Start()
        {
            button.onClick.AddListener(Use);
            gameObject.SetActive(false);
            if (race != null && race.Player != null && race.Player.TryGetComponent(out _gadget))
            {
                gameObject.SetActive(true);
                icon.sprite = GarageArt.Instance?.Get("gadget.haybale")?.full;
                var rt = (RectTransform)transform;
                SomeGame.Input.PlayerCarInput.ButtonAt = p => isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(rt, p, null);
            }
        }

        void OnDestroy() => SomeGame.Input.PlayerCarInput.ButtonAt = null;

        void Use()
        {
            if (_gadget != null && _gadget.Drop()) _usedAt = Time.time;
        }

        void Update()
        {
            if (_gadget == null) return;
            bool ready = _gadget.Ready;
            bool spent = _gadget.Charges <= 0;
            float t = Time.time;
            face.color = ready ? Theme.Ink : Theme.WithAlpha(Theme.Ink, 0.6f);
            icon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            glow.gameObject.SetActive(ready);
            glow.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * 5f));
            label.text = spent && race.State == RaceState.Racing ? "NEXT LAP" : "HAY!";
            float pop = Mathf.Clamp01((t - _usedAt) / 0.3f);
            transform.localScale = Vector3.one * (1f - 0.15f * Mathf.Sin(pop * Mathf.PI));
        }
    }
}
