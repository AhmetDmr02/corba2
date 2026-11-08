using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Corba
{
    public enum Language { English = 0, Turkish = 1 }

    /// <summary>
    /// String table for every player-facing line. English data names and blurbs stay in <see cref="Balance"/> and
    /// <see cref="GameData"/>; only their Turkish counterparts live here.
    /// </summary>
    public static class Loc
    {
        const string PrefKey = "Corba.Language";
        static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        public static event Action Changed;

        static Language current;
        static bool loaded;

        public static Language Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = (Language)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 1);
                }
                return current;
            }
        }

        public static bool Turkish => Current == Language.Turkish;

        public static void Set(Language lang)
        {
            if (lang == Current) return;
            current = lang;
            PlayerPrefs.SetInt(PrefKey, (int)lang);
            Changed?.Invoke();
        }

        public static void Cycle() => Set(Current == Language.English ? Language.Turkish : Language.English);

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out var e)) return key;
            return Turkish ? e.tr : e.en;
        }

        public static string F(string key, params object[] args) => string.Format(T(key), args);

        public static string Upper(string s) => Turkish ? s.ToUpper(Tr) : s.ToUpperInvariant();

        public static string SoupName(SoupType s) => Turkish ? GameData.Soup(s).Name : GameData.Soup(s).English;
        public static string SoupKeyword(SoupType s) => Turkish ? SoupKeywordTr[(int)s] : GameData.Soup(s).Keyword;
        public static string SoupCarry(SoupType s) => Turkish ? SoupCarryTr[(int)s] : GameData.Soup(s).Carry;
        public static string SoupTheme(SoupType s) => Turkish ? SoupThemeTr[(int)s] : GameData.Soup(s).Theme;

        public static string DefenceName(DefenceId id) => Turkish ? GameData.Defence(id).Name : GameData.Defence(id).English;
        public static string DefenceBlurb(DefenceId id) => Turkish ? DefenceBlurbTr[(int)id] : GameData.Defence(id).Blurb;
        public static string DefenceKindName(DefenceKind k) => T("kind." + k);

        public static string UnitName(UnitId id) => Turkish ? GameData.Unit(id).Name : GameData.Unit(id).English;
        public static string UnitShort(UnitId id) => Turkish ? UnitShortTr[(int)id] : UnitShortEn[(int)id];
        public static string UnitBlurb(UnitId id) => Turkish ? UnitBlurbTr[(int)id] : GameData.Unit(id).Blurb;

        public static string LadleName(int soup) => Turkish ? LadleNameTr[soup] : Balance.Ladles[soup].Name;
        public static string LadleBlurb(int soup) => Turkish ? LadleBlurbTr[soup] : Balance.Ladles[soup].Blurb;

        static readonly string[] SoupKeywordTr = { "Sağlam", "Acı", "Serin", "Ekşi", "Keskin" };
        static readonly string[] SoupCarryTr =
        {
            "Koyu: zor dökülür, biraz yavaştır.",
            "Normal çorba, sürprizi yok.",
            "Sulu: kolay dökülür ama daha hızlı yürürsün.",
            "Koyu ve affedici.",
            "Kokar: yakındaki rakip aşçılar yavaşlar.",
        };
        static readonly string[] SoupThemeTr =
        {
            "Sağlam duvarlar ve zırhlı akıncılar.",
            "Yakıcı hasar ve hızlı akıncılar.",
            "Savunmada yavaşlatır, saldırıda iyileştirir.",
            "Düşmanı zayıflatır: ekşi yığınları, zehir, kule yavaşlatma.",
            "Bozgun: korku, kök, sersemletme, yıkım.",
        };
        static readonly string[] DefenceBlurbTr =
        {
            "Mercimek sıçratır. Kalabalığa karşı iyi.",
            "Yolu kesen duvar.",
            "Hedefleri tutuşturan seri atışlar.",
            "Üstünde duran her şeyi kavurur.",
            "Alanı yavaşlatan serin dalga.",
            "Yapışkan gölcük, ağır yavaşlatma.",
            "Ekşi yığınları hedefe daha çok hasar aldırır.",
            "Zehir bulutu olarak patlar. Yeniden kurulur.",
            "Birlikleri geri kaçıran koku dalgası.",
            "3 birliğe kadar köklendirir. Yeniden kurulur.",
        };
        static readonly string[] UnitShortEn = { "Grunt", "Shield", "Runner", "Ember", "Healer", "Golem", "Saboteur", "Cloud", "Brute", "Bomber", "Mini Golem" };
        static readonly string[] UnitShortTr = { "Er", "Kalkanlı", "Koşucu", "Közcü", "Şifacı", "Golem", "Sabotajcı", "Bulut", "Dev", "Bombacı", "Minik Golem" };
        static readonly string[] UnitBlurbTr =
        {
            "Ucuz ve kalabalık.",
            "Zırhlı. Kuleleri üstüne çeker.",
            "Hızlı. Vurulunca depar atar.",
            "Kulelere köz fırlatır.",
            "Yakındaki dostlarını iyileştirir.",
            "Yavaşlamaz. Ölünce bölünür.",
            "Tuzakları etkisiz kılar.",
            "Yakındaki kuleleri yavaşlatır.",
            "Dev ve korkusuz. Kuleleri ezer.",
            "Bir kuleyi havaya uçurur.",
            "",
        };
        static readonly string[] LadleNameTr = { "Doyurucu", "Acılı", "Yatıştırıcı", "Ekşi sis", "Koku" };
        static readonly string[] LadleBlurbTr =
        {
            "Akıncılar çok daha az hasar alır.",
            "Akıncılar kulelerin önünden koşarak geçer.",
            "Akıncıları iyileştirir ve arındırır.",
            "Sisteki kuleler çok daha yavaş ateş eder.",
            "Kuleleri sersemletir, tuzakları bozar; akıncılar korku ve köke aldırmaz.",
        };

        static readonly Dictionary<string, (string en, string tr)> Table = new Dictionary<string, (string, string)>
        {
            ["lang.button"] = ("Language: English", "Dil: Türkçe"),

            ["menu.subtitle"] = ("Ladle Wars", "Kepçe Savaşları"),
            ["menu.tagline"] = ("Carry soup. Spill less. Feed your army.", "Çorbayı taşı. Az dök. Ordunu doyur."),
            ["menu.playAi"] = ("Play vs AI", "Yapay Zekâya Karşı"),
            ["menu.host"] = ("Create Room", "Oda Kur"),
            ["menu.join"] = ("Join", "Katıl"),
            ["menu.practice"] = ("Practice", "Antrenman"),
            ["menu.watch"] = ("Watch AI vs AI", "YZ'leri İzle"),
            ["menu.help"] = ("How to Play", "Nasıl Oynanır"),
            ["menu.quit"] = ("Quit", "Çıkış"),
            ["menu.netInfo"] = ("Online rooms via Unity Relay • LAN: type an IP", "Çevrim içi odalar Unity Relay ile • LAN: IP yaz"),
            ["menu.codePlaceholder"] = ("Room code", "Oda kodu"),
            ["menu.creatingRoom"] = ("Opening a room...", "Oda açılıyor..."),
            ["menu.findingRoom"] = ("Looking for the room...", "Oda aranıyor..."),
            ["menu.errHost"] = ("Could not open the kitchen (port in use?)", "Mutfak açılamadı (port kullanımda mı?)"),
            ["menu.errStart"] = ("Could not start the match.", "Maç başlatılamadı."),
            ["menu.errClient"] = ("Could not start client.", "İstemci başlatılamadı."),
            ["menu.knocking"] = ("Knocking on the kitchen door...", "Mutfak kapısı çalınıyor..."),
            ["menu.connecting"] = ("Connecting...", "Bağlanıyor..."),
            ["menu.disconnected"] = ("Disconnected.", "Bağlantı koptu."),
            ["net.full"] = ("The kitchen is full.", "Mutfak dolu."),
            ["net.enterCode"] = ("Type a room code first.", "Önce bir oda kodu yaz."),
            ["net.roomNotFound"] = ("No room with that code (or it is full).", "Bu kodla bir oda yok ya da oda dolu."),
            ["net.relayFailed"] = ("Could not open an online room. Try again.", "Çevrim içi oda açılamadı. Tekrar dene."),
            ["net.servicesFailed"] = ("Could not reach Unity services. Is the project linked?", "Unity servislerine bağlanılamadı. Proje bağlı mı?"),
            ["net.offline"] = ("No connection to the online services.", "Çevrim içi servislere bağlantı yok."),

            ["lobby.title"] = ("The Kitchen", "Mutfak"),
            ["lobby.start"] = ("Start Match (Bo3)", "Maçı Başlat (3'te 2)"),
            ["lobby.solo"] = ("Start Solo", "Tek Başına"),
            ["lobby.leave"] = ("Leave", "Ayrıl"),
            ["lobby.cooks"] = ("Cooks in the kitchen: {0} / 2", "Mutfaktaki aşçılar: {0} / 2"),
            ["lobby.waitRival"] = ("Waiting for a rival to join on port {0}...", "Rakibin {0} portundan katılması bekleniyor..."),
            ["lobby.rivalHere"] = ("Your rival is here. Start when ready!", "Rakibin geldi. Hazır olunca başla!"),
            ["lobby.waitHost"] = ("Waiting for the host to start...", "Kurucunun başlatması bekleniyor..."),
            ["lobby.roomCode"] = ("ROOM CODE", "ODA KODU"),
            ["lobby.copy"] = ("Copy", "Kopyala"),
            ["lobby.copied"] = ("Code copied. Send it to your rival!", "Kod kopyalandı. Rakibine gönder!"),
            ["lobby.shareCode"] = ("Send the room code to your rival.", "Oda kodunu rakibine gönder."),

            ["hud.sofra"] = ("Feast hunger", "Sofra açlığı"),
            ["hud.soupBook"] = ("Soup Book [B]", "Çorba Kitabı [B]"),
            ["hud.elbow"] = ("Elbow\n<size=14>shove rival</size>", "Dirsek\n<size=14>rakibi it</size>"),
            ["hud.course"] = ("Course {0}/3", "Tur {0}/3"),
            ["hud.raid"] = ("RAID", "BASKIN"),
            ["hud.spilling"] = ("SPILLING!", "DÖKÜLÜYOR!"),
            ["phase.prep"] = ("Prep", "Hazırlık"),
            ["phase.raid"] = ("Raid!", "Baskın!"),
            ["phase.half"] = ("Half over", "Devre bitti"),
            ["phase.final"] = ("Final", "Bitti"),
            ["announce.course"] = ("Course {0}", "Tur {0}"),
            ["announce.defend"] = ("DEFEND!", "SAVUN!"),
            ["announce.attack"] = ("ATTACK!", "SALDIR!"),
            ["announce.raid"] = ("RAID!", "BASKIN!"),
            ["announce.half"] = ("Half over", "Devre bitti"),
            ["announce.wave"] = ("WAVE ×{0}!", "DALGA ×{0}!"),
            ["name.you"] = ("You", "Sen"),
            ["name.ai"] = ("AI Cook", "YZ Aşçı"),
            ["name.host"] = ("Host", "Kurucu"),
            ["name.guest"] = ("Guest", "Misafir"),
            ["role.spectating"] = ("SPECTATING", "İZLİYOR"),
            ["role.defend"] = ("DEFEND", "SAVUN"),
            ["role.attack"] = ("ATTACK", "SALDIR"),

            ["pot.empty"] = ("Empty pot", "Boş tencere"),
            ["pot.hint"] = ("Fill your pot at a cauldron. <b>[B]</b> Soup Book", "Tencereni bir kazandan doldur. <b>[B]</b> Çorba Kitabı"),
            ["pot.spilled"] = ("Spilled: {0}", "Dökülen: {0}"),
            ["pot.bonus"] = ("Mom's Touch +15%", "Annenin Eli +15%"),
            ["role.builds"] = ("{0} • builds {1} or {2}", "{0} • kurar: {1} ya da {2}"),
            ["role.ladle"] = ("{0} • bank it, or throw [R]: {1}, {2}", "{0} • ocağa dök ya da fırlat [R]: {1}, {2}"),
            ["role.recruit"] = ("{0} • bank it for {1} or {2}", "{0} • ocağa dök: {1} ya da {2}"),
            ["build.repeat"] = ("(repeat +)", "(tekrar +)"),
            ["build.cost"] = ("Cost {0}", "Bedel {0}"),
            ["recruit.stats"] = ("HP {0} • Speed {1} • Bite {2}", "Can {0} • Hız {1} • Isırık {2}"),
            ["wave.raidReady"] = ("Wave: {0} ready  [G] send  • auto in {1}s", "Dalga: {0} hazır  [G] gönder  • {1} sn sonra kendiliğinden"),
            ["wave.prepReady"] = ("Wave: {0} ready • marches at raid start", "Dalga: {0} hazır • baskın başlayınca yürür"),
            ["wave.marching"] = ("{0} raiders marching", "{0} akıncı yürüyor"),
            ["wave.hint"] = ("Recruit raiders, then send them as a wave", "Akıncı topla, sonra dalga hâlinde gönder"),

            ["obj.spectate"] = ("Two AI cooks fight it out. Watch how they carry and counter.", "İki YZ aşçı kapışıyor. Nasıl taşıyıp karşılık verdiklerini izle."),
            ["obj.swapping"] = ("Swapping sides...", "Taraflar değişiyor..."),
            ["obj.fillKitchen"] = ("Fill your pot at the <b>KITCHEN</b> (E).", "Tencereni <b>MUTFAK</b> kazanlarında doldur (E)."),
            ["obj.carryPad"] = ("Carry it to a glowing <b>pad</b>, then press <b>1</b>/<b>2</b>. Golden ▲ = high ground.", "Parlayan bir <b>kaideye</b> taşı, sonra <b>1</b>/<b>2</b> tuşuna bas. Altın ▲ = yüksek zemin."),
            ["obj.raid"] = ("<color=#9C2F1C>Raid!</color> ", "<color=#9C2F1C>Baskın!</color> "),
            ["obj.fillMarket"] = ("Fill your pot at the <b>MARKET</b> (E).", "Tencereni <b>PAZAR</b> kazanlarında doldur (E)."),
            ["obj.pour"] = ("Pour it into the <b>HEARTH</b> (E).", "Çorbayı <b>OCAK</b> içine dök (E)."),
            ["obj.recruit"] = (" Recruit raiders: <b>1–0</b>.", " Akıncı topla: <b>1–0</b>."),

            ["result.hostAi"] = ("Host AI wins!", "Kurucu YZ kazandı!"),
            ["result.guestAi"] = ("Guest AI wins!", "Misafir YZ kazandı!"),
            ["result.win"] = ("Bon Appétit!", "Afiyet Olsun!"),
            ["result.lose"] = ("Well cooked, still...", "Ellerine Sağlık..."),
            ["result.youWin"] = ("<b>You win the match!</b>", "<b>Maçı kazandın!</b>"),
            ["result.rivalWins"] = ("<b>Your rival wins the match.</b>", "<b>Maçı rakibin kazandı.</b>"),
            ["result.courses"] = ("Courses: Host {0} – {1} Guest", "Turlar: Kurucu {0} – {1} Misafir"),
            ["result.lastRaid"] = ("Last course raid: Host {0} – {1} Guest", "Son tur baskını: Kurucu {0} – {1} Misafir"),
            ["result.spilled"] = ("Soup spilled: Host {0} – {1} Guest", "Dökülen çorba: Kurucu {0} – {1} Misafir"),
            ["result.rematch"] = ("Rematch", "Rövanş"),
            ["result.leave"] = ("Leave", "Ayrıl"),
            ["result.mainMenu"] = ("Main Menu", "Ana Menü"),

            ["help.title"] = ("How to Play", "Nasıl Oynanır"),
            ["help.ok"] = ("Got it", "Anladım"),
            ["help.body"] = (
                "<b>Soup is your money, and it spills.</b>\n\n" +
                "<b>WASD</b> move • <b>Shift</b> sprint (sloshes more) • <b>E</b> fill / pour / upgrade • <b>Q</b> elbow your rival • <b>Tab</b> this help\n\n" +
                "<b>Defender</b>: fill at your KITCHEN, carry soup to a glowing pad and press <b>1</b> or <b>2</b>. Golden ▲ pads are high ground: +25% range. Pour the same soup on a build to upgrade it.\n\n" +
                "<b>Attacker</b>: fill at the MARKET, pour it into the HEARTH, then recruit raiders with the cards or keys <b>1–0</b>. They march when the raid starts.\n\n" +
                "<b>Terrain</b>: cliffs block you, ramps and stairs connect the terraces. Jumping off a ledge is a shortcut, but the landing sloshes soup. Rocks and roots jolt the pot, and mud slows you.\n\n" +
                "Best of 3 courses. Each course you attack once and defend once. The bigger raid wins.",
                "<b>Çorba senin paran ve dökülüyor.</b>\n\n" +
                "<b>WASD</b> yürü • <b>Shift</b> koş (daha çok çalkalar) • <b>E</b> doldur / dök / yükselt • <b>Q</b> rakibine dirsek at • <b>Tab</b> bu yardım\n\n" +
                "<b>Savunan</b>: MUTFAK kazanlarında doldur, çorbayı parlayan bir kaideye taşı ve <b>1</b> ya da <b>2</b> tuşuna bas. Altın ▲ kaideler yüksek zemindir: +%25 menzil. Bir yapıya aynı çorbayı dökerek onu yükselt.\n\n" +
                "<b>Saldıran</b>: PAZAR kazanlarında doldur, OCAK içine dök, sonra kartlarla ya da <b>1–0</b> tuşlarıyla akıncı topla. Baskın başlayınca yürürler.\n\n" +
                "<b>Arazi</b>: Uçurumlar yolu keser, rampalar ve merdivenler terasları bağlar. Kenardan atlamak kestirmedir ama iniş çorbayı çalkalar. Taşlar ve kökler tencereyi sarsar, çamur yavaşlatır.\n\n" +
                "Üç turun en iyisi. Her turda bir kez saldırır, bir kez savunursun. Büyük baskını yapan kazanır."),

            ["prompt.topUp"] = ("[E] Top up {0}", "[E] {0} tazele"),
            ["prompt.swap"] = ("[E] Swap to {0} (dumps your pot!)", "[E] {0} ile değiştir (tencere boşalır!)"),
            ["prompt.fill"] = ("[E] Fill pot with {0}", "[E] Tencereyi doldur: {0}"),
            ["prompt.pour"] = ("[E] Pour {0} ladles into the Hearth", "[E] Ocağa {0} kepçe dök"),
            ["prompt.ladle"] = ("[R] / Right-click: toss {0} ladle ({1}) • {2}", "[R] / Sağ tık: {0} kepçe fırlat ({1}) • {2}"),
            ["prompt.needs"] = ("Needs {0} to upgrade", "Yükseltmek için gereken: {0}"),
            ["prompt.max"] = ("{0} is max level", "{0} en üst seviyede"),
            ["prompt.upgrade"] = ("[E] Upgrade {0} to Lv{1} ({2})", "[E] {0} yükselt → Sv{1} ({2})"),
            ["prompt.build"] = ("[1] {0} ({1})     [2] {2} ({3})", "[1] {0} ({1})     [2] {2} ({3})"),
            ["flash.noSoup"] = ("Not enough soup in the pot!", "Tencerede yeterli çorba yok!"),
            ["flash.elbowed"] = ("Elbowed! You got shoved!", "Dirsek yedin!"),
            ["flash.tutorialDone"] = ("Nice! The timer is running now.", "Harika! Süre şimdi işliyor."),

            ["toast.hostJoined"] = ("Host cook joined", "Kurucu aşçı katıldı"),
            ["toast.rivalJoined"] = ("Rival cook joined", "Rakip aşçı katıldı"),
            ["toast.rivalLeft"] = ("Your rival left the kitchen", "Rakibin mutfaktan ayrıldı"),
            ["toast.half"] = ("Course {0} • Half {1}", "Tur {0} • Devre {1}"),
            ["toast.raid"] = ("Raid! Units are marching", "Baskın! Birlikler yürüyor"),
            ["toast.sofraEmpty"] = ("The feast is gone! +{0} speed bonus", "Sofra boşaldı! +{0} hız bonusu"),
            ["toast.courseHost"] = ("Host cook wins course {0}!", "Kurucu aşçı {0}. turu kazandı!"),
            ["toast.courseGuest"] = ("Guest cook wins course {0}!", "Misafir aşçı {0}. turu kazandı!"),

            ["fx.poof"] = ("poof!", "puf!"),
            ["fx.nom"] = ("NOM!", "HAM!"),

            ["label.ocak"] = ("HEARTH", "OCAK"),
            ["label.ocakSub"] = ("Attacker camp", "Saldıran kampı"),
            ["label.sofra"] = ("FEAST", "SOFRA"),
            ["label.sofraSub"] = ("Defend the table", "Sofrayı savun"),
            ["label.kitchen"] = ("KITCHEN", "MUTFAK"),
            ["label.kitchenSub"] = ("Defender soups", "Savunma çorbaları"),
            ["label.market"] = ("MARKET", "PAZAR"),
            ["label.marketSub"] = ("Attacker soups", "Saldırı çorbaları"),

            ["kind.Tower"] = ("Tower", "Kule"),
            ["kind.Trap"] = ("Trap", "Tuzak"),
            ["kind.Blocker"] = ("Blocker", "Engel"),

            ["book.title"] = ("Soup Book", "Çorba Kitabı"),
            ["book.sub"] = ("Every soup is a role. The soup in your pot decides what you can build, recruit and throw.",
                "Her çorba bir roldür. Tenceredeki çorba neyi kurabileceğini, toplayabileceğini ve fırlatabileceğini belirler."),
            ["book.carrying"] = ("Carrying: {0}", "Taşırken: {0}"),
            ["book.defend"] = ("DEFEND", "SAVUN"),
            ["book.attack"] = ("ATTACK", "SALDIR"),
            ["book.ladle"] = ("Ladle: {0}", "Kepçe: {0}"),
            ["book.close"] = ("Close  [B]", "Kapat  [B]"),

            ["tut.title"] = ("Tutorial {0} / {1}", "Eğitim {0} / {1}"),
            ["tut.skip"] = ("Skip", "Geç"),
            ["tut.d1"] = ("Welcome, cook! You <b>defend</b> the Feast first.\nMove with <b>WASD</b>.", "Hoş geldin aşçı! Önce <b>sofrayı savunuyorsun</b>.\n<b>WASD</b> ile yürü."),
            ["tut.d2"] = ("Soups boil in big cauldrons at your <b>KITCHEN</b>. Follow the arrow and press <b>E</b> at one.", "Çorbalar <b>MUTFAK</b> kazanlarında kaynıyor. Oku takip et ve birinin başında <b>E</b> tuşuna bas."),
            ["tut.d3"] = ("Easy now! Sudden starts, turns, sprinting and cliff drops <b>spill</b> soup. Watch the bowl in the bottom-left.", "Yavaş ol! Ani kalkışlar, dönüşler, koşmak ve uçurumdan atlamak çorbayı <b>döker</b>. Sol alttaki kâseye göz kulak ol."),
            ["tut.d4"] = ("Carry the soup to a glowing <b>pad</b> by the road and press <b>1</b> or <b>2</b>. Golden ▲ pads on high ground get +25% range.", "Çorbayı yol kenarındaki parlayan bir <b>kaideye</b> taşı ve <b>1</b> ya da <b>2</b> tuşuna bas. Yüksek zemindeki altın ▲ kaideler +%25 menzil verir."),
            ["tut.d5"] = ("Each soup builds different towers. Mix them up, then let the raid begin!", "Her çorba farklı kuleler kurar. Karıştır, sonra baskın başlasın!"),
            ["tut.a1"] = ("Now you <b>attack</b>! Fill your pot at the <b>MARKET</b> cauldrons.", "Şimdi sen <b>saldırıyorsun</b>! Tencereni <b>PAZAR</b> kazanlarından doldur."),
            ["tut.a2"] = ("Carry it carefully to the <b>HEARTH</b> and pour it in with <b>E</b>.", "Dikkatlice <b>OCAK</b> başına taşı ve <b>E</b> ile içine dök."),
            ["tut.a3"] = ("Banked soup buys raiders. Click the cards or press <b>1–0</b>. They march when the raid starts.", "Ocaktaki çorba akıncı satın alır. Kartlara tıkla ya da <b>1–0</b> tuşlarına bas. Baskın başlayınca yürürler."),
            ["tut.a4"] = ("Tip: press <b>Q</b> next to your rival to elbow them and make them spill!", "İpucu: Rakibinin yanında <b>Q</b> tuşuna basıp dirsek at, çorbasını döksün!"),
        };
    }

    /// <summary>Re-evaluates a text source whenever the language changes.</summary>
    public class LocText : MonoBehaviour
    {
        Func<string> source;
        Text text;

        public static Text Bind(Text t, Func<string> source)
        {
            var l = t.GetComponent<LocText>();
            if (l == null) l = t.gameObject.AddComponent<LocText>();
            l.text = t;
            l.source = source;
            t.text = source();
            return t;
        }

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        void Refresh()
        {
            if (text != null && source != null) text.text = source();
        }
    }
}
