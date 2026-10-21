using System.Collections;
using MindAttic.Components.Unity;
using MindAttic.Components.Unity.Core;
using UnityEngine;

namespace MindAttic.Components.Unity.Samples.Gallery
{
    /// <summary>
    /// Every MindAttic component, built from code. Press Play in Gallery.unity: the toolbar switches between the
    /// dark and light themes, Esc (or gamepad B/Start) opens the pause menu, and the menus walk through a
    /// multiplayer front end (host, join by IP or code, a lobby with a join code and roster), every control, and
    /// the dialogs and toasts.
    /// </summary>
    public sealed class GalleryDemo : MonoBehaviour
    {
        MaProgressBar _progress;
        MaButton _themeButton;

        /// <summary>The main menu stack (exposed for tooling and tests).</summary>
        public MenuStack Stack { get; private set; }

        /// <summary>The in-game pause menu.</summary>
        public MenuStack Pause { get; private set; }

        void Start() => Build();

        /// <summary>Builds the whole gallery on its own canvases.</summary>
        public void Build()
        {
            var canvas = MaCanvas.Create("Gallery Canvas");
            canvas.transform.SetParent(transform, false);

            // A HUD bar along the top.
            var bar = MaToolbar.Create(canvas);
            bar.AddIconButton(MaIcon.Menu, () => Stack.PopToRoot(), "Main menu");
            bar.AddText("MindAttic Components", TextStyle.Heading);
            bar.AddFlexibleSpace();
            _themeButton = bar.AddButton("Light theme", ToggleTheme, ButtonVariant.Ghost, MaIcon.Settings);
            bar.AddIconButton(MaIcon.Pause, () => Pause.Open(), "Pause menu (Esc)");

            Stack = MenuStack.Create(canvas);
            var main = Stack.CreateScreen("Gallery", "Every MindAttic component, built from code");
            main.AddButton("Multiplayer", () => Stack.Push(Multiplayer()), ButtonVariant.Primary, MaIcon.Play);
            main.AddButton("Buttons and tags", () => Stack.Push(Buttons()));
            main.AddButton("Inputs", () => Stack.Push(Inputs()));
            main.AddButton("Progress", () => Stack.Push(Progress()));
            main.AddButton("Dialogs and toasts", () => Stack.Push(Dialogs()));
            main.AddButton("HUD widgets", () => Stack.Push(Hud()));
            main.AddToggle("Reduce motion", MaTheme.ReduceMotion, v => MaTheme.ReduceMotion = v);
            main.AddButton("Quit", () => MaModal.Confirm("Quit the gallery?", "Nothing is saved; it's a gallery.",
                ok => { if (ok) Application.Quit(); }, "Quit", "Stay", ButtonVariant.Danger), ButtonVariant.Danger, MaIcon.Exit);
            Stack.Push(main);

            // The in-game pause menu: closed until Esc/B/Start.
            Pause = MenuStack.Create(MaCanvas.Create("Pause Canvas", 200), backdrop: true);
            var paused = Pause.CreateScreen("Paused");
            paused.AddButton("Resume", () => Pause.Close(), ButtonVariant.Primary, MaIcon.Play);
            paused.AddButton("Settings", () => Pause.Push(Settings(Pause)), ButtonVariant.Secondary, MaIcon.Settings);
            paused.AddButton("Leave game", () => MaModal.Confirm("Leave the game?", "You can rejoin with the same code.",
                ok => { if (ok) Pause.Close(); }, "Leave", "Stay", ButtonVariant.Danger), ButtonVariant.Danger, MaIcon.Exit);
            Pause.ConfigureAsPauseMenu(paused);
        }

        void ToggleTheme()
        {
            MaTheme.Toggle();
            _themeButton.Label = MaTheme.IsDark ? "Light theme" : "Dark theme";
        }

        // ---- Multiplayer front end (what Unity.Multiplayer's drop-in menus are built from) -------------------

        MenuScreen Multiplayer()
        {
            var s = Stack.CreateScreen("Multiplayer", "Host a game or join one");
            s.AddButton("Host", () => Stack.Push(Lobby("QX7P2K")), ButtonVariant.Primary, MaIcon.Play);
            s.AddSection("Join by IP");
            var ip = s.AddTextbox("", "127.0.0.1:7777");
            s.AddButton("Join by IP", () => Connect(ip.Text));
            s.AddSection("Join by code");
            var code = s.AddTextbox("", "ABC123").AsCode(6);
            s.AddButton("Join by code", () => Connect(code.Text));
            return s;
        }

        void Connect(string target)
        {
            var busy = MaModal.Busy("Connecting", "Contacting " + (string.IsNullOrEmpty(target) ? "the host" : target) + "...",
                () => MaToast.Show("Connection cancelled"));
            StartCoroutine(FailAfter(busy, 2.5f));
        }

        IEnumerator FailAfter(MaModal busy, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (busy == null || !busy.IsOpen) yield break;
            busy.Close();
            MaModal.Alert("Couldn't connect", "The host didn't answer. Check the address or code and try again.");
        }

        MenuScreen Lobby(string joinCode)
        {
            var s = Stack.CreateScreen("Lobby", "Share the code; start when everyone is ready");
            s.AddCode("Join code", joinCode);
            var roster = s.AddList(4, "Waiting for players");
            roster.AddRow("Ryan (you)", new Color(0.42f, 0.64f, 0.91f), "host").AddTag("Host", TagTone.Accent);
            var sam = roster.AddRow("Sam", new Color(0.90f, 0.45f, 0.30f), "41 ms");
            sam.AddTag("Ready", TagTone.Success);
            sam.AddAction(MaIcon.Close, () => roster.Remove(sam), "Kick Sam", ButtonVariant.Ghost);
            var lee = roster.AddRow("Lee", new Color(0.55f, 0.80f, 0.45f), "88 ms");
            lee.AddAction(MaIcon.Close, () => roster.Remove(lee), "Kick Lee", ButtonVariant.Ghost);
            s.AddToggle("Ready", true);
            var row = s.AddRow();
            row.AddButton("Leave", () => Stack.Back(), ButtonVariant.Danger);
            row.AddButton("Start", () => MaToast.Show("Starting the match", ToastKind.Success), ButtonVariant.Primary);
            s.BackHandler = () =>
            {
                MaModal.Confirm("Leave the lobby?", "The others will see you leave.", ok =>
                {
                    if (!ok) return;
                    s.BackHandler = null;
                    Stack.Back();
                }, "Leave", "Stay", ButtonVariant.Danger);
                return true;
            };
            return s;
        }

        // ---- HUD widgets (status pill, banner, minimap) ---------------------------------------------------

        MenuScreen Hud()
        {
            var s = Stack.CreateScreen("HUD widgets", "Status pills, a timed banner and a minimap");
            s.Width = 760;
            s.AddSection("Status pills");
            var pills = s.AddRow(equalWidths: false);
            pills.Alignment = TextAnchor.MiddleLeft;
            pills.Add(MaStatusPill.Create(null, "Live", StatusKind.Ok));
            pills.Add(MaStatusPill.Create(null, "Connecting...", StatusKind.Warn));
            pills.Add(MaStatusPill.Create(null, "Offline", StatusKind.Error));
            pills.Add(MaStatusPill.Create(null, "Spectating"));
            s.AddSection("Banner");
            int level = 1;
            s.AddButton("Show a level banner", () => Banner.Show("Level " + ++level, "Push every block onto its goal"));
            s.AddSection("Minimap (click it to pan)");
            var map = s.Add(MaMinimap.Create(null, 260, 130));
            var walls = new Color(0.67f, 0.67f, 0.67f, 0.8f);
            map.AddRect(new MapRect(1000, 1000, 600, 12), walls);
            map.AddRect(new MapRect(1000, 1288, 600, 12), walls);
            map.AddRect(new MapRect(1000, 1000, 12, 300), walls);
            map.AddRect(new MapRect(1588, 1000, 12, 300), walls);
            map.AddRect(MapRect.FromCenter(1450, 1150, 80, 80), new Color(0.11f, 0.62f, 0.46f, 0.8f));
            map.AddRect(MapRect.FromCenter(1200, 1150, 40, 40), new Color(0.90f, 0.45f, 0.30f));
            map.AddPoint(1300, 1220, new Color(0.42f, 0.64f, 0.91f), 2);
            map.SetViewport(new MapRect(1100, 1080, 300, 170));
            map.Apply();
            map.Panned += w => MaToast.Show("Pan to " + Mathf.RoundToInt(w.x) + ", " + Mathf.RoundToInt(w.y));
            return s;
        }

        MaBanner _banner;

        MaBanner Banner => _banner != null ? _banner : (_banner = MaBanner.Create(MaCanvas.Create("Banner Canvas", 50)));

        // ---- Every control -----------------------------------------------------------------------------

        MenuScreen Buttons()
        {
            var s = Stack.CreateScreen("Buttons and tags");
            s.Width = 760;
            s.AddSection("Variants");
            var variants = s.AddRow();
            variants.AddButton("Primary", null, ButtonVariant.Primary);
            variants.AddButton("Secondary", null);
            variants.AddButton("Danger", null, ButtonVariant.Danger);
            variants.AddButton("Ghost", null, ButtonVariant.Ghost);
            s.AddSection("With icons");
            var icons = s.AddRow(equalWidths: false);
            icons.AddButton("Play", null, ButtonVariant.Primary, MaIcon.Play);
            icons.AddButton("Copy", null, ButtonVariant.Secondary, MaIcon.Copy);
            icons.AddFlexibleSpace();
            icons.AddIconButton(MaIcon.Settings, null, "Settings");
            icons.AddIconButton(MaIcon.User, null, "Profile");
            icons.AddIconButton(MaIcon.Info, null, "About");
            icons.AddIconButton(MaIcon.Close, null, "Close", ButtonVariant.Ghost);
            var disabled = s.AddButton("Disabled", null);
            disabled.Interactable = false;
            s.AddSection("Tags");
            var tags = s.AddRow(equalWidths: false);
            tags.Alignment = TextAnchor.MiddleLeft;
            tags.Add(MaTag.Create(null, "Neutral"));
            tags.Add(MaTag.Create(null, "Host", TagTone.Accent));
            tags.Add(MaTag.Create(null, "Ready", TagTone.Success));
            tags.Add(MaTag.Create(null, "Lagging", TagTone.Warning));
            tags.Add(MaTag.Create(null, "Kicked", TagTone.Danger));
            return s;
        }

        MenuScreen Inputs()
        {
            var s = Stack.CreateScreen("Inputs");
            s.AddTextbox("Player name", "Your name");
            s.AddSelect("Region", new[] { "Europe", "Americas", "Asia", "Oceania" }, 0);
            s.AddRange("Volume", 0, 100, 80, null, wholeNumbers: true);
            s.AddRange("Mouse sensitivity", 0.1f, 2f, 1f, null, false, "0.0x");
            s.AddToggle("Subtitles", true);
            s.AddToggle("Show ping", false);
            return s;
        }

        MenuScreen Progress()
        {
            var s = Stack.CreateScreen("Progress", "Bars and spinners run on unscaled time");
            s.AddText("Downloading level", TextStyle.Label);
            _progress = s.AddProgress(0.35f);
            s.AddText("Waiting for the server", TextStyle.Label);
            s.AddProgress(0, indeterminate: true);
            s.AddSpinner("Connecting...");
            return s;
        }

        MenuScreen Dialogs()
        {
            var s = Stack.CreateScreen("Dialogs and toasts");
            s.AddButton("Alert", () => MaModal.Alert("Disconnected", "The host closed the game."));
            s.AddButton("Confirm", () => MaModal.Confirm("Host a game?", "Others can join with your code.",
                ok => MaToast.Show(ok ? "Hosting" : "Cancelled", ok ? ToastKind.Success : ToastKind.Info), "Host"));
            s.AddButton("Busy with cancel", () => Connect("QX7P2K"));
            var toasts = s.AddRow();
            toasts.AddButton("Info", () => MaToast.Show("Sam joined the lobby"));
            toasts.AddButton("Success", () => MaToast.Show("Settings saved", ToastKind.Success));
            toasts.AddButton("Warning", () => MaToast.Show("High ping to the host", ToastKind.Warning, "Connection"));
            toasts.AddButton("Error", () => MaToast.Show("Lost connection", ToastKind.Error));
            s.AddButton("Hover or focus me", null, ButtonVariant.Ghost, MaIcon.Info).WithTooltip("Tooltips show on hover and on keyboard/gamepad focus.");
            return s;
        }

        MenuScreen Settings(MenuStack stack)
        {
            var s = stack.CreateScreen("Settings");
            s.AddRange("Master volume", 0, 100, 80, null, true);
            s.AddToggle("Subtitles", true);
            s.AddToggle("Reduce motion", MaTheme.ReduceMotion, v => MaTheme.ReduceMotion = v);
            return s;
        }

        void Update()
        {
            if (_progress != null) _progress.Value = Mathf.PingPong(Time.unscaledTime * 0.2f, 1f);
        }
    }
}
