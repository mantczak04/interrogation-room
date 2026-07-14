using System;
using System.Linq;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
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
        [SerializeField] private NetworkRoundCoordinator coordinator;

        private bool _expanded = true;
        private int _targetPlayerCount = RoundEngine.MinPlayers;
        private int _controlledPlayerId;
        private string _status;
        private Vector2 _scroll;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        private void Reset()
        {
            coordinator = GetComponent<NetworkRoundCoordinator>();
        }

        private void Awake()
        {
            if (coordinator == null)
                coordinator = GetComponent<NetworkRoundCoordinator>();
        }

        private void OnGUI()
        {
            if (!NetworkRoundCoordinator.DeveloperToolsAvailable || coordinator == null)
                return;

            InitializeStyles();
            HandleToggleKey();

            const float panelWidth = 440f;
            float panelHeight = Mathf.Min(Screen.height - 24f, 690f);
            var area = new Rect(Screen.width - panelWidth - 12f, 12f, panelWidth, panelHeight);
            GUILayout.BeginArea(area, _boxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("A7a — SANDBOX RUNDY", _headerStyle);
            if (GUILayout.Button(_expanded ? "—" : "+", _buttonStyle, GUILayout.Width(42f)))
                _expanded = !_expanded;
            GUILayout.EndHorizontal();

            if (!_expanded)
            {
                GUILayout.Label("F8 lub + otwiera panel", _labelStyle);
                GUILayout.EndArea();
                return;
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawContents();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawContents()
        {
            if (!NetworkServer.activeHost || !coordinator.IsLocalHost)
            {
                GUILayout.Label("Uruchom Host (Server + Client) w launcherze sieciowym.", _labelStyle);
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
                $"Prawdziwi gracze: {connected.Count}. Brakujące miejsca będą technicznymi slotami domeny.",
                _labelStyle);

            if (connected.Count == 0)
            {
                GUILayout.Label("Poczekaj, aż lokalna postać zostanie zespawnowana.", _labelStyle);
                return;
            }

            if (!connected.Any(player => player.Value == _controlledPlayerId))
                _controlledPlayerId = connected[0].Value;

            GUILayout.Label("Testowany prawdziwy gracz", _headerStyle);
            GUILayout.BeginHorizontal();
            foreach (var player in connected)
            {
                string prefix = player.Value == _controlledPlayerId ? "✓ " : string.Empty;
                if (GUILayout.Button($"{prefix}Player {player.Value}", _buttonStyle))
                    _controlledPlayerId = player.Value;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Docelowy Skład Rundy", _headerStyle);
            GUILayout.BeginHorizontal();
            for (var count = RoundEngine.MinPlayers; count <= RoundEngine.MaxPlayers; count++)
            {
                string prefix = count == _targetPlayerCount ? "✓ " : string.Empty;
                if (GUILayout.Button($"{prefix}{count}", _buttonStyle))
                    _targetPlayerCount = count;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Scenariusz", _headerStyle);
            DrawStartButton("Niewinny — Osobista Sprawa", RoundDeveloperScenario.PersonalMatter);
            GUI.enabled = _targetPlayerCount > RoundEngine.MinPlayers;
            DrawStartButton("Niewinny — Sekretny Cel", RoundDeveloperScenario.SecretObjective);
            GUI.enabled = true;
            DrawStartButton("Winny — Trop i Ucieczka", RoundDeveloperScenario.GuiltyEscape);
            DrawStartButton("Detektyw — Incydenty", RoundDeveloperScenario.DetectiveIncidents);

            GUILayout.Label(
                "Normalny przycisk Start Rundy nadal wymaga 4–6 prawdziwych klientów. Ten panel działa tylko w Editorze i Development Buildzie.",
                _labelStyle);
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
            _status = started ? "Scenariusz uruchomiony." : reason;
        }

        private void DrawActiveScenario(RoundDeveloperPlan plan)
        {
            var view = coordinator.DeveloperControlledView;
            GUILayout.Label(ScenarioLabel(plan.Scenario), _headerStyle);
            GUILayout.Label(
                $"Testowany: Player {plan.ControlledPlayer.Value} | skład {plan.Players.Count} " +
                $"({plan.ConnectedPlayerCount} prawdziwych + {plan.TechnicalPlayerCount} technicznych)",
                _labelStyle);

            if (view == null)
            {
                GUILayout.Label("Oczekiwanie na prywatny widok testowanego gracza.", _labelStyle);
                return;
            }

            GUILayout.Label($"Rola: {RoleLabel(view.Role)} | faza: {view.Phase}", _labelStyle);
            GUILayout.Label(DescribeNextStep(plan, view), _labelStyle);

            if (view.Phase == RoundPhase.Preparation)
            {
                if (GUILayout.Button("Zakończ Przygotowanie", _buttonStyle))
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
                        $"Wynik testowanego gracza: {(view.Result.Won ? "WYGRANA" : "PRZEGRANA")} " +
                        $"({view.Result.EndCause})",
                        _headerStyle);
                }

                if (GUILayout.Button("Wróć do lobby i zresetuj świat", _buttonStyle))
                    coordinator.RequestReturnToLobby();
            }
        }

        private void DrawDeveloperEndings(RoundDeveloperPlan plan)
        {
            GUILayout.Space(8f);
            GUILayout.Label("Symulowane zakończenie (opcjonalne)", _headerStyle);
            switch (plan.Scenario)
            {
                case RoundDeveloperScenario.PersonalMatter:
                    DrawFinishButton("Wymuś upływ Limitu Rundy", RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.SecretObjective:
                    DrawFinishButton("Wymuś Egzekucję Celu", RoundDeveloperFinish.ExecuteSecretTarget);
                    DrawFinishButton("Wymuś upływ Limitu Rundy", RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.GuiltyEscape:
                    DrawFinishButton("Wymuś upływ Limitu Rundy", RoundDeveloperFinish.TimeExpired);
                    break;
                case RoundDeveloperScenario.DetectiveIncidents:
                    DrawFinishButton("Wymuś Egzekucję Winnego", RoundDeveloperFinish.ExecuteGuilty);
                    DrawFinishButton("Wymuś Egzekucję Niewinnego", RoundDeveloperFinish.ExecuteInnocent);
                    GUILayout.Label(
                        "Prawdziwy strzał wymaga drugiej prawdziwej postaci z hitboxem. Incydenty uruchamiane fizycznie przez testowanego Detektywa są przypisywane technicznemu Podejrzanemu.",
                        _labelStyle);
                    break;
            }
        }

        private void DrawFinishButton(string label, RoundDeveloperFinish finish)
        {
            if (!GUILayout.Button(label, _buttonStyle))
                return;

            bool finished = coordinator.TryFinishDeveloperScenario(finish, out var reason);
            _status = finished ? "Runda zakończona." : reason;
        }

        private static string DescribeNextStep(RoundDeveloperPlan plan, PlayerRoundView view)
        {
            if (view.Phase == RoundPhase.Preparation)
                return "Sprawdź prywatną rolę i Alibi, następnie zakończ Przygotowanie.";
            if (view.Phase == RoundPhase.Finished)
                return "Sprawdź wynik i pełne ujawnienie, potem wróć do lobby.";

            switch (plan.Scenario)
            {
                case RoundDeveloperScenario.PersonalMatter:
                case RoundDeveloperScenario.SecretObjective:
                    return DescribeObjective(view.PrivateObjective);
                case RoundDeveloperScenario.GuiltyEscape:
                    return DescribeGuilty(view);
                case RoundDeveloperScenario.DetectiveIncidents:
                    return "Podejdź do Archive Alarm albo podłóż przedmiot w Target Locker. Hałaśliwy Incydent pojawi się od razu; Cichy odkryj ponownie przy zmienionym obiekcie.";
                default:
                    return string.Empty;
            }
        }

        private static string DescribeObjective(PrivateObjectiveView objective)
        {
            if (objective == null)
                return "Brak Prywatnego Celu dla wybranego widoku.";
            if (objective.IsCompleted)
                return "Prywatny Cel ukończony. Użyj symulowanego zakończenia, aby sprawdzić wynik.";
            if (!objective.CurrentStep.HasValue)
                return "Oczekiwanie na następny krok Celu.";

            switch (objective.CurrentStep.Value.Value)
            {
                case "osobista-sprawa-przygotuj":
                    return "Krok 1: przeszukaj Records Cabinet albo Evidence Shelf.";
                case "osobista-sprawa-zakoncz":
                    return "Krok 2: ukryj dokument w Locker albo Archive Slot.";
                case "wrobienie-przygotuj":
                    return "Krok 1 Wrobienia: zabierz przedmiot z Evidence Tray.";
                case "wrobienie-podloz":
                    return "Krok 2 Wrobienia: podłóż przedmiot w Target Locker.";
                default:
                    return $"Wykonaj fizyczny krok: {objective.CurrentStep.Value.Value}.";
            }
        }

        private static string DescribeGuilty(PlayerRoundView view)
        {
            string clue = view.AcquiredAlibiClues == null || view.AcquiredAlibiClues.Count == 0
                ? "Opcjonalnie przeszukaj Crumpled Receipt, aby zdobyć Trop. "
                : "Trop zdobyty. ";
            var plan = view.EscapePlan;
            if (plan == null)
                return clue + "Brak Planu Ucieczki w prywatnym widoku.";
            if (plan.CurrentStep.HasValue)
            {
                switch (plan.CurrentStep.Value.Value)
                {
                    case "escape-find-tool":
                        return clue + "Plan: przeszukaj Maintenance Cabinet.";
                    case "escape-open-route":
                        return clue + "Plan: sprawdź Service Panel.";
                }
            }

            if (!plan.ExitOptions.Any(option => option.IsPrepared))
                return clue + "Przygotuj Vent Control albo Loading Gate Control, następnie użyj odpowiadającego wyjścia.";
            if (plan.ActiveExit.HasValue)
                return clue + $"Trwa finał Ucieczki przy {plan.ActiveExit.Value.Value}.";
            return clue + "Użyj przygotowanego Service Vent albo Loading Gate Exit.";
        }

        private static string ScenarioLabel(RoundDeveloperScenario scenario)
        {
            switch (scenario)
            {
                case RoundDeveloperScenario.PersonalMatter: return "NIEWINNY — OSOBISTA SPRAWA";
                case RoundDeveloperScenario.SecretObjective: return "NIEWINNY — SEKRETNY CEL";
                case RoundDeveloperScenario.GuiltyEscape: return "WINNY — TROP I UCIECZKA";
                case RoundDeveloperScenario.DetectiveIncidents: return "DETEKTYW — INCYDENTY";
                default: return scenario.ToString();
            }
        }

        private static string RoleLabel(RoundRole role)
        {
            switch (role)
            {
                case RoundRole.Detective: return "Detektyw";
                case RoundRole.Guilty: return "Winny";
                case RoundRole.Innocent: return "Niewinny";
                default: return role.ToString();
            }
        }

        private void InitializeStyles()
        {
            if (_boxStyle != null)
                return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 34f,
                wordWrap = true
            };
        }

        private void HandleToggleKey()
        {
            var current = Event.current;
            if (current.type != EventType.KeyDown || current.keyCode != KeyCode.F8)
                return;

            _expanded = !_expanded;
            current.Use();
        }
    }
}
