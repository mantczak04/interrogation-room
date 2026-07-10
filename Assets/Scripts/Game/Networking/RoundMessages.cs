using System;
using InterrogationRoom.Domain;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Networking
{
    public enum RoundIntentKind : byte
    {
        StartRound,
        EndPreparation,
        Execute
    }

    /// <summary>
    /// Client intention. Sender identity is deliberately absent: the server
    /// derives it from the NetworkConnectionToClient that delivered the message.
    /// </summary>
    public struct RoundIntentMessage : NetworkMessage
    {
        public RoundIntentKind Kind;
        public int TargetPlayerId;

        public static RoundIntentMessage StartRound() =>
            Registered(new RoundIntentMessage { Kind = RoundIntentKind.StartRound });

        public static RoundIntentMessage EndPreparation() =>
            Registered(new RoundIntentMessage { Kind = RoundIntentKind.EndPreparation });

        public static RoundIntentMessage Execute(PlayerId target) =>
            Registered(new RoundIntentMessage
            {
                Kind = RoundIntentKind.Execute,
                TargetPlayerId = target.Value
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

    public struct AlibiEntryMessage
    {
        public string FactId;
        public bool IsHidden;
        public string Text;
    }

    /// <summary>
    /// Mirror-friendly wire representation of exactly one PlayerRoundView.
    /// Every field is sourced from RoundEngine.ViewFor(recipient), then sent
    /// directly to that recipient. RemainingSeconds is the only adapter-owned
    /// value and is public timer presentation data.
    /// </summary>
    public struct RoundViewMessage : NetworkMessage
    {
        public int ViewerId;
        public RoundPhase Phase;
        public RoundRole Role;
        public string CrimeDescription;

        public bool HasAlibi;
        public AlibiEntryMessage[] AlibiEntries;

        public bool HasSecretObjective;
        public int SecretObjectiveTargetId;

        public bool HasResult;
        public bool Won;
        public bool Survived;
        public bool DetectiveWon;
        public RoundEndCause EndCause;
        public bool HasExecutedPlayer;
        public int ExecutedPlayerId;

        public float RemainingSeconds;

        public static RoundViewMessage FromView(PlayerRoundView view, float remainingSeconds)
        {
            RoundMessageSerialization.Register();
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var message = new RoundViewMessage
            {
                ViewerId = view.Viewer.Value,
                Phase = view.Phase,
                Role = view.Role,
                CrimeDescription = view.CrimeDescription,
                HasAlibi = view.Alibi != null,
                AlibiEntries = view.Alibi == null
                    ? Array.Empty<AlibiEntryMessage>()
                    : CopyAlibi(view.Alibi),
                HasSecretObjective = view.SecretObjective != null,
                SecretObjectiveTargetId = view.SecretObjective?.Target.Value ?? 0,
                HasResult = view.Result != null,
                RemainingSeconds = Math.Max(0f, remainingSeconds)
            };

            if (view.Result != null)
            {
                message.Won = view.Result.Won;
                message.Survived = view.Result.Survived;
                message.DetectiveWon = view.Result.DetectiveWon;
                message.EndCause = view.Result.EndCause;
                message.HasExecutedPlayer = view.Result.ExecutedPlayer.HasValue;
                message.ExecutedPlayerId = view.Result.ExecutedPlayer?.Value ?? 0;
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

            var secretObjective = HasSecretObjective
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
                    executedPlayer);
            }

            return new PlayerRoundView(
                new PlayerId(ViewerId),
                Phase,
                Role,
                CrimeDescription,
                alibi,
                secretObjective,
                result);
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
                && Reader<RoundIntentRejectedMessage>.read != null)
                return;

            Writer<RoundIntentMessage>.write = WriteRoundIntent;
            Reader<RoundIntentMessage>.read = ReadRoundIntent;
            Writer<RoundViewMessage>.write = WriteRoundView;
            Reader<RoundViewMessage>.read = ReadRoundView;
            Writer<RoundIntentRejectedMessage>.write = WriteRoundIntentRejected;
            Reader<RoundIntentRejectedMessage>.read = ReadRoundIntentRejected;
            _registered = true;
        }

        public static void WriteRoundIntent(this NetworkWriter writer, RoundIntentMessage message)
        {
            writer.WriteByte((byte)message.Kind);
            writer.WriteInt(message.TargetPlayerId);
        }

        public static RoundIntentMessage ReadRoundIntent(this NetworkReader reader) =>
            new RoundIntentMessage
            {
                Kind = (RoundIntentKind)reader.ReadByte(),
                TargetPlayerId = reader.ReadInt()
            };

        public static void WriteRoundIntentRejected(this NetworkWriter writer, RoundIntentRejectedMessage message)
        {
            writer.WriteString(message.Reason);
        }

        public static RoundIntentRejectedMessage ReadRoundIntentRejected(this NetworkReader reader) =>
            new RoundIntentRejectedMessage { Reason = reader.ReadString() };

        public static void WriteRoundView(this NetworkWriter writer, RoundViewMessage message)
        {
            writer.WriteInt(message.ViewerId);
            writer.WriteByte((byte)message.Phase);
            writer.WriteByte((byte)message.Role);
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
            }

            writer.WriteFloat(message.RemainingSeconds);
        }

        public static RoundViewMessage ReadRoundView(this NetworkReader reader)
        {
            var message = new RoundViewMessage
            {
                ViewerId = reader.ReadInt(),
                Phase = (RoundPhase)reader.ReadByte(),
                Role = (RoundRole)reader.ReadByte(),
                CrimeDescription = reader.ReadString(),
                HasAlibi = reader.ReadBool(),
                AlibiEntries = Array.Empty<AlibiEntryMessage>()
            };

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
            }

            message.RemainingSeconds = reader.ReadFloat();
            return message;
        }
    }
}
