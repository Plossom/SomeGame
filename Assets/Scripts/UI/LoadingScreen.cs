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
        [Tooltip("The race's rival at the wheel (their home race, or the challenge they set), on the VS band.")]
        [SerializeField] UnityEngine.UI.Image rival;
        [Tooltip("A race against a rival with a face: a duel band under a smaller picture (your car VS them).")]
        [SerializeField] RectTransform vsBand, vsYou, vsThem, vsBadge;
        [SerializeField] VehicleIcon vsCar;
        [SerializeField] TMP_Text vsName;
        [SerializeField] PreviewFrame previewFrame;
        [SerializeField] Vector2 duelPreviewSize = new(820f, 820f), duelPreviewAt = new(0f, -980f);
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
                    var look = SomeGame.Car.CarLook.For(SomeGame.Car.CarLook.VehicleOf(level));
                    if (Theme.TryMap(look.Paint, Theme.Valley, palette, out var cupPaint)) look.Paint = cupPaint;
                    vehicle.Dress(look);
                }
            }
            // Their race: a smaller picture and the duel band below it (your car VS them at the wheel).
            var character = RivalOf(level);
            bool duel = character != null && character.driving != null && vsBand != null && !GameSession.Online;
            if (vsBand != null) vsBand.gameObject.SetActive(duel);
            if (duel)
            {
                rival.sprite = character.driving;
                vsName.text = character.displayName.ToUpperInvariant();
                var vehicleOf = SomeGame.Car.CarLook.VehicleOf(level);
                var look = SomeGame.Car.CarLook.For(vehicleOf);
                if (Theme.TryMap(look.Paint, Theme.Valley, palette, out var duelPaint)) look.Paint = duelPaint;
                vsCar.Show(vehicleOf == SomeGame.Track.TrackLayout.Vehicle.Snowmobile);
                vsCar.Dress(look);
                if (previewFrame != null)
                {
                    previewFrame.MaxSize = duelPreviewSize;
                    ((RectTransform)previewFrame.transform).anchoredPosition = duelPreviewAt;
                }
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

        // The duel band: you slide in from the left, they come driving in from the right (bouncing along), the VS
        // badge slams down between you.
        void Update()
        {
            if (vsBand == null || !vsBand.gameObject.activeSelf) return;
            if (_shownAt < 0f) _shownAt = Time.unscaledTime;
            float t = Time.unscaledTime - _shownAt;
            float In(float delay, float seconds) { float k = Mathf.Clamp01((t - delay) / seconds); return 1f - Mathf.Pow(1f - k, 3f); }
            vsBand.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, In(0f, 0.25f)), 1f);
            vsYou.anchoredPosition = new Vector2(Mathf.Lerp(-700f, 0f, In(0.15f, 0.45f)), 0f);
            vsThem.anchoredPosition = new Vector2(Mathf.Lerp(700f, 0f, In(0.15f, 0.45f)), 4f * Mathf.Abs(Mathf.Sin(t * 9f)));
            vsThem.localRotation = Quaternion.Euler(0f, 0f, 1.5f * Mathf.Sin(t * 9f));
            float slam = Mathf.Clamp01((t - 0.55f) / 0.3f);
            vsBadge.localScale = Vector3.one * (slam <= 0f ? 0f : Mathf.Lerp(2.2f, 1f, 1f - Mathf.Pow(1f - slam, 3f)) * (1f + 0.04f * Mathf.Sin(t * 5f)));
            vsBadge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-25f, -6f, slam));
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
