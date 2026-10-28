using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    /// <summary>Thin wrapper over the legacy Animation component that the rigged FBX models import with.</summary>
    public class CharacterAnimator : MonoBehaviour
    {
        static readonly HashSet<string> Loops = new HashSet<string> { "Idle", "Walk", "Run", "CarryIdle", "CarryWalk", "CarryRun" };

        Animation anim;
        readonly Dictionary<string, string> clips = new Dictionary<string, string>();
        string baseState;
        float oneShotUntil;

        public bool HasRig => anim != null && clips.Count > 0;

        public static CharacterAnimator Attach(GameObject model)
        {
            var a = model.AddComponent<CharacterAnimator>();
            a.anim = model.GetComponentInChildren<Animation>();
            if (a.anim != null)
            {
                a.anim.cullingType = AnimationCullingType.BasedOnRenderers;
                foreach (AnimationState s in a.anim)
                {
                    string shortName = s.name;
                    int bar = shortName.LastIndexOf('|');
                    if (bar >= 0) shortName = shortName.Substring(bar + 1);
                    a.clips[shortName] = s.name;
                    s.wrapMode = Loops.Contains(shortName) ? WrapMode.Loop : WrapMode.ClampForever;
                }
            }
            a.SetBase("Idle");
            return a;
        }

        public bool Has(string state) => clips.ContainsKey(state);

        public void SetBase(string state, float speed = 1f)
        {
            if (!HasRig || !clips.TryGetValue(state, out var clip)) return;
            anim[clip].speed = speed;
            if (baseState == state) return;
            baseState = state;
            if (Time.time >= oneShotUntil) anim.CrossFade(clip, 0.15f);
        }

        public void Trigger(string state, float speed = 1f)
        {
            if (!HasRig || !clips.TryGetValue(state, out var clip)) return;
            var s = anim[clip];
            s.speed = speed;
            s.time = 0f;
            anim.CrossFade(clip, 0.06f);
            oneShotUntil = Time.time + s.length / Mathf.Max(speed, 0.01f);
        }

        void Update()
        {
            if (!HasRig || baseState == null || oneShotUntil <= 0f || Time.time < oneShotUntil) return;
            oneShotUntil = 0f;
            anim.CrossFade(clips[baseState], 0.18f);
        }
    }
}
