using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corba
{
    /// <summary>Small helpers for building the painted uGUI hierarchy in code.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = GameData.Hex("3B2414");
        public static readonly Color Cream = GameData.Hex("FFF4DC");
        public static readonly Color Gold = GameData.Hex("F2B33D");
        public static readonly Color HostColor = GameData.Hex("E0583A");
        public static readonly Color GuestColor = GameData.Hex("4C8CD0");
        public static readonly Color DefendColor = GameData.Hex("5E93CF");
        public static readonly Color AttackColor = GameData.Hex("D9603E");

        static Font bodyFont, titleFont;
        public static Font Body => bodyFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Title => titleFont ??= LoadTitleFont();

        static Font LoadTitleFont()
        {
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(new[] { "Segoe Print", "Comic Sans MS", "Trebuchet MS" }, 48);
                if (f != null) return f;
            }
            catch { }
            return Body;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(RectTransform rt, string sprite, Color tint, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art.Sprite(sprite);
            img.color = tint;
            img.type = img.sprite != null && img.sprite.border.sqrMagnitude > 0 ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = sprite.StartsWith("icon_");
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Panel(Transform parent, string sprite, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null, Color? tint = null)
        {
            return Image(Rect(sprite, parent, anchor, pos, size, pivot), sprite, tint ?? Color.white);
        }

        public static Text Text(Transform parent, string text, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box, bool title = false, bool outline = true)
        {
            var rt = Rect("Text", parent, anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = title ? Title : Body;
            t.fontStyle = FontStyle.Bold;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            t.resizeTextMaxSize = size;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.16f, 0.08f, 0.03f, 0.9f);
                o.effectDistance = new Vector2(2f, -2f);
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0.1f, 0.05f, 0.02f, 0.45f);
                s.effectDistance = new Vector2(0f, -4f);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, int fontSize = 32, Color? tint = null)
        {
            var rt = Rect("Button_" + label, parent, anchor, pos, size);
            var img = Image(rt, "ui_button", tint ?? Color.white, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            Text(rt, label, fontSize, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 4), size - new Vector2(28, 14), true);
            rt.gameObject.AddComponent<JuicyButton>();
            return b;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }

        public static void SlideIn(RectTransform rt, Vector2 offset, float delay = 0f, float dur = 0.55f)
        {
            var home = rt.anchoredPosition;
            rt.anchoredPosition = home + offset;
            Tween.Anchored(rt, home, dur, Ease.OutBack).After(delay).Realtime();
        }
    }

    /// <summary>Hover lift, press squash, release overshoot.</summary>
    public class JuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Selectable selectable;
        Vector3 rest = Vector3.one;

        void Awake() => selectable = GetComponent<Selectable>();

        bool Interactable => selectable == null || selectable.interactable;

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Interactable) return;
            Tween.Scale(transform, rest * 1.07f, 0.25f, Ease.OutBack).Realtime();
            Tween.Rotate(transform, Quaternion.Euler(0, 0, Random.Range(-2f, 2f)), 0.25f, Ease.OutBack).Realtime();
        }

        public void OnPointerExit(PointerEventData e)
        {
            Tween.Scale(transform, rest, 0.3f, Ease.OutBack).Realtime();
            Tween.Rotate(transform, Quaternion.identity, 0.3f, Ease.OutBack).Realtime();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            Tween.Scale(transform, new Vector3(rest.x * 1.1f, rest.y * 0.86f, 1f), 0.08f, Ease.OutQuad).Realtime();
        }

        public void OnPointerUp(PointerEventData e)
        {
            Tween.Scale(transform, rest * 1.07f, 0.4f, Ease.OutElastic).Realtime();
        }
    }
}
