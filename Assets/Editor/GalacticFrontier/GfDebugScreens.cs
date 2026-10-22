#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Dialogue;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.DebugTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GalacticFrontier.DebugTools
{
    /// <summary>
    /// Opens real AppShell / BattleScene UI and captures the Game view, including Screen Space Overlay.
    /// Screenshots go to Temp/gf-ui-shots. Mutating screens refuse to run against the official save.
    /// </summary>
    public static partial class GfDebugSession
    {
        public const string ShotFolderName = "gf-ui-shots";

        const int SameSceneSteps = 90;
        const int SceneChangeSteps = 600;

        enum ScreenKind
        {
            Nexus,
            RecruitResult,
            Battle,
            BattleReport
        }

        sealed class ScreenDef
        {
            public string Id;
            public AppScreen Nav;
            public string Marker;
            public ScreenKind Kind;
            public string Note;
        }

        static readonly ScreenDef[] Screens =
        {
            new ScreenDef { Id = "bridge", Nav = AppScreen.Bridge, Marker = "Bridge Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "explore", Nav = AppScreen.Battle, Marker = "Explore Screen", Kind = ScreenKind.Nexus,
                Note = "AppScreen.Battle is the star map / explore page. The fight itself is screen_id battle." },
            new ScreenDef { Id = "formation", Nav = AppScreen.Formation, Marker = "Formation Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "missions", Nav = AppScreen.Missions, Marker = "Missions Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "ship", Nav = AppScreen.Ship, Marker = "Ship Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "recruit", Nav = AppScreen.Recruit, Marker = "Recruit Screen", Kind = ScreenKind.Nexus,
                Note = "Live recruit page. Pull results are inline text, not a separate popup." },
            new ScreenDef { Id = "recruit_result", Nav = AppScreen.Recruit, Marker = "Status", Kind = ScreenKind.RecruitResult,
                Note = "Presses the existing PullOne button. There is no separate gacha-result popup; CardResultManager is not on this page." },
            new ScreenDef { Id = "characters", Nav = AppScreen.Characters, Marker = "Characters Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "cards", Nav = AppScreen.Cards, Marker = "Cards Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "inventory", Nav = AppScreen.Inventory, Marker = "Inventory Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "crafting", Nav = AppScreen.Crafting, Marker = "Crafting Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "market", Nav = AppScreen.Market, Marker = "Market Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "settings", Nav = AppScreen.Settings, Marker = "Settings Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "production_lines", Nav = AppScreen.ProductionLines, Marker = "Production Lines Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "debug", Nav = AppScreen.Debug, Marker = "Debug Screen", Kind = ScreenKind.Nexus },
            new ScreenDef { Id = "battle", Marker = "Battle Chrome", Kind = ScreenKind.Battle,
                Note = "Loads BattleScene through the prologue battle entry (enc_prologue_sweep)." },
            new ScreenDef { Id = "battle_report", Marker = "ReportPanel", Kind = ScreenKind.BattleReport,
                Note = "Shows the existing BattleScene ReportPanel and BattleReportManager summary. Does not grant rewards." }
        };

        static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            ["starmap"] = "explore",
            ["star_map"] = "explore",
            ["map"] = "explore",
            ["quests"] = "missions",
            ["quest"] = "missions",
            ["gacha"] = "recruit",
            ["gacha_result"] = "recruit_result",
            ["report"] = "battle_report",
            ["settlement"] = "battle_report"
        };

        public static GfDebugResult OpenScreen(string screenId)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return FailScreen(screenId, error);
            error = RefuseOfficial(data);
            if (error != null)
                return FailScreen(screenId, error);

            if (!TryResolve(screenId, out var spec, out error))
                return FailScreen(screenId, error, CatalogData(screenId));

            try
            {
                error = spec.Kind switch
                {
                    ScreenKind.Battle => OpenBattle(),
                    ScreenKind.BattleReport => OpenBattleReport(),
                    ScreenKind.RecruitResult => OpenRecruitResult(),
                    _ => OpenNexus(spec)
                };
            }
            catch (Exception ex)
            {
                return FailScreen(spec.Id, ex.GetType().Name + ": " + ex.Message);
            }

            if (error != null)
                return FailScreen(spec.Id, error);
            return OkScreen(spec, "Opened " + spec.Id + ".");
        }

        public static GfDebugResult CaptureScreen(string screenId, int width, int height)
        {
            if ((width > 0) != (height > 0))
                return FailScreen(screenId, "Pass both width and height, or neither.");
            if (width < 0 || height < 0)
                return FailScreen(screenId, "width and height cannot be negative.");
            if (width > 4096 || height > 4096)
                return FailScreen(screenId, "width and height must be 4096 or less.");

            var opened = OpenScreen(screenId);
            if (!opened.Success)
                return opened;

            string sizeNote = null;
            if (width > 0 && height > 0)
            {
                sizeNote = TrySetGameViewSize(width, height);
                var pumped = Advance(8);
                if (pumped != null && sizeNote == null)
                    sizeNote = pumped;
            }

            ClearTransientOverlays();
            var settle = Advance(4);
            if (settle != null)
                return FailScreen(CanonicalId(screenId), settle);

            string pathError = CaptureGameView(CanonicalId(screenId), out var fullPath, out int texW, out int texH);
            if (pathError != null)
                return FailScreen(CanonicalId(screenId), pathError);

            var spec = FindSpec(CanonicalId(screenId));
            var data = BaseData(spec != null ? spec.Id : CanonicalId(screenId), spec);
            data["success"] = true;
            data["path"] = fullPath;
            data["width"] = texW;
            data["height"] = texH;
            data["capture"] = "ScreenCapture.CaptureScreenshotAsTexture";
            if (!string.IsNullOrEmpty(sizeNote))
                data["gameViewSizeNote"] = sizeNote;
            return GfDebugResult.Ok("Captured " + data["screen_id"] + ".", data);
        }

        static string OpenNexus(ScreenDef spec)
        {
            if (spec.Nav == AppScreen.Debug && DebugModeController.Instance?.IsEnabled != true)
                return "Debug screen stays hidden until Debug Mode is enabled. Settings was not opened instead.";

            var locked = LockReason(spec.Nav);
            if (locked != null)
                return locked;

            string scene = SceneManager.GetActiveScene().name;
            if (scene != nameof(SceneLoader.SceneName.MainScene))
            {
                AppShell.RequestScreen(spec.Nav);
                LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainScene));
                var waited = Pump(SceneChangeSteps, () => MarkerVisible(spec.Marker) && !LoadingCoverVisible());
                if (waited != null)
                    return waited + " " + spec.Marker + " did not appear.";
            }
            else
            {
                var shellWait = Pump(SameSceneSteps, () => GameObject.Find("App Chrome") != null);
                if (shellWait != null)
                    return "AppShell chrome did not finish building.";
                var shell = AppShell.Instance ?? UnityEngine.Object.FindFirstObjectByType<AppShell>();
                if (shell == null)
                    return "AppShell is not in MainScene.";
                shell.Navigate(spec.Nav);
                if (!MarkerVisible(spec.Marker))
                {
                    var waited = Pump(SameSceneSteps, () => MarkerVisible(spec.Marker) && !LoadingCoverVisible());
                    if (waited != null)
                        return waited + " " + spec.Marker + " did not appear. The page may have been blocked.";
                }
            }

            SettlePage();
            if (!MarkerVisible(spec.Marker))
                return spec.Marker + " was covered or destroyed after opening.";
            return null;
        }

        static string OpenRecruitResult()
        {
            var recruit = FindSpec("recruit");
            var error = OpenNexus(recruit);
            if (error != null)
                return error;

            GachaService.EnsureLoaded(DataUtil.Instance);
            int pullsBefore = GachaService.State != null ? GachaService.State.totalPulls : 0;
            if (!GachaService.CanAfford(1, out _))
            {
                if (!ProductionService.TryAddLocal(ItemFactory.FromDef(GachaRules.TicketDefId, 1)))
                    return "Recruit result needs one " + GachaRules.TicketDefId + " and the warehouse rejected it.";
            }

            var button = GameObject.Find("PullOne")?.GetComponent<Button>();
            if (button == null)
                return "Recruit PullOne button was not found.";
            button.onClick.Invoke();

            int pullsAfter = GachaService.State != null ? GachaService.State.totalPulls : 0;
            var status = GameObject.Find("Status")?.GetComponent<TMP_Text>();
            if (pullsAfter <= pullsBefore)
            {
                string detail = status != null ? status.text : "No status text.";
                return "Recruit pull did not complete. " + detail;
            }

            if (status == null || string.IsNullOrWhiteSpace(status.text))
                return "Recruit pull completed but the result text was not shown.";
            SettlePage();
            return null;
        }

        static string OpenBattle()
        {
            if (MarkerVisible("Battle Chrome") && !LoadingCoverVisible()
                && SceneManager.GetActiveScene().name == nameof(SceneLoader.SceneName.BattleScene))
            {
                SettlePage();
                return MarkerVisible("Battle Chrome") ? null : "Battle Chrome disappeared.";
            }

            var jump = JumpToBattle(ChapterQuestService.PrologueEncounterId, "");
            if (!jump.Success)
                return jump.Message;

            var waited = Pump(SceneChangeSteps, () =>
                MarkerVisible("Battle Chrome")
                && SceneManager.GetActiveScene().name == nameof(SceneLoader.SceneName.BattleScene)
                && !LoadingCoverVisible());
            if (waited != null)
                return "Battle scene did not show Battle Chrome. " + waited;
            SettlePage();
            if (!MarkerVisible("Battle Chrome"))
                return "Battle Chrome was not visible after the loading cover cleared.";
            return null;
        }

        static string OpenBattleReport()
        {
            var error = OpenBattle();
            if (error != null)
                return error;

            var battle = BattleController.Instance;
            if (battle == null)
                return "BattleController is not in the loaded battle scene.";
            if (battle.reportPanel == null)
                return "BattleController.reportPanel is not assigned. The settlement panel cannot be shown.";

            battle.StopAllCoroutines();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            battle.GetType().GetMethod("WireReportConfirm", flags)?.Invoke(battle, null);
            battle.reportPanel.SetActive(true);
            battle.reportPanel.transform.SetAsLastSibling();

            var overlay = battle.reportPanel.GetComponent<Canvas>();
            if (overlay == null)
                overlay = battle.reportPanel.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 480;

            battle.GetType().GetMethod("ApplyReportTitle", flags)?.Invoke(battle, null);
            var summary = battle.GetType().GetMethod("BuildReportSummary", flags)?.Invoke(battle, null) as string;
            var cards = battle.GetType().GetField("playerCards", flags)?.GetValue(battle) as List<Card>;
            if (BattleReportManager.Instance == null)
                return "BattleReportManager.Instance is missing on the report panel.";

            BattleReportManager.Instance.RefreshChart(ChartType.pDamageChart, cards);
            BattleReportManager.Instance.RefreshChart(ChartType.pInjuryChart, cards);
            BattleReportManager.Instance.RefreshChart(ChartType.pHealChart, cards);
            BattleReportManager.Instance.ShowSummary(summary ?? "");

            var waited = Pump(SameSceneSteps, () =>
                battle.reportPanel.activeInHierarchy && !LoadingCoverVisible());
            if (waited != null)
                return "ReportPanel did not stay visible. " + waited;
            SettlePage();
            if (!battle.reportPanel.activeInHierarchy)
                return "ReportPanel was hidden before the shot.";
            return null;
        }

        static void SettlePage()
        {
            Advance(3);
            ClearTransientOverlays();
            Advance(3);
        }

        static void ClearTransientOverlays()
        {
            TutorialGuideService.Dismiss();
            DialogueService.Close();
            InvokeStaticClose("Assets.Resources.Scripts.UI.Nexus.NexusDialog");
            InvokeStaticClose("Assets.Resources.Scripts.UI.Nexus.RewardPopup");
            InvokeStaticClose("Assets.Resources.Scripts.UI.Nexus.NexusSnackbar");
            InvokeStaticClose("Assets.Resources.Scripts.UI.Nexus.Tutorial.TutorialGuideOverlay");
            InvokeStaticClose("Assets.Resources.Scripts.ChapterQuest.StrategyComparePopup");
            InvokeStaticClose("Assets.Resources.Scripts.UI.Nexus.ModuleUpgradePopup");
        }

        static void InvokeStaticClose(string typeName)
        {
            var type = typeof(AppShell).Assembly.GetType(typeName);
            var method = type?.GetMethod("Close", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, null);
        }

        static string LockReason(AppScreen screen)
        {
            FeatureUnlockService.EnsureLoaded(DataUtil.Instance);
            if (FeatureUnlockService.IsScreenUnlocked(screen.ToString()))
                return null;
            var def = FeatureUnlockService.DefForScreen(screen.ToString());
            string hint = def != null ? def.hintEn : "";
            string zh = def != null ? def.hintZh : "";
            return "Screen " + screen + " is locked. " + hint + " " + zh;
        }

        static string RefuseOfficial(DataUtil data)
        {
            var bound = BoundPlayerDirectory(data);
            if (string.IsNullOrEmpty(bound))
                return "No player save directory is bound. Use gf_new_player first.";
            if (IsOfficialSave(bound) || !IsSandbox(bound))
                return "Refusing to drive pages while the player is outside Temp/gf-debug-saves. Use gf_new_player first.";
            return EnsureSandboxWrites(data);
        }

        static bool TryResolve(string screenId, out ScreenDef spec, out string error)
        {
            spec = null;
            error = null;
            string id = CanonicalId(screenId);
            if (string.IsNullOrEmpty(id))
            {
                error = "screen_id is required. Supported: " + string.Join(", ", Ids());
                return false;
            }

            spec = FindSpec(id);
            if (spec != null)
                return true;
            error = "Unknown screen_id '" + screenId + "'. Supported: " + string.Join(", ", Ids());
            return false;
        }

        static string CanonicalId(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return "";
            string id = screenId.Trim().ToLowerInvariant();
            return Aliases.TryGetValue(id, out var mapped) ? mapped : id;
        }

        static ScreenDef FindSpec(string id)
        {
            foreach (var spec in Screens)
            {
                if (spec.Id == id)
                    return spec;
            }

            return null;
        }

        static IEnumerable<string> Ids()
        {
            foreach (var spec in Screens)
                yield return spec.Id;
        }

        static bool MarkerVisible(string name)
        {
            var go = GameObject.Find(name);
            return go != null && go.activeInHierarchy;
        }

        static bool LoadingCoverVisible()
        {
            var canvas = GameObject.Find("Loading Canvas");
            if (canvas == null || !canvas.activeInHierarchy)
                return false;
            var group = canvas.GetComponent<CanvasGroup>();
            return group == null || group.alpha > 0.05f;
        }

        static string Advance(int steps)
        {
            bool wasPaused = EditorApplication.isPaused;
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                for (int i = 0; i < steps; i++)
                    EditorApplication.Step();
                return null;
            }
            catch (Exception ex)
            {
                return "Play Mode step failed: " + ex.Message;
            }
            finally
            {
                if (!wasPaused)
                    EditorApplication.isPaused = false;
            }
        }

        /// <summary>Returns null when <paramref name="ready"/> is true, or an error string.</summary>
        static string Pump(int maxSteps, Func<bool> ready)
        {
            if (ready())
                return null;
            bool wasPaused = EditorApplication.isPaused;
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                for (int i = 0; i < maxSteps && !ready(); i++)
                    EditorApplication.Step();
            }
            catch (Exception ex)
            {
                return "Play Mode step failed: " + ex.Message;
            }
            finally
            {
                if (!wasPaused)
                    EditorApplication.isPaused = false;
            }

            return ready() ? null : "Timed out waiting for the page.";
        }

        static string TrySetGameViewSize(int width, int height)
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                    return "Game view type was not found; captured at the current size.";
                var window = EditorWindow.GetWindow(gameViewType);
                var method = gameViewType.GetMethod(
                    "SetCustomResolution",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (method == null)
                    return "Game view has no SetCustomResolution; captured at the current size.";
                var parameters = method.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Vector2))
                    method.Invoke(window, new object[] { new Vector2(width, height), "gf-ui-shot" });
                else if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(int)
                    && parameters[1].ParameterType == typeof(int))
                    method.Invoke(window, new object[] { width, height, "gf-ui-shot" });
                else
                    return "SetCustomResolution signature was not recognized; captured at the current size.";
                window.Repaint();
                return null;
            }
            catch (Exception ex)
            {
                return "Could not resize the Game view (" + ex.Message + "). Captured at the current size.";
            }
        }

        static void EnsureGameView()
        {
            try
            {
                if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
                    EditorApplication.ExecuteMenuItem("Window/General/Game %2");
            }
            catch (Exception)
            {
                // The view may already be open under a different menu path.
            }

            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return;
            var window = EditorWindow.GetWindow(gameViewType);
            window?.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static string CaptureGameView(string screenId, out string fullPath, out int width, out int height)
        {
            fullPath = null;
            width = 0;
            height = 0;
            EnsureGameView();

            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", ShotFolderName));
            Directory.CreateDirectory(folder);
            string name = Sanitize(screenId) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png";
            fullPath = Path.Combine(folder, name);

            Texture2D tex = null;
            string captureError = null;
            bool wasPaused = EditorApplication.isPaused;
            try
            {
                var host = new GameObject("__gf_ui_shot__") { hideFlags = HideFlags.HideAndDontSave };
                var capturer = host.AddComponent<GfUiShotCapturer>();
                capturer.Begin(texture =>
                {
                    tex = texture;
                }, message => captureError = message);

                for (int i = 0; i < 8 && tex == null && captureError == null; i++)
                    EditorApplication.Step();
            }
            catch (Exception ex)
            {
                captureError = ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                if (!wasPaused)
                    EditorApplication.isPaused = false;
            }

            if (captureError != null)
                return captureError;
            if (tex == null)
                return "ScreenCapture did not return a Game view frame. A camera-only shot was not used, because it would miss overlay UI.";

            try
            {
                if (!HasVisiblePixels(tex))
                    return "The Game view frame was blank, so it was not saved as a successful shot.";
                width = tex.width;
                height = tex.height;
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }

            if (!File.Exists(fullPath) || new FileInfo(fullPath).Length < 64)
                return "Screenshot file was not written: " + fullPath;
            fullPath = Path.GetFullPath(fullPath);
            return null;
        }

        static bool HasVisiblePixels(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            if (pixels == null || pixels.Length == 0)
                return false;
            int samples = 0;
            int lit = 0;
            for (int i = 0; i < pixels.Length; i += 64)
            {
                samples++;
                var p = pixels[i];
                if (p.r > 12 || p.g > 12 || p.b > 12)
                    lit++;
            }

            return samples > 0 && lit * 20 >= samples;
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "screen";
            foreach (var ch in Path.GetInvalidFileNameChars())
                value = value.Replace(ch, '_');
            return value.Replace(' ', '_');
        }

        static GfDebugResult OkScreen(ScreenDef spec, string message)
        {
            var data = BaseData(spec.Id, spec);
            data["success"] = true;
            return GfDebugResult.Ok(message, data);
        }

        static GfDebugResult FailScreen(string screenId, string message, Dictionary<string, object> data = null)
        {
            string id = CanonicalId(screenId);
            data ??= BaseData(string.IsNullOrEmpty(id) ? screenId ?? "" : id, FindSpec(id));
            data["success"] = false;
            data["screen_id"] = string.IsNullOrEmpty(id) ? screenId ?? "" : id;
            data["reason"] = message ?? "";
            return GfDebugResult.Fail(message, data);
        }

        static Dictionary<string, object> BaseData(string screenId, ScreenDef spec)
        {
            var data = DataUtil.Instance;
            string bound = BoundPlayerDirectory(data);
            var payload = new Dictionary<string, object>
            {
                ["success"] = false,
                ["screen_id"] = screenId ?? "",
                ["scene"] = Application.isPlaying ? SceneManager.GetActiveScene().name : "",
                ["savePath"] = bound ?? "",
                ["sandbox"] = IsSandbox(bound),
                ["officialSaveUntouched"] = !IsOfficialSave(bound)
            };
            if (spec != null && !string.IsNullOrEmpty(spec.Note))
                payload["note"] = spec.Note;
            if (spec != null)
                payload["marker"] = spec.Marker;
            return payload;
        }

        static Dictionary<string, object> CatalogData(string screenId)
        {
            var data = BaseData(CanonicalId(screenId), null);
            data["supported"] = new List<string>(Ids());
            return data;
        }

        sealed class GfUiShotCapturer : MonoBehaviour
        {
            Action<Texture2D> done;
            Action<string> failed;

            public void Begin(Action<Texture2D> onDone, Action<string> onFailed)
            {
                done = onDone;
                failed = onFailed;
            }

            IEnumerator Start()
            {
                yield return new WaitForEndOfFrame();
                Texture2D tex = null;
                try
                {
                    tex = ScreenCapture.CaptureScreenshotAsTexture(1);
                }
                catch (Exception ex)
                {
                    failed?.Invoke("CaptureScreenshotAsTexture failed: " + ex.Message);
                    Destroy(gameObject);
                    yield break;
                }

                if (tex == null)
                    failed?.Invoke("CaptureScreenshotAsTexture returned null.");
                else
                    done?.Invoke(tex);
                Destroy(gameObject);
            }
        }
    }
}
#endif
