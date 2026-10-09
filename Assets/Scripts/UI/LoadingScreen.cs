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
            chapter.text = level != null ? level.chapter : "";
            title.text = level != null ? level.displayName.ToUpperInvariant() : "";
            details.text = level != null ? $"{level.laps} laps  ·  {level.rivalCount} rivals" : "";
            preview.sprite = level != null ? level.preview : null;
            preview.enabled = preview.sprite != null;
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
