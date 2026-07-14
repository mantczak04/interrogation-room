using System;
using System.Linq;
using InterrogationRoom.Domain;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Networking
{
    public enum RoundIntentKind : byte
    {
        StartRound,
        EndPreparation,
        ReturnToLobby,
        AdvancePrivateObjective,
        RegisterIncident,
        DiscoverQuietIncident,
        AcquireAlibiClue,
        PrepareEscape,
        BeginEscape,
        InterruptEscape,
        CompleteEscape
    }

    /// <summary>
    /// Client intention. Sender identity is deliberately absent: the server
    /// derives it from the NetworkConnectionToClient that delivered the message.
    /// </summary>
    public struct RoundIntentMessage : NetworkMessage
    {
        public RoundIntentKind Kind;
        public string ObjectiveId;
        public string ObjectiveStepId;
        public bool HasObjectiveStepReference;
        public string IncidentId;
        public IncidentKind IncidentKind;
        public string EffectId;
        public string LocationId;
        public string AlibiClueId;
        public string EscapePlanId;
        public string EscapeStepId;
        public string EscapeExitId;

        public static RoundIntentMessage StartRound() =>
            Registered(new RoundIntentMessage { Kind = RoundIntentKind.StartRound });

        public static RoundIntentMessage EndPreparation() =>
            Registered(new RoundIntentMessage { Kind = RoundIntentKind.EndPreparation });

        public static RoundIntentMessage ReturnToLobby() =>
            Registered(new RoundIntentMessage { Kind = RoundIntentKind.ReturnToLobby });

        public static RoundIntentMessage AdvancePrivateObjective(
            PrivateObjectiveId objectiveId,
            PrivateObjectiveStepId stepId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.AdvancePrivateObjective,
                ObjectiveId = objectiveId.Value,
                ObjectiveStepId = stepId.Value
            });

        public static RoundIntentMessage RegisterIncident(
            IncidentId incidentId,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location,
            PrivateObjectiveStepReference objectiveStep = null) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.RegisterIncident,
                IncidentId = incidentId.Value,
                IncidentKind = kind,
                EffectId = effect.Value,
                LocationId = location.Value,
                HasObjectiveStepReference = objectiveStep != null,
                ObjectiveId = objectiveStep?.ObjectiveId.Value,
                ObjectiveStepId = objectiveStep?.StepId.Value
            });

        public static RoundIntentMessage DiscoverQuietIncident(IncidentId incidentId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.DiscoverQuietIncident,
                IncidentId = incidentId.Value
            });

        public static RoundIntentMessage AcquireAlibiClue(
            AlibiClueId clueId,
            IncidentId incidentId,
            IncidentKind incidentKind,
            IncidentEffectId effect,
            IncidentLocationId location) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.AcquireAlibiClue,
                AlibiClueId = clueId.Value,
                IncidentId = incidentId.Value,
                IncidentKind = incidentKind,
                EffectId = effect.Value,
                LocationId = location.Value
            });

        public static RoundIntentMessage PrepareEscape(EscapePlanId planId, EscapeStepId stepId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.PrepareEscape,
                EscapePlanId = planId.Value,
                EscapeStepId = stepId.Value
            });

        public static RoundIntentMessage BeginEscape(
            EscapePlanId planId,
            EscapeExitId exitId,
            IncidentId incidentId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.BeginEscape,
                EscapePlanId = planId.Value,
                EscapeExitId = exitId.Value,
                IncidentId = incidentId.Value
            });

        public static RoundIntentMessage InterruptEscape(EscapePlanId planId, EscapeExitId exitId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.InterruptEscape,
                EscapePlanId = planId.Value,
                EscapeExitId = exitId.Value
            });

        public static RoundIntentMessage CompleteEscape(EscapePlanId planId, EscapeExitId exitId) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.CompleteEscape,
                EscapePlanId = planId.Value,
                EscapeExitId = exitId.Value
            });

        private static RoundIntentMessage Registered(RoundIntentMessage message)
        {
            RoundMessageSerialization.Register();
            return message;
        }
    }

    /// <summary>A rejection returned only to the connection that sent an invalid intention.</summary>
    public struct RoundIntentRejectedMessage : NetworkMessage
    {
        public string Reason;
    }

    /// <summary>Clears a client's completed private view after the host returns to lobby.</summary>
    public struct RoundLobbyResetMessage : NetworkMessage
    {
    }

    /// <summary>Public lobby state. Contains no role, Alibi, objective, or Incident data.</summary>
    public struct RoundLobbyStateMessage : NetworkMessage
    {
        public int PlayerCount;
    }

    public struct AlibiEntryMessage
    {
        public string FactId;
        public bool IsHidden;
        public string Text;
    }

    /// <summary>
    /// Mirror-friendly wire representation of exactly one PlayerRoundView.
    /// Every field is sourced from RoundEngine.ViewFor(recipient), then sent
    /// directly to that recipient. RoundEndsAtNetworkTime is the only
    /// adapter-owned value and is public timer presentation data.
    /// </summary>
    public struct RoundViewMessage : NetworkMessage
    {
        public int ViewerId;
        public RoundPhase Phase;
        public RoundRole Role;
        public int[] PlayerIds;
        public int DetectiveId;
        public string CrimeDescription;

        public bool HasAlibi;
        public AlibiEntryMessage[] AlibiEntries;

        public bool HasSecretObjective;
        public int SecretObjectiveTargetId;

        public bool HasPrivateObjective;
        public PrivateObjectiveMessage PrivateObjective;
        public bool HasIncidentRegistry;
        public IncidentRegistryEntryMessage[] IncidentRegistry;
        public bool HasRevealedIncidents;
        public IncidentRevealMessage[] RevealedIncidents;
        public bool HasAcquiredAlibiClues;
        public AlibiClueMessage[] AcquiredAlibiClues;
        public bool HasEscapePlan;
        public EscapePlanMessage EscapePlan;
        public bool HasRoundReveal;
        public RoundRevealMessage RoundReveal;

        public bool HasResult;
        public bool Won;
        public bool Survived;
        public bool DetectiveWon;
        public RoundEndCause EndCause;
        public bool HasExecutedPlayer;
        public int ExecutedPlayerId;
        public bool PrivateObjectiveCompleted;
        public bool Escaped;

        public double RoundEndsAtNetworkTime;

        public static RoundViewMessage FromView(PlayerRoundView view, double roundEndsAtNetworkTime)
        {
            RoundMessageSerialization.Register();
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var message = new RoundViewMessage
            {
                ViewerId = view.Viewer.Value,
                Phase = view.Phase,
                Role = view.Role,
                PlayerIds = CopyPlayerIds(view.Players),
                DetectiveId = view.Detective.Value,
                CrimeDescription = view.CrimeDescription,
                HasAlibi = view.Alibi != null,
                AlibiEntries = view.Alibi == null
                    ? Array.Empty<AlibiEntryMessage>()
                    : CopyAlibi(view.Alibi),
                HasSecretObjective = view.SecretObjective != null,
                SecretObjectiveTargetId = view.SecretObjective?.Target.Value ?? 0,
                HasPrivateObjective = view.PrivateObjective != null,
                PrivateObjective = view.PrivateObjective == null
                    ? default
                    : PrivateObjectiveMessage.FromView(view.PrivateObjective),
                HasIncidentRegistry = view.IncidentRegistry != null,
                IncidentRegistry = view.IncidentRegistry == null
                    ? Array.Empty<IncidentRegistryEntryMessage>()
                    : view.IncidentRegistry.Select(IncidentRegistryEntryMessage.FromView).ToArray(),
                HasRevealedIncidents = view.RevealedIncidents != null,
                RevealedIncidents = view.RevealedIncidents == null
                    ? Array.Empty<IncidentRevealMessage>()
                    : view.RevealedIncidents.Select(IncidentRevealMessage.FromView).ToArray(),
                HasAcquiredAlibiClues = view.AcquiredAlibiClues != null,
                AcquiredAlibiClues = view.AcquiredAlibiClues == null
                    ? Array.Empty<AlibiClueMessage>()
                    : view.AcquiredAlibiClues.Select(AlibiClueMessage.FromView).ToArray(),
                HasEscapePlan = view.EscapePlan != null,
                EscapePlan = view.EscapePlan == null
                    ? default
                    : EscapePlanMessage.FromView(view.EscapePlan),
                HasRoundReveal = view.RoundReveal != null,
                RoundReveal = view.RoundReveal == null
                    ? default
                    : RoundRevealMessage.FromView(view.RoundReveal),
                HasResult = view.Result != null,
                RoundEndsAtNetworkTime = Math.Max(0d, roundEndsAtNetworkTime)
            };

            if (view.Result != null)
            {
                message.Won = view.Result.Won;
                message.Survived = view.Result.Survived;
                message.DetectiveWon = view.Result.DetectiveWon;
                message.EndCause = view.Result.EndCause;
                message.HasExecutedPlayer = view.Result.ExecutedPlayer.HasValue;
                message.ExecutedPlayerId = view.Result.ExecutedPlayer?.Value ?? 0;
                message.PrivateObjectiveCompleted = view.Result.PrivateObjectiveCompleted;
                message.Escaped = view.Result.Escaped;
            }

            return message;
        }

        public PlayerRoundView ToView()
        {
            AlibiView alibi = null;
            if (HasAlibi)
            {
                var source = AlibiEntries ?? Array.Empty<AlibiEntryMessage>();
                var entries = new AlibiEntry[source.Length];
                for (var index = 0; index < source.Length; index++)
                {
                    var entry = source[index];
                    entries[index] = new AlibiEntry(entry.FactId, entry.IsHidden, entry.Text);
                }

                alibi = new AlibiView(entries);
            }

            var privateObjective = HasPrivateObjective
                ? PrivateObjective.ToView()
                : null;
            var secretObjective = privateObjective == null && HasSecretObjective
                ? new SecretObjectiveView(new PlayerId(SecretObjectiveTargetId))
                : null;

            PlayerResultView result = null;
            if (HasResult)
            {
                PlayerId? executedPlayer = HasExecutedPlayer
                    ? new PlayerId(ExecutedPlayerId)
                    : (PlayerId?)null;
                result = new PlayerResultView(
                    Won,
                    Survived,
                    DetectiveWon,
                    EndCause,
                    executedPlayer,
                    PrivateObjectiveCompleted,
                    Escaped);
            }

            if (privateObjective == null && secretObjective != null)
            {
                return new PlayerRoundView(
                    new PlayerId(ViewerId),
                    Phase,
                    Role,
                    CrimeDescription,
                    alibi,
                    secretObjective,
                    result,
                    CopyPlayers(PlayerIds),
                    new PlayerId(DetectiveId),
                    HasIncidentRegistry ? IncidentRegistry.Select(value => value.ToView()).ToArray() : null,
                    HasRevealedIncidents ? RevealedIncidents.Select(value => value.ToView()).ToArray() : null,
                    HasAcquiredAlibiClues ? AcquiredAlibiClues.Select(value => value.ToView()).ToArray() : null,
                    HasEscapePlan ? EscapePlan.ToView() : null,
                    HasRoundReveal ? RoundReveal.ToView() : null);
            }

            return new PlayerRoundView(
                new PlayerId(ViewerId),
                Phase,
                Role,
                CrimeDescription,
                alibi,
                privateObjective,
                result,
                CopyPlayers(PlayerIds),
                new PlayerId(DetectiveId),
                HasIncidentRegistry ? IncidentRegistry.Select(value => value.ToView()).ToArray() : null,
                HasRevealedIncidents ? RevealedIncidents.Select(value => value.ToView()).ToArray() : null,
                HasAcquiredAlibiClues ? AcquiredAlibiClues.Select(value => value.ToView()).ToArray() : null,
                HasEscapePlan ? EscapePlan.ToView() : null,
                HasRoundReveal ? RoundReveal.ToView() : null);
        }

        private static int[] CopyPlayerIds(System.Collections.Generic.IReadOnlyList<PlayerId> players)
        {
            var ids = new int[players.Count];
            for (var index = 0; index < players.Count; index++)
                ids[index] = players[index].Value;
            return ids;
        }

        private static PlayerId[] CopyPlayers(int[] playerIds)
        {
            var source = playerIds ?? Array.Empty<int>();
            var players = new PlayerId[source.Length];
            for (var index = 0; index < source.Length; index++)
                players[index] = new PlayerId(source[index]);
            return players;
        }

        private static AlibiEntryMessage[] CopyAlibi(AlibiView alibi)
        {
            var entries = new AlibiEntryMessage[alibi.Entries.Count];
            for (var index = 0; index < alibi.Entries.Count; index++)
            {
                var entry = alibi.Entries[index];
                entries[index] = new AlibiEntryMessage
                {
                    FactId = entry.FactId,
                    IsHidden = entry.IsHidden,
                    Text = entry.Text
                };
            }

            return entries;
        }
    }

    /// <summary>
    /// Explicit serializers keep the wire contract deterministic even when an
    /// assembly reload does not make Mirror's generated readers available.
    /// </summary>
    public static class RoundMessageSerialization
    {
        private const int MaxAlibiEntries = 256;
        private const int MaxPlayers = RoundEngine.MaxPlayers;
        private const int MaxIncidents = 256;
        private const int MaxAlibiClues = 256;
        private const int MaxEscapeOptions = 32;
        private const int MaxEscapeActions = 256;
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBeforeSceneLoad()
        {
            Register();
        }

        public static void Register()
        {
            if (_registered
                && Writer<RoundIntentMessage>.write != null
                && Reader<RoundIntentMessage>.read != null
                && Writer<RoundViewMessage>.write != null
                && Reader<RoundViewMessage>.read != null
                && Writer<RoundIntentRejectedMessage>.write != null
                && Reader<RoundIntentRejectedMessage>.read != null
                && Writer<RoundLobbyResetMessage>.write != null
                && Reader<RoundLobbyResetMessage>.read != null
                && Writer<RoundLobbyStateMessage>.write != null
                && Reader<RoundLobbyStateMessage>.read != null)
                return;

            Writer<RoundIntentMessage>.write = WriteRoundIntent;
            Reader<RoundIntentMessage>.read = ReadRoundIntent;
            Writer<RoundViewMessage>.write = WriteRoundView;
            Reader<RoundViewMessage>.read = ReadRoundView;
            Writer<RoundIntentRejectedMessage>.write = WriteRoundIntentRejected;
            Reader<RoundIntentRejectedMessage>.read = ReadRoundIntentRejected;
            Writer<RoundLobbyResetMessage>.write = WriteRoundLobbyReset;
            Reader<RoundLobbyResetMessage>.read = ReadRoundLobbyReset;
            Writer<RoundLobbyStateMessage>.write = WriteRoundLobbyState;
            Reader<RoundLobbyStateMessage>.read = ReadRoundLobbyState;
            _registered = true;
        }

        public static void WriteRoundIntent(this NetworkWriter writer, RoundIntentMessage message)
        {
            writer.WriteByte((byte)message.Kind);
            switch (message.Kind)
            {
                case RoundIntentKind.StartRound:
                case RoundIntentKind.EndPreparation:
                case RoundIntentKind.ReturnToLobby:
                    break;

                case RoundIntentKind.AdvancePrivateObjective:
                    writer.WriteString(message.ObjectiveId);
                    writer.WriteString(message.ObjectiveStepId);
                    break;

                case RoundIntentKind.RegisterIncident:
                    writer.WriteString(message.IncidentId);
                    writer.WriteByte((byte)message.IncidentKind);
                    writer.WriteString(message.EffectId);
                    writer.WriteString(message.LocationId);
                    writer.WriteBool(message.HasObjectiveStepReference);
                    if (message.HasObjectiveStepReference)
                    {
                        writer.WriteString(message.ObjectiveId);
                        writer.WriteString(message.ObjectiveStepId);
                    }
                    break;

                case RoundIntentKind.DiscoverQuietIncident:
                    writer.WriteString(message.IncidentId);
                    break;

                case RoundIntentKind.AcquireAlibiClue:
                    writer.WriteString(message.AlibiClueId);
                    writer.WriteString(message.IncidentId);
                    writer.WriteByte((byte)message.IncidentKind);
                    writer.WriteString(message.EffectId);
                    writer.WriteString(message.LocationId);
                    break;

                case RoundIntentKind.PrepareEscape:
                    writer.WriteString(message.EscapePlanId);
                    writer.WriteString(message.EscapeStepId);
                    break;

                case RoundIntentKind.BeginEscape:
                    writer.WriteString(message.EscapePlanId);
                    writer.WriteString(message.EscapeExitId);
                    writer.WriteString(message.IncidentId);
                    break;

                case RoundIntentKind.InterruptEscape:
                case RoundIntentKind.CompleteEscape:
                    writer.WriteString(message.EscapePlanId);
                    writer.WriteString(message.EscapeExitId);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message.Kind, "Unknown Runda intention.");
            }
        }

        public static RoundIntentMessage ReadRoundIntent(this NetworkReader reader)
        {
            var message = new RoundIntentMessage
            {
                Kind = (RoundIntentKind)reader.ReadByte()
            };

            switch (message.Kind)
            {
                case RoundIntentKind.StartRound:
                case RoundIntentKind.EndPreparation:
                case RoundIntentKind.ReturnToLobby:
                    break;

                case RoundIntentKind.AdvancePrivateObjective:
                    message.ObjectiveId = reader.ReadString();
                    message.ObjectiveStepId = reader.ReadString();
                    break;

                case RoundIntentKind.RegisterIncident:
                    message.IncidentId = reader.ReadString();
                    message.IncidentKind = (IncidentKind)reader.ReadByte();
                    message.EffectId = reader.ReadString();
                    message.LocationId = reader.ReadString();
                    message.HasObjectiveStepReference = reader.ReadBool();
                    if (message.HasObjectiveStepReference)
                    {
                        message.ObjectiveId = reader.ReadString();
                        message.ObjectiveStepId = reader.ReadString();
                    }
                    break;

                case RoundIntentKind.DiscoverQuietIncident:
                    message.IncidentId = reader.ReadString();
                    break;

                case RoundIntentKind.AcquireAlibiClue:
                    message.AlibiClueId = reader.ReadString();
                    message.IncidentId = reader.ReadString();
                    message.IncidentKind = (IncidentKind)reader.ReadByte();
                    message.EffectId = reader.ReadString();
                    message.LocationId = reader.ReadString();
                    break;

                case RoundIntentKind.PrepareEscape:
                    message.EscapePlanId = reader.ReadString();
                    message.EscapeStepId = reader.ReadString();
                    break;

                case RoundIntentKind.BeginEscape:
                    message.EscapePlanId = reader.ReadString();
                    message.EscapeExitId = reader.ReadString();
                    message.IncidentId = reader.ReadString();
                    break;

                case RoundIntentKind.InterruptEscape:
                case RoundIntentKind.CompleteEscape:
                    message.EscapePlanId = reader.ReadString();
                    message.EscapeExitId = reader.ReadString();
                    break;

                default:
                    throw new FormatException($"Received unknown Runda intention {(byte)message.Kind}.");
            }

            return message;
        }

        public static void WriteRoundIntentRejected(this NetworkWriter writer, RoundIntentRejectedMessage message)
        {
            writer.WriteString(message.Reason);
        }

        public static RoundIntentRejectedMessage ReadRoundIntentRejected(this NetworkReader reader) =>
            new RoundIntentRejectedMessage { Reason = reader.ReadString() };

        public static void WriteRoundLobbyReset(this NetworkWriter writer, RoundLobbyResetMessage message)
        {
        }

        public static RoundLobbyResetMessage ReadRoundLobbyReset(this NetworkReader reader) =>
            new RoundLobbyResetMessage();

        public static void WriteRoundLobbyState(this NetworkWriter writer, RoundLobbyStateMessage message)
        {
            writer.WriteInt(message.PlayerCount);
        }

        public static RoundLobbyStateMessage ReadRoundLobbyState(this NetworkReader reader) =>
            new RoundLobbyStateMessage { PlayerCount = reader.ReadInt() };

        public static void WriteRoundView(this NetworkWriter writer, RoundViewMessage message)
        {
            writer.WriteInt(message.ViewerId);
            writer.WriteByte((byte)message.Phase);
            writer.WriteByte((byte)message.Role);
            var playerIds = message.PlayerIds ?? Array.Empty<int>();
            if (playerIds.Length > MaxPlayers)
                throw new ArgumentOutOfRangeException(nameof(message), $"Skład Rundy cannot contain more than {MaxPlayers} players.");
            writer.WriteByte((byte)playerIds.Length);
            foreach (var playerId in playerIds)
                writer.WriteInt(playerId);
            writer.WriteInt(message.DetectiveId);
            writer.WriteString(message.CrimeDescription);

            writer.WriteBool(message.HasAlibi);
            if (message.HasAlibi)
            {
                var entries = message.AlibiEntries ?? Array.Empty<AlibiEntryMessage>();
                if (entries.Length > MaxAlibiEntries)
                    throw new ArgumentOutOfRangeException(nameof(message), $"Alibi cannot contain more than {MaxAlibiEntries} entries.");

                writer.WriteUShort((ushort)entries.Length);
                foreach (var entry in entries)
                {
                    writer.WriteString(entry.FactId);
                    writer.WriteBool(entry.IsHidden);
                    writer.WriteString(entry.Text);
                }
            }

            writer.WriteBool(message.HasSecretObjective);
            if (message.HasSecretObjective)
                writer.WriteInt(message.SecretObjectiveTargetId);

            writer.WriteBool(message.HasPrivateObjective);
            if (message.HasPrivateObjective)
                WritePrivateObjective(writer, message.PrivateObjective);

            WriteIncidentRegistry(writer, message.HasIncidentRegistry, message.IncidentRegistry);
            WriteIncidentReveals(writer, message.HasRevealedIncidents, message.RevealedIncidents);
            WriteAlibiClues(writer, message.HasAcquiredAlibiClues, message.AcquiredAlibiClues);

            writer.WriteBool(message.HasEscapePlan);
            if (message.HasEscapePlan)
                WriteEscapePlan(writer, message.EscapePlan);

            writer.WriteBool(message.HasRoundReveal);
            if (message.HasRoundReveal)
                WriteRoundReveal(writer, message.RoundReveal);

            writer.WriteBool(message.HasResult);
            if (message.HasResult)
            {
                writer.WriteBool(message.Won);
                writer.WriteBool(message.Survived);
                writer.WriteBool(message.DetectiveWon);
                writer.WriteByte((byte)message.EndCause);
                writer.WriteBool(message.HasExecutedPlayer);
                if (message.HasExecutedPlayer)
                    writer.WriteInt(message.ExecutedPlayerId);
                writer.WriteBool(message.PrivateObjectiveCompleted);
                writer.WriteBool(message.Escaped);
            }

            writer.WriteDouble(message.RoundEndsAtNetworkTime);
        }

        public static RoundViewMessage ReadRoundView(this NetworkReader reader)
        {
            var message = new RoundViewMessage
            {
                ViewerId = reader.ReadInt(),
                Phase = (RoundPhase)reader.ReadByte(),
                Role = (RoundRole)reader.ReadByte(),
                PlayerIds = Array.Empty<int>()
            };

            var playerCount = reader.ReadByte();
            if (playerCount > MaxPlayers)
                throw new FormatException($"Received Skład Rundy count {playerCount}, maximum is {MaxPlayers}.");
            message.PlayerIds = new int[playerCount];
            for (var index = 0; index < playerCount; index++)
                message.PlayerIds[index] = reader.ReadInt();
            message.DetectiveId = reader.ReadInt();
            message.CrimeDescription = reader.ReadString();
            message.HasAlibi = reader.ReadBool();
            message.AlibiEntries = Array.Empty<AlibiEntryMessage>();

            if (message.HasAlibi)
            {
                var count = reader.ReadUShort();
                if (count > MaxAlibiEntries)
                    throw new FormatException($"Received Alibi entry count {count}, maximum is {MaxAlibiEntries}.");

                message.AlibiEntries = new AlibiEntryMessage[count];
                for (var index = 0; index < count; index++)
                {
                    message.AlibiEntries[index] = new AlibiEntryMessage
                    {
                        FactId = reader.ReadString(),
                        IsHidden = reader.ReadBool(),
                        Text = reader.ReadString()
                    };
                }
            }

            message.HasSecretObjective = reader.ReadBool();
            if (message.HasSecretObjective)
                message.SecretObjectiveTargetId = reader.ReadInt();

            message.HasPrivateObjective = reader.ReadBool();
            if (message.HasPrivateObjective)
                message.PrivateObjective = ReadPrivateObjective(reader);

            ReadIncidentRegistry(reader, ref message);
            ReadIncidentReveals(reader, ref message);
            ReadAlibiClues(reader, ref message);

            message.HasEscapePlan = reader.ReadBool();
            if (message.HasEscapePlan)
                message.EscapePlan = ReadEscapePlan(reader);

            message.HasRoundReveal = reader.ReadBool();
            if (message.HasRoundReveal)
                message.RoundReveal = ReadRoundReveal(reader);

            message.HasResult = reader.ReadBool();
            if (message.HasResult)
            {
                message.Won = reader.ReadBool();
                message.Survived = reader.ReadBool();
                message.DetectiveWon = reader.ReadBool();
                message.EndCause = (RoundEndCause)reader.ReadByte();
                message.HasExecutedPlayer = reader.ReadBool();
                if (message.HasExecutedPlayer)
                    message.ExecutedPlayerId = reader.ReadInt();
                message.PrivateObjectiveCompleted = reader.ReadBool();
                message.Escaped = reader.ReadBool();
            }

            message.RoundEndsAtNetworkTime = reader.ReadDouble();
            return message;
        }

        private static void WritePrivateObjective(NetworkWriter writer, PrivateObjectiveMessage message)
        {
            writer.WriteString(message.Id);
            writer.WriteByte((byte)message.Kind);
            writer.WriteBool(message.HasCurrentStep);
            if (message.HasCurrentStep)
                writer.WriteString(message.CurrentStepId);
            writer.WriteInt(message.CompletedStepCount);
            writer.WriteInt(message.TotalStepCount);
            writer.WriteBool(message.IsCompleted);
            writer.WriteBool(message.HasTarget);
            if (message.HasTarget)
                writer.WriteInt(message.TargetPlayerId);
        }

        private static PrivateObjectiveMessage ReadPrivateObjective(NetworkReader reader)
        {
            var message = new PrivateObjectiveMessage
            {
                Id = reader.ReadString(),
                Kind = (PrivateObjectiveKind)reader.ReadByte(),
                HasCurrentStep = reader.ReadBool()
            };
            if (message.HasCurrentStep)
                message.CurrentStepId = reader.ReadString();
            message.CompletedStepCount = reader.ReadInt();
            message.TotalStepCount = reader.ReadInt();
            message.IsCompleted = reader.ReadBool();
            message.HasTarget = reader.ReadBool();
            if (message.HasTarget)
                message.TargetPlayerId = reader.ReadInt();
            return message;
        }

        private static void WriteIncidentRegistry(
            NetworkWriter writer,
            bool hasRegistry,
            IncidentRegistryEntryMessage[] values)
        {
            writer.WriteBool(hasRegistry);
            if (!hasRegistry)
                return;

            var entries = values ?? Array.Empty<IncidentRegistryEntryMessage>();
            WriteCount(writer, entries.Length, MaxIncidents, "Incydent registry");
            foreach (var entry in entries)
            {
                writer.WriteString(entry.Id);
                writer.WriteByte((byte)entry.Kind);
                writer.WriteString(entry.EffectId);
                writer.WriteString(entry.LocationId);
                writer.WriteLong(entry.ReportedAtMilliseconds);
            }
        }

        private static void ReadIncidentRegistry(NetworkReader reader, ref RoundViewMessage message)
        {
            message.HasIncidentRegistry = reader.ReadBool();
            message.IncidentRegistry = Array.Empty<IncidentRegistryEntryMessage>();
            if (!message.HasIncidentRegistry)
                return;

            int count = ReadCount(reader, MaxIncidents, "Incydent registry");
            message.IncidentRegistry = new IncidentRegistryEntryMessage[count];
            for (var index = 0; index < count; index++)
            {
                message.IncidentRegistry[index] = new IncidentRegistryEntryMessage
                {
                    Id = reader.ReadString(),
                    Kind = (IncidentKind)reader.ReadByte(),
                    EffectId = reader.ReadString(),
                    LocationId = reader.ReadString(),
                    ReportedAtMilliseconds = reader.ReadLong()
                };
            }
        }

        private static void WriteIncidentReveals(
            NetworkWriter writer,
            bool hasValues,
            IncidentRevealMessage[] values)
        {
            writer.WriteBool(hasValues);
            if (!hasValues)
                return;
            var entries = values ?? Array.Empty<IncidentRevealMessage>();
            WriteCount(writer, entries.Length, MaxIncidents, "revealed Incydents");
            foreach (var entry in entries)
                WriteIncidentReveal(writer, entry);
        }

        private static void ReadIncidentReveals(NetworkReader reader, ref RoundViewMessage message)
        {
            message.HasRevealedIncidents = reader.ReadBool();
            message.RevealedIncidents = Array.Empty<IncidentRevealMessage>();
            if (!message.HasRevealedIncidents)
                return;
            int count = ReadCount(reader, MaxIncidents, "revealed Incydents");
            message.RevealedIncidents = new IncidentRevealMessage[count];
            for (var index = 0; index < count; index++)
                message.RevealedIncidents[index] = ReadIncidentReveal(reader);
        }

        private static void WriteIncidentReveal(NetworkWriter writer, IncidentRevealMessage entry)
        {
            writer.WriteString(entry.Id);
            writer.WriteByte((byte)entry.Kind);
            writer.WriteString(entry.EffectId);
            writer.WriteString(entry.LocationId);
            writer.WriteInt(entry.AuthorPlayerId);
        }

        private static IncidentRevealMessage ReadIncidentReveal(NetworkReader reader) =>
            new IncidentRevealMessage
            {
                Id = reader.ReadString(),
                Kind = (IncidentKind)reader.ReadByte(),
                EffectId = reader.ReadString(),
                LocationId = reader.ReadString(),
                AuthorPlayerId = reader.ReadInt()
            };

        private static void WriteAlibiClues(
            NetworkWriter writer,
            bool hasValues,
            AlibiClueMessage[] values)
        {
            writer.WriteBool(hasValues);
            if (!hasValues)
                return;
            var entries = values ?? Array.Empty<AlibiClueMessage>();
            WriteCount(writer, entries.Length, MaxAlibiClues, "Tropy do Alibi");
            foreach (var entry in entries)
            {
                writer.WriteString(entry.Id);
                writer.WriteString(entry.Content);
            }
        }

        private static void ReadAlibiClues(NetworkReader reader, ref RoundViewMessage message)
        {
            message.HasAcquiredAlibiClues = reader.ReadBool();
            message.AcquiredAlibiClues = Array.Empty<AlibiClueMessage>();
            if (!message.HasAcquiredAlibiClues)
                return;
            int count = ReadCount(reader, MaxAlibiClues, "Tropy do Alibi");
            message.AcquiredAlibiClues = new AlibiClueMessage[count];
            for (var index = 0; index < count; index++)
            {
                message.AcquiredAlibiClues[index] = new AlibiClueMessage
                {
                    Id = reader.ReadString(),
                    Content = reader.ReadString()
                };
            }
        }

        private static void WriteEscapePlan(NetworkWriter writer, EscapePlanMessage message)
        {
            writer.WriteString(message.Id);
            writer.WriteBool(message.HasCurrentStep);
            if (message.HasCurrentStep)
                writer.WriteString(message.CurrentStepId);
            writer.WriteInt(message.CompletedCommonStepCount);
            writer.WriteInt(message.TotalCommonStepCount);
            writer.WriteBool(message.IsPrepared);
            writer.WriteBool(message.HasActiveExit);
            if (message.HasActiveExit)
                writer.WriteString(message.ActiveExitId);

            var options = message.ExitOptions ?? Array.Empty<EscapeExitOptionMessage>();
            WriteCount(writer, options.Length, MaxEscapeOptions, "Escape exit options");
            foreach (var option in options)
            {
                writer.WriteString(option.Id);
                writer.WriteString(option.PreparationStepId);
                writer.WriteString(option.LocationId);
                writer.WriteBool(option.IsPrepared);
            }
        }

        private static EscapePlanMessage ReadEscapePlan(NetworkReader reader)
        {
            var message = new EscapePlanMessage
            {
                Id = reader.ReadString(),
                HasCurrentStep = reader.ReadBool()
            };
            if (message.HasCurrentStep)
                message.CurrentStepId = reader.ReadString();
            message.CompletedCommonStepCount = reader.ReadInt();
            message.TotalCommonStepCount = reader.ReadInt();
            message.IsPrepared = reader.ReadBool();
            message.HasActiveExit = reader.ReadBool();
            if (message.HasActiveExit)
                message.ActiveExitId = reader.ReadString();

            int count = ReadCount(reader, MaxEscapeOptions, "Escape exit options");
            message.ExitOptions = new EscapeExitOptionMessage[count];
            for (var index = 0; index < count; index++)
            {
                message.ExitOptions[index] = new EscapeExitOptionMessage
                {
                    Id = reader.ReadString(),
                    PreparationStepId = reader.ReadString(),
                    LocationId = reader.ReadString(),
                    IsPrepared = reader.ReadBool()
                };
            }
            return message;
        }

        private static void WritePlayerResult(NetworkWriter writer, PlayerResultMessage result)
        {
            writer.WriteBool(result.Won);
            writer.WriteBool(result.Survived);
            writer.WriteBool(result.DetectiveWon);
            writer.WriteByte((byte)result.EndCause);
            writer.WriteBool(result.HasExecutedPlayer);
            if (result.HasExecutedPlayer)
                writer.WriteInt(result.ExecutedPlayerId);
            writer.WriteBool(result.PrivateObjectiveCompleted);
            writer.WriteBool(result.Escaped);
        }

        private static PlayerResultMessage ReadPlayerResult(NetworkReader reader)
        {
            var result = new PlayerResultMessage
            {
                Won = reader.ReadBool(),
                Survived = reader.ReadBool(),
                DetectiveWon = reader.ReadBool(),
                EndCause = (RoundEndCause)reader.ReadByte(),
                HasExecutedPlayer = reader.ReadBool()
            };
            if (result.HasExecutedPlayer)
                result.ExecutedPlayerId = reader.ReadInt();
            result.PrivateObjectiveCompleted = reader.ReadBool();
            result.Escaped = reader.ReadBool();
            return result;
        }

        private static void WriteRoundReveal(NetworkWriter writer, RoundRevealMessage reveal)
        {
            var players = reveal.Players ?? Array.Empty<PlayerEndRevealMessage>();
            WriteCount(writer, players.Length, MaxPlayers, "Runda reveal players");
            foreach (var player in players)
            {
                writer.WriteInt(player.PlayerId);
                writer.WriteByte((byte)player.Role);
                writer.WriteBool(player.HasPrivateObjective);
                if (player.HasPrivateObjective)
                    WritePrivateObjective(writer, player.PrivateObjective);
                WritePlayerResult(writer, player.Result);
            }

            var clues = reveal.AcquiredAlibiClues ?? Array.Empty<AlibiClueRevealMessage>();
            WriteCount(writer, clues.Length, MaxAlibiClues, "revealed Tropy do Alibi");
            foreach (var clue in clues)
            {
                writer.WriteString(clue.Id);
                writer.WriteString(clue.LinkedFactId);
                writer.WriteString(clue.Content);
            }

            WriteEscapePlanReveal(writer, reveal.EscapePlan);

            var incidents = reveal.Incidents ?? Array.Empty<IncidentRevealMessage>();
            WriteCount(writer, incidents.Length, MaxIncidents, "Runda reveal Incydents");
            foreach (var incident in incidents)
                WriteIncidentReveal(writer, incident);
        }

        private static RoundRevealMessage ReadRoundReveal(NetworkReader reader)
        {
            var reveal = new RoundRevealMessage();
            int playerCount = ReadCount(reader, MaxPlayers, "Runda reveal players");
            reveal.Players = new PlayerEndRevealMessage[playerCount];
            for (var index = 0; index < playerCount; index++)
            {
                var player = new PlayerEndRevealMessage
                {
                    PlayerId = reader.ReadInt(),
                    Role = (RoundRole)reader.ReadByte(),
                    HasPrivateObjective = reader.ReadBool()
                };
                if (player.HasPrivateObjective)
                    player.PrivateObjective = ReadPrivateObjective(reader);
                player.Result = ReadPlayerResult(reader);
                reveal.Players[index] = player;
            }

            int clueCount = ReadCount(reader, MaxAlibiClues, "revealed Tropy do Alibi");
            reveal.AcquiredAlibiClues = new AlibiClueRevealMessage[clueCount];
            for (var index = 0; index < clueCount; index++)
            {
                reveal.AcquiredAlibiClues[index] = new AlibiClueRevealMessage
                {
                    Id = reader.ReadString(),
                    LinkedFactId = reader.ReadString(),
                    Content = reader.ReadString()
                };
            }

            reveal.EscapePlan = ReadEscapePlanReveal(reader);

            int incidentCount = ReadCount(reader, MaxIncidents, "Runda reveal Incydents");
            reveal.Incidents = new IncidentRevealMessage[incidentCount];
            for (var index = 0; index < incidentCount; index++)
                reveal.Incidents[index] = ReadIncidentReveal(reader);
            return reveal;
        }

        private static void WriteEscapePlanReveal(NetworkWriter writer, EscapePlanRevealMessage reveal)
        {
            writer.WriteString(reveal.Id);
            var actions = reveal.Actions ?? Array.Empty<EscapeActionRevealMessage>();
            WriteCount(writer, actions.Length, MaxEscapeActions, "Escape reveal actions");
            foreach (var action in actions)
            {
                writer.WriteByte((byte)action.Kind);
                writer.WriteBool(action.HasStep);
                if (action.HasStep)
                    writer.WriteString(action.StepId);
                writer.WriteBool(action.HasExit);
                if (action.HasExit)
                    writer.WriteString(action.ExitId);
            }
            writer.WriteBool(reveal.HasSuccessfulExit);
            if (reveal.HasSuccessfulExit)
                writer.WriteString(reveal.SuccessfulExitId);
        }

        private static EscapePlanRevealMessage ReadEscapePlanReveal(NetworkReader reader)
        {
            var reveal = new EscapePlanRevealMessage { Id = reader.ReadString() };
            int count = ReadCount(reader, MaxEscapeActions, "Escape reveal actions");
            reveal.Actions = new EscapeActionRevealMessage[count];
            for (var index = 0; index < count; index++)
            {
                var action = new EscapeActionRevealMessage
                {
                    Kind = (EscapeActionKind)reader.ReadByte(),
                    HasStep = reader.ReadBool()
                };
                if (action.HasStep)
                    action.StepId = reader.ReadString();
                action.HasExit = reader.ReadBool();
                if (action.HasExit)
                    action.ExitId = reader.ReadString();
                reveal.Actions[index] = action;
            }
            reveal.HasSuccessfulExit = reader.ReadBool();
            if (reveal.HasSuccessfulExit)
                reveal.SuccessfulExitId = reader.ReadString();
            return reveal;
        }

        private static void WriteCount(NetworkWriter writer, int count, int maximum, string label)
        {
            if (count < 0 || count > maximum)
                throw new ArgumentOutOfRangeException(label, $"{label} count {count} exceeds maximum {maximum}.");
            writer.WriteUShort((ushort)count);
        }

        private static int ReadCount(NetworkReader reader, int maximum, string label)
        {
            int count = reader.ReadUShort();
            if (count > maximum)
                throw new FormatException($"Received {label} count {count}, maximum is {maximum}.");
            return count;
        }
    }
}
