using System;
using System.Linq;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using InterrogationRoom.UI;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Debugging
{
    /// <summary>
    /// Local host pilot for the Room playground. It prepares a valid domain
    /// roster, but leaves objective, clue, incident and escape progress on the
    /// real physical interaction path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoundDeveloperPanel : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;
        private const float PanelWidth = 480f;
        private const float PanelMaxHeight = 690f;
        private const float PanelMargin = 12f;
        private const float ShortcutStripHeight = 68f;

        [SerializeField] private NetworkRoundCoordinator coordinator;

        private bool _isVisible;
        private bool _expanded = true;
        private int _targetPlayerCount = RoundEngine.MinPlayers;
        private int _controlledPlayerId;
        private string _status;
        private Vector2 _scroll;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _paperTexture;
        private Texture2D _greenTexture;
        private Texture2D _greenHoverTexture;
        private Texture2D _greenPressedTexture;

        private void Reset()
        {
            coordinator = GetComponent<NetworkRoundCoordinator>();
        }

        private void Awake()
        {
            if (coordinator == null)
                coordinator = GetComponent<NetworkRoundCoordinator>();
        }

        private void OnDestroy()
        {
            DestroyTexture(_paperTexture);
            DestroyTexture(_greenTexture);
            DestroyTexture(_greenHoverTexture);
            DestroyTexture(_greenPressedTexture);
        }

        private void OnGUI()
        {
            if (!_isVisible || !NetworkRoundCoordinator.DeveloperToolsAvailable || coordinator == null)
                return;

            InitializeStyles();

            float guiScale = CalculateGuiScale(Screen.height);
            float virtualWidth = Screen.width / guiScale;
            float virtualHeight = Screen.height / guiScale;
            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
                DrawPanel(virtualWidth, virtualHeight);
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        public static float CalculateGuiScale(int screenHeight)
        {
            return Mathf.Max(0.01f, screenHeight / ReferenceHeight);
        }

        private void DrawPanel(float virtualWidth, float virtualHeight)
        {
            float panelHeight = Mathf.Min(
                virtualHeight - (PanelMargin * 2f) - ShortcutStripHeight,
                PanelMaxHeight);
            var area = new Rect(
                virtualWidth - PanelWidth - PanelMargin,
                PanelMargin,
                PanelWidth,
                panelHeight);
            GUILayout.BeginArea(area, _boxStyle);
            GUILayout.Label(UiText.Get("AKTA TESTOWE • NARZĘDZIA RUNDY"), _labelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(UiText.Get("TRYB DEVELOPERSKI — BEZ LIMITU"), _headerStyle);
            if (GUILayout.Button(_expanded ? "—" : "+", _buttonStyle, GUILayout.Width(42f)))
                _expanded = !_expanded;
            GUILayout.EndHorizontal();

            if (!_expanded)
            {
                GUILayout.Label(UiText.Get("F8 lub + otwiera panel"), _labelStyle);
                GUILayout.EndArea();
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawContents();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
        }

        private void DrawContents()
        {
            if (!NetworkServer.activeHost || !coordinator.IsLocalHost)
            {
                GUILayout.Label(UiText.Get(
                    "Uruchom Host (Server + Client) w launcherze sieciowym."), _labelStyle);
                return;
            }

            var activePlan = coordinator.ActiveDeveloperPlan;
            if (activePlan == null)
                DrawLobbySetup();
            else
                DrawActiveScenario(activePlan);

            if (!string.IsNullOrWhiteSpace(_status))
                GUILayout.Label(_status, _labelStyle);
        }

        private void DrawLobbySetup()
        {
            var connected = coordinator.ConnectedPlayers;
            GUILayout.Label(
                UiText.Format(
                    "Prawdziwi gracze: {0}. Brakujące miejsca będą technicznymi slotami domeny.",
                    connected.Count),
                _labelStyle);

            DrawLobbyRosterPreviewControls(connected.Count);

            if (connected.Count == 0)
            {
                GUILayout.Label(UiText.Get(
                    "Poczekaj, aż lokalna postać zostanie zespawnowana."), _labelStyle);
                return;
            }

            if (!connected.Any(player => player.Value == _controlledPlayerId))
                _controlledPlayerId = connected[0].Value;

            GUILayout.Label(UiText.Get("Testowany prawdziwy gracz"), _headerStyle);
            GUILayout.BeginHorizontal();
            foreach (var player in connected)
            {
                string prefix = player.Value == _controlledPlayerId ? "✓ " : string.Empty;
                if (GUILayout.Button(UiText.Format(
                    "{0}Gracz {1}", prefix, player.Value), _buttonStyle))
                    _controlledPlayerId = player.Value;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(UiText.Get("Docelowy Skład Rundy"), _headerStyle);
            GUILayout.BeginHorizontal();
            for (var count = RoundEngine.MinPlayers; count <= RoundEngine.MaxPlayers; count++)
            {
                string prefix = count == _targetPlayerCount ? "✓ " : string.Empty;
                if (GUILayout.Button($"{prefix}{count}", _buttonStyle))
                    _targetPlayerCount = count;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(UiText.Get("Scenariusz"), _headerStyle);
            DrawStartButton(UiText.Get("Niewinny — Osobista Sprawa"), RoundDeveloperScenario.PersonalMatter);
            GUI.enabled = _targetPlayerCount >= RoundEngine.MinPlayersForSecretObjective;
            DrawStartButton(UiText.Get("Niewinny — Sekretny Cel"), RoundDeveloperScenario.SecretObjective);
            GUI.enabled = true;
            DrawStartButton(UiText.Get("Winny — Trop i Ucieczka"), RoundDeveloperScenario.GuiltyEscape);
            DrawStartButton(UiText.Get("Detektyw — Incydenty"), RoundDeveloperScenario.DetectiveIncidents);

            GUILayout.Label(
                UiText.Get("Wybór scenariusza wymusza rolę testowanego gracza. Runda nie kończy się automatycznie po czasie. Panel działa tylko w Editorze i Development Buildzie."),
                _labelStyle);
        }

        private void DrawLobbyRosterPreviewControls(int connectedPlayerCount)
        {
            GUILayout.Space(8f);
            GUILayout.Label(UiText.Get("Podgląd listy lobby"), _headerStyle);
            GUILayout.Label(
                UiText.Format(
                    "Gracze testowi: {0}. Są widoczni wyłącznie w lobby i nie wchodzą do Rundy.",
                    coordinator.DeveloperLobbyFakePlayerCount),
                _labelStyle);

            GUILayout.BeginHorizontal();
            GUI.enabled = coordinator.DeveloperLobbyFakePlayerCount > 0;
            if (GUILayout.Button("−1", _buttonStyle))
                coordinator.TrySetDeveloperLobbyFakePlayerCount(coordinator.DeveloperLobbyFakePlayerCount - 1);
            GUI.enabled = connectedPlayerCount + coordinator.DeveloperLobbyFakePlayerCount < RoundEngine.MaxPlayers;
            if (GUILayout.Button("+1", _buttonStyle))
                coordinator.TrySetDeveloperLobbyFakePlayerCount(coordinator.DeveloperLobbyFakePlayerCount + 1);
            GUI.enabled = true;
            if (GUILayout.Button(UiText.Get("Pokaż 5 graczy"), _buttonStyle))
                coordinator.TrySetDeveloperLobbyFakePlayerCount(Math.Max(0, 5 - connectedPlayerCount));
            if (GUILayout.Button(UiText.Get("Pokaż 8 graczy"), _buttonStyle))
                coordinator.TrySetDeveloperLobbyFakePlayerCount(Math.Max(0, 8 - connectedPlayerCount));
            GUILayout.EndHorizontal();

            if (GUILayout.Button(UiText.Get("Usuń graczy testowych"), _buttonStyle))
                coordinator.TrySetDeveloperLobbyFakePlayerCount(0);
        }

        private void DrawStartButton(string label, RoundDeveloperScenario scenario)
        {
            if (!GUILayout.Button(label, _buttonStyle))
                return;

            bool started = coordinator.TryStartDeveloperScenario(
                scenario,
                _targetPlayerCount,
                new PlayerId(_controlledPlayerId),
                out var reason);
            if (!started)
                Debug.LogWarning($"[RoundDeveloperPanel] Scenario start rejected: {reason}", this);
            _status = started
                ? UiText.Get("Scenariusz uruchomiony.")
                : UiText.Get("Nie udało się uruchomić scenariusza.");
        }

        private void DrawActiveScenario(RoundDeveloperPlan plan)
        {
            var view = coordinator.DeveloperControlledView;
            GUILayout.Label(ScenarioLabel(plan.Scenario), _headerStyle);
            GUILayout.Label(
                UiText.Format(
                    "Testowany: Gracz {0} | skład {1} ({2} prawdziwych + {3} technicznych)",
                    plan.ControlledPlayer.Value,
                    plan.Players.Count,
                    plan.ConnectedPlayerCount,
                    plan.TechnicalPlayerCount),
                _labelStyle);

            if (view == null)
            {
                GUILayout.Label(UiText.Get(
                    "Oczekiwanie na prywatny widok testowanego gracza."), _labelStyle);
                return;
            }

            GUILayout.Label(UiText.Format(
                "Rola: {0} | faza: {1}",
                RoleLabel(view.Role),
                UiText.Get(view.Phase.ToString())), _labelStyle);
            GUILayout.Label(DescribeNextStep(plan, view), _labelStyle);

            if (view.Phase == RoundPhase.Preparation)
            {
                if (GUILayout.Button(UiText.Get("Zakończ Przygotowanie"), _buttonStyle))
                    coordinator.RequestEndPreparation();
                return;
            }

            if (view.Phase == RoundPhase.Round)
            {
                DrawDeveloperEndings(plan);
                return;
            }

            if (view.Phase == RoundPhase.Finished)
            {
                if (view.Result != null)
                {
                    GUILayout.Label(
                        UiText.Format(
                            "Wynik testowanego gracza: {0} ({1})",
                            UiText.Get(view.Result.Won ? "WYGRANA" : "PRZEGRANA"),
                            UiText.Get(view.Result.EndCause.ToString())),
                        _headerStyle);
                }

                if (GUILayout.Button(UiText.Get("Wróć do lobby i zresetuj świat"), _buttonStyle))
                    coordinator.RequestReturnToLobby();
            }
        }

        private void DrawDeveloperEndings(RoundDeveloperPlan plan)
        {
            GUILayout.Space(8f);
            GUILayout.Label(UiText.Get("Symulowane zakończenie (opcjonalne)"), _headerStyle);
            switch (plan.Scenario)
            {
                case RoundDeveloperScenario.PersonalMatter:
                    DrawFinishButton(UiText.Get("Wymuś upływ Limitu Rundy"), RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.SecretObjective:
                    DrawFinishButton(UiText.Get("Wymuś Egzekucję Celu"), RoundDeveloperFinish.ExecuteSecretTarget);
                    DrawFinishButton(UiText.Get("Wymuś upływ Limitu Rundy"), RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.GuiltyEscape:
                    DrawFinishButton(UiText.Get("Wymuś upływ Limitu Rundy"), RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.DetectiveIncidents:
                    DrawFinishButton(UiText.Get("Wymuś Egzekucję Winnego"), RoundDeveloperFinish.ExecuteGuilty);
                    DrawFinishButton(UiText.Get("Wymuś Egzekucję Niewinnego"), RoundDeveloperFinish.ExecuteInnocent);
                    GUILayout.Label(
                        UiText.Get("Prawdziwy strzał wymaga drugiej prawdziwej postaci z hitboxem. Incydenty uruchamiane fizycznie przez testowanego Detektywa są przypisywane technicznemu Podejrzanemu."),
                        _labelStyle);
                    break;
            }
        }

        private void DrawFinishButton(string label, RoundDeveloperFinish finish)
        {
            if (!GUILayout.Button(label, _buttonStyle))
                return;

            bool finished = coordinator.TryFinishDeveloperScenario(finish, out var reason);
            if (!finished)
                Debug.LogWarning($"[RoundDeveloperPanel] Scenario finish rejected: {reason}", this);
            _status = finished
                ? UiText.Get("Runda zakończona.")
                : UiText.Get("Nie udało się zakończyć scenariusza.");
        }

        private static string DescribeNextStep(RoundDeveloperPlan plan, PlayerRoundView view)
        {
            if (view.Phase == RoundPhase.Preparation)
                return UiText.Get("Sprawdź prywatną rolę i Alibi, następnie zakończ Przygotowanie.");
            if (view.Phase == RoundPhase.Finished)
                return UiText.Get("Sprawdź wynik i pełne ujawnienie, potem wróć do lobby.");

            switch (plan.Scenario)
            {
                case RoundDeveloperScenario.PersonalMatter:
                case RoundDeveloperScenario.SecretObjective:
                    return DescribeObjective(view.PrivateObjective);
                case RoundDeveloperScenario.GuiltyEscape:
                    return DescribeGuilty(view);
                case RoundDeveloperScenario.DetectiveIncidents:
                    return UiText.Get("Podejdź do Archive Alarm albo podłóż przedmiot w Target Locker. Hałaśliwy Incydent pojawi się od razu; Cichy odkryj ponownie przy zmienionym obiekcie.");
                default:
                    return string.Empty;
            }
        }

        private static string DescribeObjective(PrivateObjectiveView objective)
        {
            if (objective == null)
                return UiText.Get("Brak Prywatnego Celu dla wybranego widoku.");
            if (objective.IsCompleted)
                return UiText.Get("Prywatny Cel ukończony. Użyj symulowanego zakończenia, aby sprawdzić wynik.");
            if (!objective.CurrentStep.HasValue)
                return UiText.Get("Oczekiwanie na następny krok Celu.");

            switch (objective.CurrentStep.Value.Value)
            {
                case "osobista-sprawa-przygotuj":
                    return UiText.Get("Krok 1: przeszukaj Records Cabinet albo Evidence Shelf.");
                case "osobista-sprawa-zakoncz":
                    return UiText.Get("Krok 2: ukryj dokument w Locker albo Archive Slot.");
                case "wrobienie-przygotuj":
                    return UiText.Get("Krok 1 Wrobienia: zabierz przedmiot z Evidence Tray.");
                case "wrobienie-podloz":
                    return UiText.Get("Krok 2 Wrobienia: podłóż przedmiot w Target Locker.");
                default:
                    return UiText.Format(
                        "Wykonaj fizyczny krok: {0}.",
                        objective.CurrentStep.Value.Value);
            }
        }

        private static string DescribeGuilty(PlayerRoundView view)
        {
            string clue = view.AcquiredAlibiClues == null || view.AcquiredAlibiClues.Count == 0
                ? UiText.Get("Opcjonalnie przeszukaj Crumpled Receipt, aby zdobyć Trop. ")
                : UiText.Get("Trop zdobyty. ");
            var plan = view.EscapePlan;
            if (plan == null)
                return clue + UiText.Get("Brak Planu Ucieczki w prywatnym widoku.");
            if (plan.CurrentStep.HasValue)
            {
                switch (plan.CurrentStep.Value.Value)
                {
                    case "escape-find-tool":
                        return clue + UiText.Get("Plan: przeszukaj Maintenance Cabinet.");
                    case "escape-open-route":
                        return clue + UiText.Get("Plan: sprawdź Service Panel.");
                }
            }

            if (!plan.ExitOptions.Any(option => option.IsPrepared))
                return clue + UiText.Get("Przygotuj Vent Control albo Loading Gate Control, następnie użyj odpowiadającego wyjścia.");
            if (plan.ActiveExit.HasValue)
                return clue + UiText.Format(
                    "Trwa finał Ucieczki przy {0}.", plan.ActiveExit.Value.Value);
            return clue + UiText.Get("Użyj przygotowanego Service Vent albo Loading Gate Exit.");
        }

        private static string ScenarioLabel(RoundDeveloperScenario scenario)
        {
            switch (scenario)
            {
                case RoundDeveloperScenario.PersonalMatter: return UiText.Get("NIEWINNY — OSOBISTA SPRAWA");
                case RoundDeveloperScenario.SecretObjective: return UiText.Get("NIEWINNY — SEKRETNY CEL");
                case RoundDeveloperScenario.GuiltyEscape: return UiText.Get("WINNY — TROP I UCIECZKA");
                case RoundDeveloperScenario.DetectiveIncidents: return UiText.Get("DETEKTYW — INCYDENTY");
                default: return scenario.ToString();
            }
        }

        private static string RoleLabel(RoundRole role)
        {
            switch (role)
            {
                case RoundRole.Detective: return UiText.Get("Detektyw");
                case RoundRole.Guilty: return UiText.Get("Winny");
                case RoundRole.Innocent: return UiText.Get("Niewinny");
                default: return role.ToString();
            }
        }

        private void InitializeStyles()
        {
            if (_boxStyle != null)
                return;

            _paperTexture = CreateTexture(new Color32(0xE8, 0xDC, 0xC5, 0xFC));
            _greenTexture = CreateTexture(new Color32(0x41, 0x5B, 0x4C, 0xFF));
            _greenHoverTexture = CreateTexture(new Color32(0x4E, 0x6E, 0x5B, 0xFF));
            _greenPressedTexture = CreateTexture(new Color32(0x33, 0x47, 0x3C, 0xFF));

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 16, 18),
                normal = { background = _paperTexture },
                border = new RectOffset(2, 2, 2, 2)
            };
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color32(0x2B, 0x2A, 0x24, 0xFF) }
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color32(0x6E, 0x68, 0x57, 0xFF) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 38f,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color32(0xE8, 0xE3, 0xD5, 0xFF), background = _greenTexture },
                hover = { textColor = Color.white, background = _greenHoverTexture },
                active = { textColor = Color.white, background = _greenPressedTexture },
                focused = { textColor = Color.white, background = _greenHoverTexture },
                padding = new RectOffset(12, 12, 6, 6),
                border = new RectOffset(1, 1, 1, 1)
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
                Destroy(texture);
        }

    }
}
