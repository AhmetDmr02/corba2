using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Corba.UIKit;

namespace Corba
{
    public class Tutorial : MonoBehaviour
    {
        const string DoneKey = "Corba.TutorialDone";

        class Step
        {
            public string Key;
            public Func<bool> Done;
            public float MinTime;
        }

        RectTransform card;
        Text title, body;
        List<Step> steps;
        int index;
        float stepTime;
        int activeHalf = -1;
        bool pausedByMe;
        Vector3 startPos;
        WorldMarker marker;

        static bool Completed => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        void Start()
        {
            var root = CorbaUI.Instance.Root;
            card = Rect("Tutorial", root, new Vector2(0f, 0.5f), new Vector2(18, 40), new Vector2(540, 210), new Vector2(0f, 0.5f));
            Image(card, "ui_panel", Color.white);
            var chip = Panel(card, "ui_chip", new Vector2(0f, 1f), new Vector2(40, -24), new Vector2(230, 44), new Vector2(0f, 1f), Gold).rectTransform;
            title = Text(chip, "", 20, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(210, 36), true, false);
            body = Text(card, "", 23, Ink, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0, -126), new Vector2(450, 100), false, false);
            Button(card, "", new Vector2(1f, 1f), new Vector2(-84, -46), new Vector2(110, 46), Skip, 20).Bind("tut.skip");
            card.gameObject.SetActive(false);
            marker = new GameObject("ObjectiveMarker").AddComponent<WorldMarker>();
            Loc.Changed += RefreshStep;
        }

        void OnDestroy() => Loc.Changed -= RefreshStep;

        void RefreshStep()
        {
            if (steps == null || index >= steps.Count) return;
            title.text = Loc.F("tut.title", index + 1, steps.Count);
            body.text = Loc.T(steps[index].Key);
        }

        void Skip()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            Finish();
        }

        void Finish()
        {
            steps = null;
            if (card.gameObject.activeSelf)
            {
                Tween.Anchored(card, card.anchoredPosition + new Vector2(-620, 0), 0.4f, Ease.InBack).OnDone(() =>
                {
                    card.gameObject.SetActive(false);
                    card.anchoredPosition = new Vector2(18, 40);
                });
            }
            SetPause(false);
        }

        void SetPause(bool paused)
        {
            if (pausedByMe == paused) return;
            pausedByMe = paused;
            MatchDirector.Instance?.SetPausedRpc(paused);
        }

        void Update()
        {
            var d = MatchDirector.Instance;
            var me = ChefController.Local;
            marker.Target = CorbaUI.Instance.ObjectiveTarget;
            if (d == null || !d.IsSpawned || me == null || d.Mode.Value != (int)GameMode.VsAi || Completed)
            {
                if (steps != null) Finish();
                return;
            }

            int halfKey = d.Course.Value * 2 + d.Half.Value;
            if (d.Course.Value == 0 && halfKey != activeHalf && d.Phase.Value == MatchPhase.Prep)
            {
                activeHalf = halfKey;
                steps = d.IsDefender(me.Slot.Value) ? DefenderSteps(me) : AttackerSteps(d, me);
                index = 0;
                stepTime = 0f;
                startPos = me.transform.position;
                card.gameObject.SetActive(true);
                SlideIn(card, new Vector2(-620, 0), 0.6f);
                ShowStep();
                SetPause(true);
            }
            if (steps == null) return;
            if (d.Phase.Value != MatchPhase.Prep)
            {
                Finish();
                return;
            }

            stepTime += Time.deltaTime;
            var step = steps[index];
            if (stepTime >= step.MinTime && step.Done())
            {
                index++;
                stepTime = 0f;
                if (index >= steps.Count)
                {
                    if (d.Half.Value == 1) PlayerPrefs.SetInt(DoneKey, 1);
                    CorbaUI.Instance.Flash(Loc.T("flash.tutorialDone"));
                    Finish();
                    return;
                }
                ShowStep();
            }
        }

        void ShowStep()
        {
            RefreshStep();
            Tween.Punch(card, 0.08f, 0.4f);
            body.transform.Pop(0.85f, 0.3f);
        }

        List<Step> DefenderSteps(ChefController me)
        {
            return new List<Step>
            {
                new Step { Key = "tut.d1", Done = () => CorbaMap.FlatDist(me.transform.position, startPos) > 3f, MinTime = 1f },
                new Step { Key = "tut.d2", Done = () => me.Soup.Value >= 0 },
                new Step { Key = "tut.d3", Done = () => true, MinTime = 5f },
                new Step { Key = "tut.d4", Done = () => DefenceBuild.All.Count > 0 },
                new Step { Key = "tut.d5", Done = () => true, MinTime = 4f },
            };
        }

        List<Step> AttackerSteps(MatchDirector d, ChefController me)
        {
            int slot = me.Slot.Value;
            Func<int> bank = () =>
            {
                int total = 0;
                for (int s = 0; s < 5; s++) total += d.BankOf(slot, (SoupType)s);
                return total;
            };
            return new List<Step>
            {
                new Step { Key = "tut.a1", Done = () => me.Soup.Value >= 0 },
                new Step { Key = "tut.a2", Done = () => bank() > 0 },
                new Step { Key = "tut.a3", Done = () => d.Queued.Value > 0 },
                new Step { Key = "tut.a4", Done = () => true, MinTime = 4f },
            };
        }
    }

    public class WorldMarker : MonoBehaviour
    {
        public Vector3? Target;
        Transform quad;
        float t;

        void Awake()
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = FX.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = FX.Mat("icon_marker");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad = go.transform;
            quad.localScale = Vector3.one * 1.5f;
        }

        void LateUpdate()
        {
            quad.gameObject.SetActive(Target != null);
            if (Target == null) return;
            t += Time.deltaTime;
            var cam = Camera.main;
            float bounce = Mathf.Abs(Mathf.Sin(t * 4f)) * 0.6f;
            transform.position = Vector3.Lerp(transform.position, Target.Value + Vector3.up * (3.6f + bounce), 1f - Mathf.Exp(-14f * Time.deltaTime));
            if (cam != null) quad.rotation = cam.transform.rotation;
            float squash = 1f + Mathf.Sin(t * 8f) * 0.06f;
            quad.localScale = new Vector3(1.5f * squash, 1.5f / squash, 1f);
        }
    }
}
