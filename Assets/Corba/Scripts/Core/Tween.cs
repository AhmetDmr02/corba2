using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corba
{
    public enum Ease { Linear, OutQuad, OutCubic, InOutSine, OutBack, InBack, OutElastic, OutBounce, Spring }

    public class Tween
    {
        public object Target;
        public float Duration, Delay, Elapsed;
        public Ease Ease;
        public bool Unscaled;
        public Action<float> Apply;
        public Action Done;
        public bool Killed;

        public Tween OnDone(Action a) { Done += a; return this; }
        public Tween After(float delay) { Delay = delay; return this; }
        public Tween Realtime() { Unscaled = true; return this; }
        public void Kill() => Killed = true;

        static TweenRunner runner;
        static readonly List<Tween> active = new List<Tween>();

        static Tween Add(object target, float duration, Ease ease, Action<float> apply, bool exclusive = true)
        {
            if (runner == null)
            {
                runner = new GameObject("Tweens").AddComponent<TweenRunner>();
                UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
            }
            if (exclusive && target != null) KillTarget(target, apply.Method.Name);
            var t = new Tween { Target = target, Duration = Mathf.Max(duration, 1e-4f), Ease = ease, Apply = apply, Channel = apply.Method.Name };
            active.Add(t);
            return t;
        }

        string Channel;

        static void KillTarget(object target, string channel)
        {
            foreach (var t in active)
                if (ReferenceEquals(t.Target, target) && t.Channel == channel) t.Killed = true;
        }

        public static void KillAll(object target)
        {
            foreach (var t in active)
                if (ReferenceEquals(t.Target, target)) t.Killed = true;
        }

        internal static void Step()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var t = active[i];
                if (t.Killed || (t.Target is UnityEngine.Object o && o == null))
                {
                    active.RemoveAt(i);
                    continue;
                }
                float dt = t.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                if (t.Delay > 0f) { t.Delay -= dt; continue; }
                t.Elapsed += dt;
                float k = Mathf.Clamp01(t.Elapsed / t.Duration);
                t.Apply(Evaluate(t.Ease, k));
                if (k >= 1f)
                {
                    active.RemoveAt(i);
                    t.Done?.Invoke();
                }
            }
        }

        public static float Evaluate(Ease e, float t)
        {
            switch (e)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.InBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f) return t;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f, d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
                case Ease.Spring:
                    return 1f - Mathf.Exp(-6f * t) * Mathf.Cos(t * 14f);
                default: return t;
            }
        }

        public static Tween Value(object target, float from, float to, float dur, Ease ease, Action<float> set)
        {
            return Add(target, dur, ease, k => set(Mathf.LerpUnclamped(from, to, k)), false);
        }

        public static Tween Scale(Transform t, Vector3 to, float dur, Ease ease = Ease.OutBack)
        {
            Vector3 from = t.localScale;
            return Add(t, dur, ease, k => { if (t) t.localScale = Vector3.LerpUnclamped(from, to, k); });
        }

        public static Tween ScaleFrom(Transform t, Vector3 from, float dur, Ease ease = Ease.OutBack)
        {
            Vector3 to = t.localScale;
            t.localScale = from;
            return Add(t, dur, ease, k => { if (t) t.localScale = Vector3.LerpUnclamped(from, to, k); });
        }

        public static Tween LocalMove(Transform t, Vector3 to, float dur, Ease ease = Ease.OutCubic)
        {
            Vector3 from = t.localPosition;
            return Add(t, dur, ease, k => { if (t) t.localPosition = Vector3.LerpUnclamped(from, to, k); });
        }

        public static Tween Move(Transform t, Vector3 to, float dur, Ease ease = Ease.OutCubic)
        {
            Vector3 from = t.position;
            return Add(t, dur, ease, k => { if (t) t.position = Vector3.LerpUnclamped(from, to, k); });
        }

        public static Tween Anchored(RectTransform r, Vector2 to, float dur, Ease ease = Ease.OutBack)
        {
            Vector2 from = r.anchoredPosition;
            return Add(r, dur, ease, k => { if (r) r.anchoredPosition = Vector2.LerpUnclamped(from, to, k); });
        }

        public static Tween Rotate(Transform t, Quaternion to, float dur, Ease ease = Ease.OutCubic)
        {
            Quaternion from = t.localRotation;
            return Add(t, dur, ease, k => { if (t) t.localRotation = Quaternion.SlerpUnclamped(from, to, k); });
        }

        public static Tween Fade(CanvasGroup g, float to, float dur, Ease ease = Ease.OutQuad)
        {
            float from = g.alpha;
            return Add(g, dur, ease, k => { if (g) g.alpha = Mathf.LerpUnclamped(from, to, k); });
        }

        public static Tween Color(Graphic g, Color to, float dur, Ease ease = Ease.OutQuad)
        {
            Color from = g.color;
            return Add(g, dur, ease, k => { if (g) g.color = UnityEngine.Color.LerpUnclamped(from, to, k); });
        }

        /// <summary>Elastic kick around the current scale that settles back to it.</summary>
        public static Tween Punch(Transform t, float amount, float dur = 0.35f)
        {
            var baseScale = Rest(t);
            return Add(t, dur, Ease.Linear, k =>
            {
                if (!t) return;
                float decay = 1f - k;
                float wave = Mathf.Sin(k * Mathf.PI * 3f) * decay;
                t.localScale = new Vector3(baseScale.x * (1f + amount * wave), baseScale.y * (1f - amount * 0.6f * wave), baseScale.z * (1f + amount * wave));
                if (k >= 1f) t.localScale = baseScale;
            });
        }

        /// <summary>Squash-and-stretch landing: wide and short, then tall, then rest.</summary>
        public static Tween Squash(Transform t, float amount = 0.25f, float dur = 0.4f)
        {
            var baseScale = Rest(t);
            return Add(t, dur, Ease.Linear, k =>
            {
                if (!t) return;
                float s = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * amount;
                t.localScale = new Vector3(baseScale.x * (1f + s), baseScale.y * (1f - s * 1.4f), baseScale.z * (1f + s));
                if (k >= 1f) t.localScale = baseScale;
            }, true);
        }

        /// <summary>Records the scale that punch/squash effects return to. Pop sets it to its end scale.</summary>
        public static void SetRest(Transform t, Vector3 scale) => restScale[t] = scale;

        public static Vector3 Rest(Transform t)
        {
            if (!restScale.TryGetValue(t, out var s))
            {
                s = t.localScale;
                restScale[t] = s;
            }
            return s;
        }

        static readonly Dictionary<Transform, Vector3> restScale = new Dictionary<Transform, Vector3>();

        public static Tween Shake(RectTransform r, float amount, float dur)
        {
            Vector2 basePos = r.anchoredPosition;
            return Add(r, dur, Ease.Linear, k =>
            {
                if (!r) return;
                float d = (1f - k) * amount;
                r.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * 60f) * d, Mathf.Cos(k * 47f) * d * 0.5f);
                if (k >= 1f) r.anchoredPosition = basePos;
            });
        }

        public static Tween Wait(float seconds, Action a)
        {
            var t = Add(null, seconds, Ease.Linear, _ => { }, false);
            t.Done += a;
            return t;
        }

        class TweenRunner : MonoBehaviour
        {
            void Update() => Step();
        }
    }

    public static class TweenExtensions
    {
        public static void Pop(this Transform t, float from = 0.4f, float dur = 0.45f)
        {
            var rest = Tween.Rest(t);
            Tween.KillAll(t);
            t.localScale = rest;
            Tween.ScaleFrom(t, rest * from, dur, Ease.OutBack);
        }
    }
}
