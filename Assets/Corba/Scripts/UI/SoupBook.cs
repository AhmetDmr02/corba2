using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Corba.UIKit;

namespace Corba
{
    /// <summary>Reference card for all five soups: what they carry like, build, recruit and throw.</summary>
    public class SoupBook : MonoBehaviour
    {
        public static SoupBook Instance { get; private set; }

        RectTransform root, panel;
        readonly Text[] defenceCosts = new Text[10];
        readonly Text[] unitCosts = new Text[10];
        bool open;

        public bool IsOpen => open;

        void Start()
        {
            Instance = this;
            root = Stretch("SoupBook", CorbaUI.Instance.Root);
            Image(Stretch("Dim", root), "ui_vignette", new Color(1, 1, 1, 0.93f), true);
            panel = Panel(root, "ui_panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1760, 900)).rectTransform;
            Text(panel, "", 50, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(800, 70), true).Bind("book.title");
            Text(panel, "", 22, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(1500, 34), false, false).Bind("book.sub");
            for (int s = 0; s < 5; s++) BuildColumn(s);
            Button(panel, "", new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(260, 68), Toggle, 26).Bind("book.close");
            root.gameObject.SetActive(false);
        }

        void BuildColumn(int s)
        {
            var soup = GameData.Soups[s];
            var type = (SoupType)s;
            float x = -680 + s * 340;
            var col = Panel(panel, "ui_card", new Vector2(0.5f, 1f), new Vector2(x, -470), new Vector2(322, 700), null, Color.Lerp(soup.Identity, Color.white, 0.55f)).rectTransform;

            var header = Panel(col, "ui_chip", new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(296, 70), null, soup.Identity).rectTransform;
            Panel(header, soup.Glyph, new Vector2(0f, 0.5f), new Vector2(40, 0), new Vector2(54, 54));
            Text(header, "", 28, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(170, 10), new Vector2(200, 36), true).Bind(() => Loc.SoupName(type));
            Text(header, "", 17, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(170, -18), new Vector2(200, 24)).Bind(() => Loc.Upper(Loc.SoupKeyword(type)));

            Panel(col, "icon_soup_" + s, new Vector2(0.5f, 1f), new Vector2(0, -132), new Vector2(84, 84));
            Text(col, "", 17, Ink, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -198), new Vector2(290, 46), false, false).Bind(() => Loc.SoupTheme(type));
            Text(col, "", 15, Ink, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -238), new Vector2(290, 40), false, false).Bind(() => "<i>" + Loc.F("book.carrying", Loc.SoupCarry(type)) + "</i>");

            Text(col, "", 18, DefendColor, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(128, -282), new Vector2(200, 26), true, false).Bind("book.defend");
            for (int o = 0; o < 2; o++)
            {
                var id = GameData.DefenceOption((SoupType)s, o);
                Row(col, -330 - o * 80, "icon_" + GameData.Defence(id).Model, () => Loc.DefenceName(id), () => Loc.DefenceBlurb(id), out defenceCosts[(int)id]);
            }

            Text(col, "", 18, AttackColor, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(128, -454), new Vector2(200, 26), true, false).Bind("book.attack");
            for (int o = 0; o < 2; o++)
            {
                var id = GameData.UnitOption((SoupType)s, o);
                Row(col, -502 - o * 80, "icon_" + GameData.Unit(id).Model, () => Loc.UnitName(id), () => Loc.UnitBlurb(id), out unitCosts[(int)id]);
            }

            var ladleBox = Panel(col, "ui_chip", new Vector2(0.5f, 0f), new Vector2(0, 46), new Vector2(296, 70), null, Color.Lerp(soup.Identity, Color.black, 0.15f)).rectTransform;
            Text(ladleBox, "", 18, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(280, 26), true).Bind(() => Loc.F("book.ladle", Loc.LadleName(s)));
            Text(ladleBox, "", 14, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(280, 34)).Bind(() => Loc.LadleBlurb(s));
        }

        static void Row(RectTransform parent, float y, string icon, System.Func<string> name, System.Func<string> blurb, out Text cost)
        {
            var row = Rect("Row", parent, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(296, 74));
            Image(row, "ui_panel_dark", new Color(1, 1, 1, 0.92f));
            Panel(row, icon, new Vector2(0f, 0.5f), new Vector2(38, 0), new Vector2(62, 62));
            Text(row, "", 17, Gold, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(160, 14), new Vector2(180, 24), true).Bind(name);
            Text(row, "", 13, Cream, TextAnchor.UpperLeft, new Vector2(0f, 0.5f), new Vector2(160, -14), new Vector2(180, 34)).Bind(blurb);
            cost = Text(row, "", 16, Cream, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-30, -16), new Vector2(50, 22), true);
        }

        public void Toggle()
        {
            open = !open;
            root.gameObject.SetActive(open);
            if (!open) return;
            root.SetAsLastSibling();
            panel.Pop(0.7f, 0.4f);
            RefreshCosts();
        }

        void RefreshCosts()
        {
            var d = MatchDirector.Instance;
            for (int i = 0; i < 10; i++)
            {
                defenceCosts[i].text = (d != null ? d.BuildCost((DefenceId)i) : GameData.Defence((DefenceId)i).Cost).ToString();
                unitCosts[i].text = (d != null ? d.RecruitCost((UnitId)i) : GameData.Unit((UnitId)i).Cost).ToString();
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame && !CorbaUI.TypingInField) Toggle();
            if (open && Time.frameCount % 15 == 0) RefreshCosts();
        }
    }
}
