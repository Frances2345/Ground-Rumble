using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GroundRumble
{
    public class FadingOut : MonoBehaviour
    {
        [System.Serializable]
        public class FadeEntry
        {
            public string label;
            public GameObject target;
            [Min(0f)] public float delay = 2f;
            [Min(0f)] public float fadeDuration = 0.5f;
            public bool deactivateWhenHidden = true;
        }

        [Header("Elementos a desvanecer")]
        [SerializeField] private List<FadeEntry> entries = new List<FadeEntry>();

        [Header("Inicio automatico")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool playOnEnable;

        private readonly List<Coroutine> routines = new List<Coroutine>();
        private readonly Dictionary<GameObject, CanvasGroup> groups = new Dictionary<GameObject, CanvasGroup>();

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        public void Play()
        {
            StopAll();

            foreach (FadeEntry entry in entries)
            {
                if (entry == null || entry.target == null)
                {
                    continue;
                }

                routines.Add(StartCoroutine(FadeRoutine(entry)));
            }
        }

        public void Play(string label)
        {
            StopAll();

            foreach (FadeEntry entry in entries)
            {
                if (entry == null || entry.target == null)
                {
                    continue;
                }

                if (entry.label == label)
                {
                    routines.Add(StartCoroutine(FadeRoutine(entry)));
                }
            }
        }

        public void Play(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            FadeEntry entry = new FadeEntry
            {
                target = target,
                delay = 0f,
                fadeDuration = 0.5f,
                deactivateWhenHidden = true
            };

            routines.Add(StartCoroutine(FadeRoutine(entry)));
        }

        public void ResetFade()
        {
            StopAll();

            foreach (FadeEntry entry in entries)
            {
                if (entry == null || entry.target == null)
                {
                    continue;
                }

                entry.target.SetActive(true);
                GetOrAddGroup(entry.target).alpha = 1f;
            }
        }

        private IEnumerator FadeRoutine(FadeEntry entry)
        {
            GameObject target = entry.target;

            if (target == null)
            {
                yield break;
            }

            target.SetActive(true);

            CanvasGroup group = GetOrAddGroup(target);
            group.alpha = 1f;

            if (entry.delay > 0f)
            {
                yield return new WaitForSecondsRealtime(entry.delay);
            }

            float duration = Mathf.Max(0.01f, entry.fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            group.alpha = 0f;

            if (entry.deactivateWhenHidden)
            {
                target.SetActive(false);
            }
        }

        private CanvasGroup GetOrAddGroup(GameObject target)
        {
            if (groups.TryGetValue(target, out CanvasGroup group) && group != null)
            {
                return group;
            }

            group = target.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            groups[target] = group;
            return group;
        }

        private void StopAll()
        {
            foreach (Coroutine routine in routines)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            routines.Clear();
        }
    }
}
