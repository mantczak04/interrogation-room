using System;
using System.Collections.Generic;
using InterrogationRoom.Networking;

namespace InterrogationRoom.UI
{
    public enum VoiceRosterChangeKind
    {
        Add,
        Remove,
        Update
    }

    public readonly struct VoiceRosterChange
    {
        public VoiceRosterChangeKind Kind { get; }
        public uint NetworkIdentityNetId { get; }
        public string DisplayName { get; }

        public VoiceRosterChange(
            VoiceRosterChangeKind kind,
            uint networkIdentityNetId,
            string displayName)
        {
            Kind = kind;
            NetworkIdentityNetId = networkIdentityNetId;
            DisplayName = displayName ?? string.Empty;
        }
    }

    public static class VoiceRosterDiff
    {
        public static IReadOnlyList<VoiceRosterChange> Calculate(
            IReadOnlyDictionary<uint, string> previous,
            IReadOnlyList<VoiceRosterEntry> current,
            uint localNetId)
        {
            var currentByNetId = new Dictionary<uint, string>();
            if (current != null)
            {
                foreach (VoiceRosterEntry entry in current)
                {
                    if (entry.NetworkIdentityNetId != 0u &&
                        entry.NetworkIdentityNetId != localNetId)
                    {
                        currentByNetId[entry.NetworkIdentityNetId] =
                            entry.DisplayName ?? string.Empty;
                    }
                }
            }

            var changes = new List<VoiceRosterChange>();
            if (previous != null)
            {
                foreach (KeyValuePair<uint, string> entry in previous)
                {
                    if (!currentByNetId.ContainsKey(entry.Key))
                    {
                        changes.Add(new VoiceRosterChange(
                            VoiceRosterChangeKind.Remove,
                            entry.Key,
                            entry.Value));
                    }
                }
            }

            foreach (KeyValuePair<uint, string> entry in currentByNetId)
            {
                if (previous == null || !previous.TryGetValue(entry.Key, out string oldName))
                {
                    changes.Add(new VoiceRosterChange(
                        VoiceRosterChangeKind.Add,
                        entry.Key,
                        entry.Value));
                }
                else if (!string.Equals(oldName, entry.Value, StringComparison.Ordinal))
                {
                    changes.Add(new VoiceRosterChange(
                        VoiceRosterChangeKind.Update,
                        entry.Key,
                        entry.Value));
                }
            }

            return changes;
        }
    }
}
