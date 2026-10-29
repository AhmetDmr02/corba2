using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Corba.UIKit;

namespace Corba
{
    public class CorbaUI : MonoBehaviour
    {
        public static CorbaUI Instance { get; private set; }
        public static Font BodyFont => Body;
        public static Font TitleFont => Title;
        public static bool TypingInField => Instance != null && Instance.ipField != null && Instance.ipField.isFocused;

        public RectTransform Root { get; private set; }
        Canvas canvas;

        RectTransform menuRoot, lobbyRoot, hudRoot, resultRoot, helpRoot;
        InputField ipField;
        Text menuStatus, lobbyText, lobbyHint;
        Button startButton, soloButton;
        bool menuShown, hudShown, resultShown;

        // HUD
        RectTransform versus, objectiveCard, sofraCard, potCard, abilityCard, promptPill, recruitBar, buildBar, announcer;
        Text potRole;
        Image potGlyph;
        Text timerText, phaseText, courseText, hostScore, guestScore, hostName, guestName, objectiveRole, objectiveText, sofraText, potName, potAmount, spillText, promptText, announcerText, queueText, shoveKey;
        Image phaseRibbon, objectiveChip, sofraFill, potFill, sloshDot, shoveFill, edgeArrow;
        readonly Image[] hostStars = new Image[2], guestStars = new Image[2];
        readonly Text[] bankTexts = new Text[5];
        readonly RectTransform[] bankChips = new RectTransform[5];
        readonly int[] lastBank = new int[5];
        readonly List<RecruitCard> cards = new List<RecruitCard>();
        readonly Image[] buildIcons = new Image[2];
        readonly Text[] buildTexts = new Text[2];
        readonly RectTransform[] buildCards = new RectTransform[2];
        RectTransform toastRoot;
        readonly List<(RectTransform rt, CanvasGroup g, float until)> toasts = new List<(RectTransform, CanvasGroup, float)>();
        float flashUntil;
        Text flashText;

        MatchPhase lastPhase = MatchPhase.Lobby;
        int lastHalfKey = -1;
        int lastSecond = -1;
        float shownFill, shownHunger = 1f;
        int lastWinsA, lastWinsB;
        bool lastDefender;
        MatchDirector hookedDirector;

        Text resultTitle, resultBody, resultButtonText;
        Button resultButton;

        public Vector3? ObjectiveTarget { get; private set; }
        public string ObjectiveLabel { get; private set; }

        class RecruitCard
        {
            public UnitId Id;
            public Button Button;
            public RectTransform Rt;
            public Image Shade;
            public Text Cost;
            public bool WasAffordable;
        }

        void Awake()
        {
            Instance = this;
            var cgo = new GameObject("Canvas");
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)cgo.transform;

            BuildMenu();
            BuildLobby();
            BuildHud();
            BuildResult();
            BuildHelp();
            lobbyRoot.gameObject.SetActive(false);
            hudRoot.gameObject.SetActive(false);
            resultRoot.gameObject.SetActive(false);
            cgo.AddComponent<Tutorial>();
            cgo.AddComponent<SoupBook>();
        }

        #region Menu

        void BuildMenu()
        {
            menuRoot = Stretch("Menu", Root);
            Image(Stretch("Vignette", menuRoot), "ui_vignette", new Color(1, 1, 1, 0.75f));

            var title = Panel(menuRoot, "ui_banner", new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(900, 230)).rectTransform;
            Text(title, "ÇORBA", 120, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(820, 140), true);
            Text(title, "Kepçe Savaşları", 40, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -62), new Vector2(820, 56), true);
            title.gameObject.AddComponent<Bobber>().Amount = 6f;

            var panel = Panel(menuRoot, "ui_panel", new Vector2(0.5f, 0.5f), new Vector2(0, -105), new Vector2(640, 600)).rectTransform;
            Text(panel, "Carry soup. Spill less. Feed your army.", 26, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(540, 40), false, false);

            var buttons = new List<RectTransform>();
            buttons.Add(Button(panel, "Play vs AI", new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(460, 92), OnVsAi, 40, GameData.Hex("FFD9A0")).GetComponent<RectTransform>());
            buttons.Add(Button(panel, "Host Online", new Vector2(0.5f, 1f), new Vector2(-118, -244), new Vector2(226, 76), OnHost, 26).GetComponent<RectTransform>());

            var ipRt = Rect("IP", panel, new Vector2(0.5f, 1f), new Vector2(118, -228), new Vector2(226, 44));
            var ipImg = Image(ipRt, "ui_field", Color.white, true);
            ipField = ipRt.gameObject.AddComponent<InputField>();
            ipField.targetGraphic = ipImg;
            var ipText = Text(ipRt, "", 22, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 36), false, false);
            ipText.supportRichText = false;
            ipText.resizeTextForBestFit = false;
            var ph = Text(ipRt, "127.0.0.1", 22, new Color(Ink.r, Ink.g, Ink.b, 0.45f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 36), false, false);
            ipField.textComponent = ipText;
            ipField.placeholder = ph;
            ipField.text = "127.0.0.1";
            buttons.Add(ipRt);
            buttons.Add(Button(panel, "Join", new Vector2(0.5f, 1f), new Vector2(118, -282), new Vector2(226, 56), OnJoin, 24).GetComponent<RectTransform>());

            buttons.Add(Button(panel, "Practice", new Vector2(0.5f, 1f), new Vector2(-118, -350), new Vector2(226, 70), OnSolo, 26).GetComponent<RectTransform>());
            buttons.Add(Button(panel, "Watch AI vs AI", new Vector2(0.5f, 1f), new Vector2(118, -350), new Vector2(226, 70), OnAiVsAi, 22).GetComponent<RectTransform>());
            buttons.Add(Button(panel, "How to Play", new Vector2(0.5f, 1f), new Vector2(-118, -440), new Vector2(226, 70), ShowHelp, 24).GetComponent<RectTransform>());
            buttons.Add(Button(panel, "Quit", new Vector2(0.5f, 1f), new Vector2(118, -440), new Vector2(226, 70), Application.Quit, 24).GetComponent<RectTransform>());
            menuStatus = Text(panel, "", 22, GameData.Hex("9C2F1C"), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(560, 40), false, false);

            Text(menuRoot, "Netcode for GameObjects • port " + CorbaBootstrap.Port, 18, Cream, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-190, 26), new Vector2(360, 30));

            menuIntro = () =>
            {
                SlideIn(title, new Vector2(0, 420), 0f, 0.8f);
                SlideIn(panel, new Vector2(0, -900), 0.15f, 0.7f);
                for (int i = 0; i < buttons.Count; i++) buttons[i].Pop(0.2f, 0.45f);
            };
        }

        System.Action menuIntro;

        void BuildLobby()
        {
            lobbyRoot = Stretch("Lobby", Root);
            var panel = Panel(lobbyRoot, "ui_panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 440)).rectTransform;
            Text(panel, "The Kitchen", 54, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -74), new Vector2(600, 80), true);
            lobbyText = Text(panel, "", 30, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(600, 44), false, false);
            lobbyHint = Text(panel, "", 22, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(600, 40), false, false);
            startButton = Button(panel, "Start Match (Bo3)", new Vector2(0.5f, 0f), new Vector2(0, 180), new Vector2(440, 80), () => MatchDirector.Instance?.StartMatchRpc(false), 30);
            soloButton = Button(panel, "Start Solo", new Vector2(0.5f, 0f), new Vector2(-115, 80), new Vector2(220, 68), () => MatchDirector.Instance?.StartMatchRpc(true), 24);
            Button(panel, "Leave", new Vector2(0.5f, 0f), new Vector2(115, 80), new Vector2(220, 68), OnLeave, 24);
        }

        #endregion

        #region HUD

        void BuildHud()
        {
            hudRoot = Stretch("HUD", Root);

            versus = Rect("Versus", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(820, 170), new Vector2(0.5f, 1f));
            BuildPlayerCard(versus, true);
            BuildPlayerCard(versus, false);
            var medal = Panel(versus, "ui_badge", new Vector2(0.5f, 1f), new Vector2(0, -74), new Vector2(150, 150), null, GameData.Hex("F4E3C0")).rectTransform;
            timerText = Text(medal, "", 44, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(130, 60), true, false);
            courseText = Text(medal, "", 16, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -38), new Vector2(130, 24), false, false);
            phaseRibbon = Panel(versus, "ui_chip", new Vector2(0.5f, 1f), new Vector2(0, -158), new Vector2(230, 46));
            phaseText = Text(phaseRibbon.rectTransform, "", 24, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(210, 38), true);

            objectiveCard = Rect("Objective", hudRoot, new Vector2(0f, 1f), new Vector2(18, -18), new Vector2(470, 156), new Vector2(0f, 1f));
            Image(objectiveCard, "ui_panel", Color.white);
            objectiveChip = Panel(objectiveCard, "ui_chip", new Vector2(0f, 1f), new Vector2(40, -22), new Vector2(170, 42), new Vector2(0f, 1f));
            objectiveRole = Text(objectiveChip.rectTransform, "", 22, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(160, 36), true);
            objectiveText = Text(objectiveCard, "", 21, Ink, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(240, -102), new Vector2(384, 72), false, false);

            sofraCard = Rect("Sofra", hudRoot, new Vector2(1f, 1f), new Vector2(-18, -18), new Vector2(400, 104), new Vector2(1f, 1f));
            Image(sofraCard, "ui_panel_dark", Color.white);
            Text(sofraCard, "Sofra hunger", 24, Gold, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(130, -32), new Vector2(220, 34), true);
            sofraText = Text(sofraCard, "", 22, Cream, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-90, -32), new Vector2(150, 34));
            var bar = Panel(sofraCard, "ui_bar_frame", new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(350, 32)).rectTransform;
            sofraFill = Panel(bar, "ui_bar_fill", new Vector2(0f, 0.5f), new Vector2(6, 0), new Vector2(338, 20), new Vector2(0f, 0.5f), GameData.Hex("E8962E"));
            sofraFill.type = UnityEngine.UI.Image.Type.Filled;
            sofraFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            potCard = Rect("Pot", hudRoot, new Vector2(0f, 0f), new Vector2(18, 18), new Vector2(420, 250), new Vector2(0f, 0f));
            Image(potCard, "ui_panel_dark", Color.white);
            var bowl = Panel(potCard, "ui_bowl_back", new Vector2(0f, 0.5f), new Vector2(92, 26), new Vector2(150, 150)).rectTransform;
            potFill = Panel(bowl, "ui_soup_circle", new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(112, 112));
            potFill.type = UnityEngine.UI.Image.Type.Filled;
            potFill.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
            potFill.preserveAspect = false;
            Panel(bowl, "ui_bowl_rim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(146, 146));
            sloshDot = Panel(bowl, "ui_badge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            potGlyph = Panel(potCard, "glyph_shield", new Vector2(0f, 1f), new Vector2(196, -42), new Vector2(40, 40));
            potName = Text(potCard, "", 26, Gold, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(312, -42), new Vector2(180, 36), true);
            potAmount = Text(potCard, "", 40, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(282, -92), new Vector2(220, 50), true);
            spillText = Text(potCard, "", 16, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(282, -140), new Vector2(220, 40));
            potRole = Text(potCard, "", 17, Cream, TextAnchor.MiddleLeft, new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(380, 44));
            Button(hudRoot, "Soup Book [B]", new Vector2(0f, 1f), new Vector2(150, -196), new Vector2(240, 52), () => SoupBook.Instance?.Toggle(), 20);

            abilityCard = Rect("Abilities", hudRoot, new Vector2(1f, 0f), new Vector2(-18, 18), new Vector2(150, 196), new Vector2(1f, 0f));
            Image(abilityCard, "ui_panel_dark", Color.white);
            var shove = Panel(abilityCard, "ui_badge", new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(104, 104), null, GameData.Hex("D9B48A")).rectTransform;
            shoveFill = Panel(shove, "ui_badge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104), null, new Color(0.15f, 0.08f, 0.04f, 0.7f));
            shoveFill.type = UnityEngine.UI.Image.Type.Filled;
            shoveFill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            shoveFill.preserveAspect = false;
            shoveKey = Text(shove, "Q", 40, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(90, 60), true, false);
            Text(abilityCard, "Dirsek\n<size=14>elbow rival</size>", 20, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 38), new Vector2(140, 50));

            promptPill = Rect("Prompt", hudRoot, new Vector2(0.5f, 0f), new Vector2(0, 236), new Vector2(720, 66));
            Image(promptPill, "ui_panel", Color.white);
            promptText = Text(promptPill, "", 26, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(680, 50), false, false);

            BuildRecruitBar();
            BuildBuildBar();

            toastRoot = Rect("Toasts", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -232), new Vector2(800, 220), new Vector2(0.5f, 1f));
            flashText = Text(hudRoot, "", 38, GameData.Hex("FFDD8A"), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1000, 60), true);

            announcer = Rect("Announcer", hudRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1100, 200));
            announcerText = Text(announcer, "", 110, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 180), true);
            Group(announcer.gameObject).alpha = 0f;

            edgeArrow = Panel(hudRoot, "icon_arrow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            edgeArrow.gameObject.SetActive(false);
        }

        void BuildPlayerCard(RectTransform parent, bool host)
        {
            float x = host ? -250 : 250;
            var anchorSide = new Vector2(host ? 0f : 1f, 1f);
            float dir = host ? 1f : -1f;
            var align = host ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            var card = Panel(parent, "ui_panel_dark", new Vector2(0.5f, 1f), new Vector2(x, -10), new Vector2(320, 112), new Vector2(0.5f, 1f)).rectTransform;
            var portrait = Panel(card, "ui_badge", new Vector2(host ? 0f : 1f, 0.5f), new Vector2(dir * 60, 0), new Vector2(88, 88), null, host ? HostColor : GuestColor).rectTransform;
            Panel(portrait, host ? "icon_chef_a" : "icon_chef_b", new Vector2(0.5f, 0.5f), new Vector2(0, -2), new Vector2(78, 78));
            var name = Text(card, host ? "Host" : "Guest", 22, host ? GameData.Hex("FFB8A0") : GameData.Hex("A8CCF0"), align, anchorSide, new Vector2(dir * 172, -32), new Vector2(120, 30), true);
            var score = Text(card, "0", 22, Cream, align, anchorSide, new Vector2(dir * 172, -74), new Vector2(120, 32), true);
            var stars = host ? hostStars : guestStars;
            for (int i = 0; i < 2; i++)
                stars[i] = Panel(card, "icon_star", new Vector2(host ? 1f : 0f, 1f), new Vector2(-dir * (34 + i * 36), -56), new Vector2(34, 34));
            if (host) { hostScore = score; hostName = name; } else { guestScore = score; guestName = name; }
        }

        void BuildRecruitBar()
        {
            recruitBar = Rect("Recruit", hudRoot, new Vector2(0.5f, 0f), new Vector2(-10, 14), new Vector2(1180, 206), new Vector2(0.5f, 0f));
            Image(recruitBar, "ui_panel_dark", Color.white);
            for (int s = 0; s < 5; s++)
            {
                var soup = GameData.Soups[s];
                float gx = 22 + s * 230 + 113;
                var chip = Panel(recruitBar, "ui_chip", new Vector2(0f, 1f), new Vector2(gx, -30), new Vector2(214, 40), null, soup.Identity).rectTransform;
                Panel(chip, "icon_soup_" + s, new Vector2(0f, 0.5f), new Vector2(22, 2), new Vector2(42, 42));
                bankTexts[s] = Text(chip, "", 20, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(122, 0), new Vector2(160, 34));
                bankChips[s] = chip;
                for (int o = 0; o < 2; o++)
                {
                    var info = GameData.Unit(GameData.UnitOption((SoupType)s, o));
                    var rt = Rect("Card", recruitBar, new Vector2(0f, 0f), new Vector2(gx - 54 + o * 108, 78), new Vector2(100, 128));
                    var frame = Image(rt, "ui_card", soup.Identity, true);
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = frame;
                    btn.transition = Selectable.Transition.None;
                    int idx = (int)info.Id;
                    btn.onClick.AddListener(() => Recruit(idx));
                    Panel(rt, "icon_" + info.Model, new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(80, 80));
                    var costText = Text(rt, info.Cost.ToString(), 22, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(90, 28), true);
                    Text(rt, ((idx + 1) % 10).ToString(), 16, Gold, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(16, -14), new Vector2(26, 22));
                    Text(rt, ShortName(info.Name), 13, Cream, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(96, 18));
                    var shade = Image(Stretch("Shade", rt), "ui_card", new Color(0.1f, 0.06f, 0.04f, 0.6f));
                    var tip = rt.gameObject.AddComponent<Tooltip>();
                    tip.Text = $"<b>{info.Name}</b>  <i>{info.English}</i>\n{info.Blurb}\nHP {info.Hp} • Speed {info.Speed} • Bite {info.Bite}";
                    rt.gameObject.AddComponent<JuicyButton>();
                    cards.Add(new RecruitCard { Id = info.Id, Button = btn, Rt = rt, Shade = shade, Cost = costText });
                }
            }
            queueText = Text(recruitBar, "", 18, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, 18), new Vector2(620, 28));
        }

        static string ShortName(string n)
        {
            int space = n.IndexOf(' ');
            return space > 0 ? n.Substring(0, space) : n;
        }

        void BuildBuildBar()
        {
            buildBar = Rect("Build", hudRoot, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(860, 200), new Vector2(0.5f, 0f));
            for (int i = 0; i < 2; i++)
            {
                var card = Rect("Option", buildBar, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -218 : 218, 0), new Vector2(420, 190));
                Image(card, "ui_panel_dark", Color.white);
                var key = Panel(card, "ui_badge", new Vector2(0f, 1f), new Vector2(26, -26), new Vector2(46, 46), null, Gold).rectTransform;
                Text(key, (i + 1).ToString(), 26, Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(40, 40), true, false);
                buildIcons[i] = Panel(card, "ui_badge", new Vector2(0f, 0.5f), new Vector2(100, -4), new Vector2(150, 150));
                buildTexts[i] = Text(card, "", 19, Cream, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(298, 0), new Vector2(230, 170));
                buildTexts[i].verticalOverflow = VerticalWrapMode.Overflow;
                buildTexts[i].resizeTextForBestFit = false;
                buildCards[i] = card;
            }
        }

        #endregion

        void BuildResult()
        {
            resultRoot = Stretch("Result", Root);
            Image(Stretch("Dim", resultRoot), "ui_vignette", new Color(1, 1, 1, 0.9f));
            var panel = Panel(resultRoot, "ui_panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 500)).rectTransform;
            resultTitle = Text(panel, "", 70, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -96), new Vector2(760, 110), true);
            resultBody = Text(panel, "", 26, Ink, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(700, 200), false, false);
            resultButton = Button(panel, "Back to Menu", new Vector2(0.5f, 0f), new Vector2(-150, 80), new Vector2(280, 80), OnResultButton, 28);
            resultButtonText = resultButton.GetComponentInChildren<Text>();
            Button(panel, "Main Menu", new Vector2(0.5f, 0f), new Vector2(150, 80), new Vector2(280, 80), OnLeave, 28);
        }

        void BuildHelp()
        {
            helpRoot = Stretch("Help", Root);
            Image(Stretch("Dim", helpRoot), "ui_vignette", new Color(1, 1, 1, 0.94f), true);
            var panel = Panel(helpRoot, "ui_panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120, 800)).rectTransform;
            Text(panel, "How to Play", 54, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(900, 80), true);
            var body = Text(panel,
                "<b>Soup is your money, and it spills.</b>\n\n" +
                "<b>WASD</b> move • <b>Shift</b> sprint (sloshes more) • <b>E</b> fill / pour / upgrade • <b>Q</b> elbow your rival • <b>Tab</b> this help\n\n" +
                "<b>Defender</b>: fill at your KITCHEN, carry soup to a glowing pad and press <b>1</b> or <b>2</b>. Golden ▲ pads are high ground: +25% range. Pour the same soup on a build to upgrade it.\n\n" +
                "<b>Attacker</b>: fill at the MARKET, pour it into the OCAK, then recruit raiders with the cards or keys <b>1–0</b>. They march when the raid starts.\n\n" +
                "<b>Terrain</b>: cliffs block you, ramps and stairs connect the terraces. Jumping off a ledge is a shortcut, but the landing sloshes soup. Rocks and roots jolt the pot, and mud slows you.\n\n" +
                "Best of 3 courses. Each course you attack once and defend once. The bigger raid wins.",
                23, Ink, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0, -410), new Vector2(1000, 580), false, false);
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.resizeTextForBestFit = false;
            Button(panel, "Got it", new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(260, 76), () => helpRoot.gameObject.SetActive(false), 28);
            helpRoot.gameObject.SetActive(false);
        }

        void ShowHelp()
        {
            helpRoot.gameObject.SetActive(true);
            helpRoot.GetChild(1).Pop(0.6f, 0.45f);
        }

        #region Actions

        void OnHost()
        {
            if (!CorbaBootstrap.Instance.Host(GameMode.Online)) ShowMenu("Could not open the kitchen (port in use?)");
        }

        void OnVsAi()
        {
            if (!CorbaBootstrap.Instance.Host(GameMode.VsAi)) ShowMenu("Could not start the match.");
        }

        void OnAiVsAi()
        {
            if (!CorbaBootstrap.Instance.Host(GameMode.AiVsAi)) ShowMenu("Could not start the match.");
        }

        void OnSolo()
        {
            if (!CorbaBootstrap.Instance.Host(GameMode.Solo)) ShowMenu("Could not start the match.");
        }

        void OnJoin()
        {
            if (!CorbaBootstrap.Instance.Join(ipField.text)) ShowMenu("Could not start client.");
            else menuStatus.text = "Knocking on the kitchen door...";
        }

        void OnLeave()
        {
            CorbaBootstrap.Instance.Leave();
            ShowMenu("");
        }

        void OnResultButton()
        {
            var d = MatchDirector.Instance;
            if (d != null && d.Phase.Value == MatchPhase.MatchOver && CorbaBootstrap.IsHost)
            {
                d.StartMatchRpc(d.PlayerCount.Value < 2);
                return;
            }
            OnLeave();
        }

        void Recruit(int unit) => MatchDirector.Instance?.RecruitRpc(unit);

        public void ShowMenu(string status)
        {
            if (menuStatus != null) menuStatus.text = status;
            CameraRig.Instance?.Follow(null);
        }

        public void Flash(string msg)
        {
            flashText.text = msg;
            flashUntil = Time.time + 1.6f;
            flashText.transform.Pop(1.6f, 0.4f);
        }

        public void Announce(string msg, Color color)
        {
            announcerText.text = msg;
            announcerText.color = color;
            var g = Group(announcer.gameObject);
            g.alpha = 1f;
            Tween.KillAll(announcer);
            announcer.localScale = Vector3.one * 2.6f;
            Tween.Scale(announcer, Vector3.one, 0.45f, Ease.OutBack);
            Tween.Fade(g, 0f, 0.5f).After(1.3f);
            CameraRig.Instance?.Shake(0.5f);
        }

        void OnToast(string msg, Color c)
        {
            var rt = Rect("Toast", toastRoot, new Vector2(0.5f, 1f), new Vector2(0, 30), new Vector2(760, 46));
            Image(rt, "ui_chip", new Color(0.24f, 0.15f, 0.09f, 0.85f));
            Text(rt, msg, 24, c, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(740, 40), true);
            var g = Group(rt.gameObject);
            g.alpha = 0f;
            Tween.Fade(g, 1f, 0.25f);
            toasts.Add((rt, g, Time.time + 3.2f));
        }

        #endregion

        void Update()
        {
            var director = MatchDirector.Instance;
            if (director != hookedDirector)
            {
                if (hookedDirector != null) hookedDirector.Toast -= OnToast;
                hookedDirector = director;
                if (director != null) director.Toast += OnToast;
            }

            bool online = CorbaBootstrap.Online;
            bool hasDirector = online && director != null && director.IsSpawned;
            var phase = hasDirector ? director.Phase.Value : MatchPhase.Lobby;

            bool showMenu = !hasDirector;
            if (showMenu != menuShown)
            {
                menuShown = showMenu;
                menuRoot.gameObject.SetActive(showMenu);
                if (showMenu) menuIntro?.Invoke();
            }
            if (online && !hasDirector) menuStatus.text = "Connecting...";

            lobbyRoot.gameObject.SetActive(hasDirector && phase == MatchPhase.Lobby);
            bool showHud = hasDirector && phase != MatchPhase.Lobby;
            if (showHud != hudShown)
            {
                hudShown = showHud;
                hudRoot.gameObject.SetActive(showHud);
                if (showHud) HudIntro();
            }
            bool showResult = hasDirector && phase == MatchPhase.MatchOver;
            if (showResult != resultShown)
            {
                resultShown = showResult;
                resultRoot.gameObject.SetActive(showResult);
                if (showResult)
                {
                    resultRoot.GetChild(1).Pop(0.3f, 0.6f);
                    resultTitle.transform.Pop(2.5f, 0.6f);
                }
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (helpRoot.gameObject.activeSelf) helpRoot.gameObject.SetActive(false); else ShowHelp();
            }

            UpdateToasts();
            if (!hasDirector) return;
            if (phase == MatchPhase.Lobby) UpdateLobby(director);
            else UpdateHud(director, phase);
            if (showResult) UpdateResult(director);
        }

        void HudIntro()
        {
            SlideIn(versus, new Vector2(0, 260), 0f);
            SlideIn(objectiveCard, new Vector2(-560, 0), 0.08f);
            SlideIn(sofraCard, new Vector2(500, 0), 0.12f);
            SlideIn(potCard, new Vector2(-460, 0), 0.16f);
            SlideIn(abilityCard, new Vector2(260, 0), 0.2f);
        }

        void UpdateToasts()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (rt, g, until) = toasts[i];
                if (rt == null) { toasts.RemoveAt(i); continue; }
                float left = until - Time.time;
                if (left <= 0f) { Destroy(rt.gameObject); toasts.RemoveAt(i); continue; }
                float targetY = -(toasts.Count - 1 - i) * 52f;
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, new Vector2(0, targetY), 1f - Mathf.Exp(-12f * Time.deltaTime));
                if (left < 0.4f) g.alpha = left / 0.4f;
            }
            float flashLeft = flashUntil - Time.time;
            if (flashText != null) flashText.gameObject.SetActive(flashLeft > 0f);
        }

        void UpdateLobby(MatchDirector d)
        {
            lobbyText.text = $"Cooks in the kitchen: {d.PlayerCount.Value} / 2";
            bool host = CorbaBootstrap.IsHost;
            lobbyHint.text = host
                ? (d.PlayerCount.Value < 2 ? "Waiting for a rival to join on port " + CorbaBootstrap.Port + "..." : "Your rival is here. Start when ready!")
                : "Waiting for the host to start...";
            startButton.gameObject.SetActive(host);
            soloButton.gameObject.SetActive(host);
            startButton.interactable = d.PlayerCount.Value >= 2;
        }

        void UpdateHud(MatchDirector d, MatchPhase phase)
        {
            var me = ChefController.Local;
            bool spectator = me == null;
            int mySlot = spectator ? 0 : me.Slot.Value;
            if (spectator)
            {
                var follow = ChefController.BySlot[d.AttackerSlot] ?? ChefController.BySlot[0];
                if (follow != null) CameraRig.Instance?.Follow(follow.transform);
            }
            bool defender = d.IsDefender(mySlot);

            int halfKey = d.Course.Value * 2 + d.Half.Value;
            if (phase != lastPhase || halfKey != lastHalfKey)
            {
                if (phase == MatchPhase.Prep)
                    Announce(spectator ? $"Course {d.Course.Value + 1}" : defender ? "DEFEND!" : "ATTACK!", defender ? DefendColor : AttackColor);
                else if (phase == MatchPhase.Raid) Announce("RAID!", GameData.Hex("FFB040"));
                else if (phase == MatchPhase.HalfResult) Announce("Half over", Cream);
                Tween.Punch(versus, 0.08f, 0.5f);
                if (defender != lastDefender || halfKey != lastHalfKey)
                {
                    Tween.Punch(objectiveCard, 0.12f, 0.5f);
                    lastDefender = defender;
                }
                lastPhase = phase;
                lastHalfKey = halfKey;
            }

            courseText.text = $"Course {d.Course.Value + 1}/3";
            phaseText.text = phase switch
            {
                MatchPhase.Prep => "Prep",
                MatchPhase.Raid => "Raid!",
                MatchPhase.HalfResult => "Half over",
                MatchPhase.MatchOver => "Final",
                _ => "",
            };
            phaseRibbon.color = phase == MatchPhase.Raid ? GameData.Hex("C9452E") : phase == MatchPhase.Prep ? GameData.Hex("D49A35") : GameData.Hex("8A7A62");
            float t = d.TimeLeft;
            int sec = Mathf.CeilToInt(t);
            timerText.text = phase == MatchPhase.MatchOver ? "-" : d.Paused.Value ? "II" : $"{sec / 60}:{sec % 60:00}";
            timerText.color = t < 10f && phase == MatchPhase.Raid ? GameData.Hex("C9302B") : Ink;
            if (sec != lastSecond)
            {
                if (t < 10f && (phase == MatchPhase.Raid || phase == MatchPhase.Prep)) Tween.Punch(timerText.transform, 0.25f, 0.35f);
                lastSecond = sec;
            }

            hostName.text = mySlot == 0 && !spectator ? "You" : d.IsBotSlot(0) ? "AI Cook" : "Host";
            guestName.text = mySlot == 1 && !spectator ? "You" : d.IsBotSlot(1) ? "AI Cook" : "Guest";
            hostScore.text = $"<size=15>RAID</size> {d.ScoreA.Value}";
            guestScore.text = $"{d.ScoreB.Value} <size=15>RAID</size>";
            for (int i = 0; i < 2; i++)
            {
                hostStars[i].color = i < d.WinsA.Value ? Color.white : new Color(0.3f, 0.22f, 0.16f, 0.8f);
                guestStars[i].color = i < d.WinsB.Value ? Color.white : new Color(0.3f, 0.22f, 0.16f, 0.8f);
            }
            if (d.WinsA.Value != lastWinsA) { hostStars[Mathf.Clamp(d.WinsA.Value - 1, 0, 1)].transform.Pop(0.1f, 0.7f); lastWinsA = d.WinsA.Value; }
            if (d.WinsB.Value != lastWinsB) { guestStars[Mathf.Clamp(d.WinsB.Value - 1, 0, 1)].transform.Pop(0.1f, 0.7f); lastWinsB = d.WinsB.Value; }

            float hunger = d.Hunger.Value / GameData.SofraHunger;
            if (hunger < shownHunger - 0.005f) Tween.Shake(sofraCard, 8f, 0.3f);
            shownHunger = Mathf.Lerp(shownHunger, hunger, 1f - Mathf.Exp(-6f * Time.deltaTime));
            if (hunger > shownHunger) shownHunger = hunger;
            sofraFill.fillAmount = shownHunger;
            sofraText.text = $"{Mathf.CeilToInt(d.Hunger.Value)} / {GameData.SofraHunger:0}";

            objectiveChip.color = spectator ? GameData.Hex("8A7A62") : defender ? DefendColor : AttackColor;
            objectiveRole.text = spectator ? "SPECTATING" : defender ? "DEFEND" : "ATTACK";
            UpdateObjective(d, me, defender, spectator);

            potCard.gameObject.SetActive(!spectator);
            abilityCard.gameObject.SetActive(!spectator);
            if (me != null) UpdatePot(d, me, mySlot);

            promptPill.gameObject.SetActive(me != null && !string.IsNullOrEmpty(me.Prompt));
            if (me != null && promptText.text != me.Prompt)
            {
                promptText.text = me.Prompt;
                if (!string.IsNullOrEmpty(me.Prompt)) promptPill.Pop(0.7f, 0.3f);
            }

            bool showBuild = !spectator && defender && d.InPlay && me.Soup.Value >= 0 && me.PromptPad >= 0 && d.PadBuild(me.PromptPad) == null;
            if (showBuild != buildBar.gameObject.activeSelf)
            {
                buildBar.gameObject.SetActive(showBuild);
                if (showBuild) for (int i = 0; i < 2; i++) buildCards[i].Pop(0.4f, 0.4f);
            }
            if (showBuild) UpdateBuildBar(me);

            bool attacking = !spectator && !defender && d.InPlay;
            if (attacking != recruitBar.gameObject.activeSelf)
            {
                recruitBar.gameObject.SetActive(attacking);
                if (attacking) SlideIn(recruitBar, new Vector2(0, -260), 0f, 0.5f);
            }
            if (attacking) UpdateRecruit(d, mySlot);

            UpdateEdgeArrow();
        }

        void UpdatePot(MatchDirector d, ChefController me, int mySlot)
        {
            float cap = d.Capacity(mySlot);
            float fill = me.DisplayFill;
            bool has = me.Soup.Value >= 0 && fill > 0.01f;
            if (fill > shownFill + 5f) Tween.Punch(potCard, 0.06f, 0.35f);
            shownFill = Mathf.Lerp(shownFill, has ? fill : 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
            potFill.fillAmount = shownFill / cap;
            potFill.color = has ? GameData.Soup((SoupType)me.Soup.Value).Liquid : Color.clear;
            potName.text = has ? GameData.Soup((SoupType)me.Soup.Value).Name : "Empty pot";
            potGlyph.gameObject.SetActive(has);
            potRole.text = has ? RoleLine(d, (SoupType)me.Soup.Value, d.IsDefender(mySlot)) : "Fill your pot at a cauldron. <b>[B]</b> Soup Book";
            if (has)
            {
                var info = GameData.Soup((SoupType)me.Soup.Value);
                potGlyph.sprite = Art.Sprite(info.Glyph);
                potGlyph.color = info.Identity;
                potName.color = Color.Lerp(info.Identity, Color.white, 0.35f);
            }
            else potName.color = Gold;
            potAmount.text = has ? $"{Mathf.RoundToInt(shownFill)}<size=22>/{cap:0}</size>" : "-";
            float spilled = mySlot == 0 ? d.SpillA.Value : d.SpillB.Value;
            spillText.text = $"Spilled: {Mathf.RoundToInt(spilled)}" + ((d.BonusMask.Value & (1 << mySlot)) != 0 ? "\n<color=#F2B33D>Annenin Eli +15%</color>" : "");
            Vector2 s = me.SloshVector;
            sloshDot.rectTransform.anchoredPosition = Vector2.ClampMagnitude(s, 1.4f) * 48f;
            sloshDot.color = s.magnitude > GameData.SloshRim ? GameData.Hex("D9442B") : s.magnitude > 0.7f ? GameData.Hex("F2B33D") : GameData.Hex("8FD19E");
            potFill.rectTransform.localRotation = Quaternion.Euler(0, 0, -s.x * 25f);
            shoveFill.fillAmount = me.ShoveCooldown01;
        }

        static string RoleLine(MatchDirector d, SoupType soup, bool defender)
        {
            var info = GameData.Soup(soup);
            string kw = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(info.Identity, Color.white, 0.3f))}><b>{info.Keyword}</b></color>";
            if (defender)
            {
                var a = GameData.Defence(GameData.DefenceOption(soup, 0));
                var b = GameData.Defence(GameData.DefenceOption(soup, 1));
                return $"{kw} \u2022 builds {a.English} or {b.English}";
            }
            if (d.Phase.Value == MatchPhase.Raid)
            {
                var l = Balance.Ladles[(int)soup];
                return $"{kw} \u2022 bank it, or throw [R]: {l.Name}, {l.Blurb.ToLowerInvariant()}";
            }
            var u = GameData.Unit(GameData.UnitOption(soup, 0));
            var v = GameData.Unit(GameData.UnitOption(soup, 1));
            return $"{kw} \u2022 bank it for {u.English} or {v.English}";
        }

        void UpdateBuildBar(ChefController me)
        {
            var soup = (SoupType)me.Soup.Value;
            for (int i = 0; i < 2; i++)
            {
                var info = GameData.Defence(GameData.DefenceOption(soup, i));
                var sprite = Art.Sprite("icon_" + info.Model);
                buildIcons[i].sprite = sprite != null ? sprite : Art.Sprite("ui_badge");
                int cost = MatchDirector.Instance.BuildCost(info.Id);
                bool afford = me.DisplayFill + 0.01f >= cost;
                string repeat = cost > info.Cost ? " <size=15>(repeat +)</size>" : "";
                buildTexts[i].text = $"<size=24><color=#F2B33D>{info.Name}</color></size>\n<i>{info.English}</i> • {info.Kind}\n{info.Blurb}\n<color={(afford ? "#B8F0B0" : "#FF9A80")}><b>Cost {cost}</b></color>{repeat}";
                buildCards[i].GetComponent<Image>().color = afford ? Color.white : new Color(0.7f, 0.6f, 0.6f);
            }
        }

        void UpdateRecruit(MatchDirector d, int mySlot)
        {
            for (int s = 0; s < 5; s++)
            {
                int bank = d.BankOf(mySlot, (SoupType)s);
                bankTexts[s].text = $"{GameData.Soups[s].Name}  <color=#FFE6A0>{bank}</color>";
                if (bank > lastBank[s]) Tween.Punch(bankChips[s], 0.25f, 0.45f);
                lastBank[s] = bank;
            }
            var kb = Keyboard.current;
            foreach (var card in cards)
            {
                var info = GameData.Unit(card.Id);
                int cost = d.RecruitCost(card.Id);
                bool afford = d.BankOf(mySlot, info.Soup) >= cost;
                card.Cost.text = cost.ToString();
                card.Cost.color = cost > info.Cost ? GameData.Hex("FFC28A") : Cream;
                card.Button.interactable = afford;
                card.Shade.gameObject.SetActive(!afford);
                if (afford && !card.WasAffordable) card.Rt.Pop(0.7f, 0.35f);
                card.WasAffordable = afford;
                int key = (int)card.Id;
                if (afford && kb != null && !TypingInField && DigitPressed(kb, (key + 1) % 10))
                {
                    Recruit(key);
                    Tween.Punch(card.Rt, 0.2f, 0.3f);
                }
            }
            string wave = d.Queued.Value > 0
                ? (d.Phase.Value == MatchPhase.Raid
                    ? $"Wave: {d.Queued.Value} ready  [G] send  • auto in {Mathf.Max(0, Mathf.CeilToInt((float)(d.WaveAutoAt.Value - MatchDirector.Now)))}s"
                    : $"Wave: {d.Queued.Value} ready • marches at raid start")
                : d.Marching.Value > 0 ? $"{d.Marching.Value} raiders marching" : "Recruit raiders, then send them as a wave";
            queueText.text = wave;
        }

        void UpdateObjective(MatchDirector d, ChefController me, bool defender, bool spectator)
        {
            ObjectiveTarget = null;
            var map = CorbaMap.Instance;
            string text;
            if (spectator) text = "Two AI cooks fight it out. Watch how they carry and counter.";
            else if (!d.InPlay) text = d.Phase.Value == MatchPhase.HalfResult ? "Swapping sides..." : "";
            else if (defender)
            {
                if (me.Soup.Value < 0)
                {
                    text = "Fill your pot at the <b>KITCHEN</b> (E).";
                    var st = Nearest(me, StationKind.DefenderCauldron);
                    ObjectiveTarget = st?.Position;
                }
                else
                {
                    text = "Carry it to a glowing <b>pad</b>, then press <b>1</b>/<b>2</b>. Golden ▲ = high ground.";
                    BuildPad best = null;
                    float bd = float.MaxValue;
                    foreach (var p in map.Pads)
                    {
                        if (d.PadBuild(p.Index) != null) continue;
                        float dist = CorbaMap.FlatDist(me.transform.position, p.TowerPosition);
                        if (dist < bd) { bd = dist; best = p; }
                    }
                    ObjectiveTarget = best?.TowerPosition;
                }
                if (d.Phase.Value == MatchPhase.Raid) text = "<color=#9C2F1C>Raid!</color> " + text;
            }
            else
            {
                int bank = 0;
                for (int s = 0; s < 5; s++) bank += d.BankOf(me.Slot.Value, (SoupType)s);
                if (me.Soup.Value < 0)
                {
                    text = "Fill your pot at the <b>MARKET</b> (E).";
                    ObjectiveTarget = Nearest(me, StationKind.AttackerCauldron)?.Position;
                }
                else
                {
                    text = "Pour it into the <b>OCAK</b> (E).";
                    ObjectiveTarget = map.Ocak.Position;
                }
                if (bank >= 15) text += " Recruit raiders: <b>1–0</b>.";
            }
            if (objectiveText.text != text)
            {
                objectiveText.text = text;
                Tween.Punch(objectiveCard, 0.05f, 0.3f);
            }
        }

        static Station Nearest(ChefController me, StationKind kind)
        {
            Station best = null;
            float bd = float.MaxValue;
            foreach (var s in CorbaMap.Instance.Stations)
            {
                if (s.Kind != kind) continue;
                float d = CorbaMap.FlatDist(me.transform.position, s.Position);
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        void UpdateEdgeArrow()
        {
            var cam = Camera.main;
            if (ObjectiveTarget == null || cam == null)
            {
                edgeArrow.gameObject.SetActive(false);
                return;
            }
            Vector3 world = ObjectiveTarget.Value + Vector3.up * 1.5f;
            Vector3 sp = cam.WorldToViewportPoint(world);
            bool onScreen = sp.z > 0 && sp.x > 0.05f && sp.x < 0.95f && sp.y > 0.08f && sp.y < 0.9f;
            edgeArrow.gameObject.SetActive(!onScreen);
            if (onScreen) return;
            Vector2 dir = new Vector2(sp.x - 0.5f, sp.y - 0.5f);
            if (sp.z < 0) dir = -dir;
            dir.Normalize();
            var size = Root.rect.size;
            float k = Mathf.Min((size.x * 0.5f - 90f) / Mathf.Max(Mathf.Abs(dir.x), 1e-3f), (size.y * 0.5f - 120f) / Mathf.Max(Mathf.Abs(dir.y), 1e-3f));
            edgeArrow.rectTransform.anchoredPosition = dir * k * (0.96f + Mathf.Sin(Time.time * 6f) * 0.03f);
            edgeArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }

        static bool DigitPressed(Keyboard kb, int digit)
        {
            return digit switch
            {
                0 => kb.digit0Key.wasPressedThisFrame,
                1 => kb.digit1Key.wasPressedThisFrame,
                2 => kb.digit2Key.wasPressedThisFrame,
                3 => kb.digit3Key.wasPressedThisFrame,
                4 => kb.digit4Key.wasPressedThisFrame,
                5 => kb.digit5Key.wasPressedThisFrame,
                6 => kb.digit6Key.wasPressedThisFrame,
                7 => kb.digit7Key.wasPressedThisFrame,
                8 => kb.digit8Key.wasPressedThisFrame,
                9 => kb.digit9Key.wasPressedThisFrame,
                _ => false,
            };
        }

        void UpdateResult(MatchDirector d)
        {
            var me = ChefController.Local;
            int mySlot = me != null ? me.Slot.Value : 0;
            bool won = d.Winner.Value == mySlot;
            resultTitle.text = me == null ? (d.Winner.Value == 0 ? "Host AI wins!" : "Guest AI wins!") : won ? "Afiyet Olsun!" : "Ellerine Sağlık...";
            resultTitle.color = won || me == null ? Gold : GameData.Hex("C9B79C");
            resultBody.text = (me == null ? "" : won ? "<b>You win the match!</b>" : "<b>Your rival wins the match.</b>") +
                              $"\n\nCourses: Host {d.WinsA.Value} – {d.WinsB.Value} Guest" +
                              $"\nLast course raid: Host {d.ScoreA.Value} – {d.ScoreB.Value} Guest" +
                              $"\nSoup spilled: Host {Mathf.RoundToInt(d.SpillA.Value)} – {Mathf.RoundToInt(d.SpillB.Value)} Guest";
            resultButtonText.text = CorbaBootstrap.IsHost ? "Rematch" : "Leave";
        }
    }

    public class Bobber : MonoBehaviour
    {
        public float Amount = 5f;
        float t;

        void Update()
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * Amount * 0.25f);
        }
    }

    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Text;
        static RectTransform box;
        static Text label;

        public void OnPointerEnter(PointerEventData e)
        {
            if (box == null)
            {
                var root = CorbaUI.Instance.Root;
                box = UIKit.Rect("Tooltip", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 120), new Vector2(0.5f, 0f));
                UIKit.Image(box, "ui_panel", Color.white);
                label = UIKit.Text(box, "", 19, UIKit.Ink, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 96), false, false);
            }
            label.text = Text;
            box.gameObject.SetActive(true);
            box.position = transform.position + new Vector3(0, ((RectTransform)transform).rect.height * 0.5f * transform.lossyScale.y + 10f, 0);
            box.SetAsLastSibling();
            box.Pop(0.7f, 0.25f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (box != null) box.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (box != null) box.gameObject.SetActive(false);
        }
    }
}
