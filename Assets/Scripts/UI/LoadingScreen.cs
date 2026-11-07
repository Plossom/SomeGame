using System;
using System.Collections;
using SomeGame.Race;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SomeGame.UI
{
    /// <summary>
    /// Full-screen loading screen for a race: a picture of the whole track, the race name and a progress
    /// bar with the buggy riding along it. It loads the scene in the background, stays up for at least a
    /// moment (so the track can be seen), waits until the race has built its track, then fades away.
    /// Built as a prefab by the UI builder (Resources/LoadingScreen).
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("The race's rival at the wheel (their home race, or the challenge they set).")]
        [SerializeField] UnityEngine.UI.Image rival;
        [SerializeField] CanvasGroup group;
        [SerializeField] UnityEngine.UI.Image preview;
        [SerializeField] TMP_Text chapter;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text details;
        [SerializeField] UnityEngine.UI.Image barFill;
        [SerializeField] RectTransform barCar;
        [SerializeField] TMP_Text percent;
        [SerializeField, Min(0f)] float minimumSeconds = 1.4f;
        [SerializeField, Min(0.01f)] float fadeSeconds = 0.35f;

        float _shown;

        public static void Show(LevelDefinition level, string scene, Action done)
        {
            var prefab = Resources.Load<LoadingScreen>("LoadingScreen");
            if (prefab == null)
            {
                var op = SceneManager.LoadSceneAsync(scene);
                op.completed += _ => done?.Invoke();
                return;
            }
            var screen = Instantiate(prefab);
            DontDestroyOnLoad(screen.gameObject);
            screen.StartCoroutine(screen.Run(level, scene, done));
        }

        IEnumerator Run(LevelDefinition level, string scene, Action done)
        {
            // In the colours of the race's cup (the prefab is built in the Valley colours).
            var palette = Theme.OfLevel(level);
            Theme.RecolorAll(transform, Theme.Valley, palette);
            Theme.Hills(transform, palette);
            chapter.text = level != null ? level.chapter : "";
            title.text = level != null ? level.displayName.ToUpperInvariant() : "";
            details.text = level == null ? "" : level.isChallenge ? level.description : $"{level.LapsText}  ·  {level.rivalCount} rivals";
            preview.sprite = level != null ? level.preview : null;
            preview.enabled = preview.sprite != null;
            // A snowmobile rides the bar to a Winter Cup race.
            if (barCar != null && barCar.TryGetComponent(out VehicleIcon vehicle))
            {
                vehicle.Show(level != null && level.track != null && level.track.biome == SomeGame.Track.TrackLayout.Biome.Snow);
                // Online races wear the room's colours. The first paint is the cup's own accent (like on the map, and
                // like the race's recolouring leaves it at the end), so it doesn't change colour as the race opens.
                if (!GameSession.Online)
                {
                    var look = SomeGame.Car.CarLook.Player;
                    if (Theme.TryMap(look.Paint, Theme.Valley, palette, out var cupPaint)) look.Paint = cupPaint;
                    vehicle.Dress(look);
                }
            }
            // Their race: the rival comes driving in over the corner of the picture.
            var character = RivalOf(level);
            if (rival != null)
            {
                rival.gameObject.SetActive(character != null && character.driving != null);
                if (character != null) rival.sprite = character.driving;
            }
            SetProgress(0f);
            group.alpha = 0f;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime) { group.alpha = t / 0.2f; yield return null; }
            group.alpha = 1f;

            float start = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            // Loading reports 0..0.9 until it may switch over; the bar also never runs ahead of the clock,
            // so it fills smoothly even when loading is quick.
            while (op.progress < 0.9f || Time.unscaledTime - start < minimumSeconds)
            {
                float byLoad = op.progress / 0.9f;
                float byTime = (Time.unscaledTime - start) / minimumSeconds;
                SetProgress(Mathf.Min(byLoad, byTime) * 0.9f);
                yield return null;
            }
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            // The race builds its track in Start: give it a couple of frames.
            yield return null;
            yield return null;
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.15f);
            done?.Invoke();
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / fadeSeconds; yield return null; }
            Destroy(gameObject);
        }

        static RivalCharacter RivalOf(LevelDefinition level)
        {
            if (level == null) return null;
            var home = RivalCharacter.For(level);
            if (home != null) return home;
            foreach (var cup in CupList.Instance.cups)
                if (cup != null && cup.challenge == level && !string.IsNullOrEmpty(cup.challengeAfterRival))
                    return Resources.Load<RivalCharacter>("Rivals/" + cup.challengeAfterRival);
            return null;
        }

        float _shownAt = -1f;

        // The rival drives in from the side with a bump, then bounces along.
        void Update()
        {
            if (rival == null || !rival.gameObject.activeSelf) return;
            if (_shownAt < 0f) _shownAt = Time.unscaledTime;
            float t = Time.unscaledTime - _shownAt;
            float k = Mathf.Clamp01(t / 0.6f);
            float slide = 1f - Mathf.Pow(1f - k, 3f);
            var rt = rival.rectTransform;
            rt.localScale = Vector3.one * (0.6f + 0.4f * slide);
            rt.localRotation = Quaternion.Euler(0f, 0f, 2.5f * Mathf.Sin(t * 9f));
            rt.anchoredPosition = new Vector2(-260f + 500f * (1f - slide), 300f + 10f * Mathf.Abs(Mathf.Sin(t * 9f)));
        }

        void SetProgress(float value)
        {
            barFill.fillAmount = value;
            percent.text = $"LOADING  {Mathf.RoundToInt(value * 100f)}%";
            if (barCar != null)
            {
                var parent = (RectTransform)barCar.parent;
                barCar.anchoredPosition = new Vector2(Mathf.Lerp(0f, parent.rect.width, value), barCar.anchoredPosition.y);
            }
        }
    }
}
