using System;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Characters
{
    /// <summary>
    /// Seam for the dance radial menu. The menu itself is UI and lives in the runtime UI
    /// assembly, which already references gameplay; PlayerController talks to it through this
    /// interface so gameplay does not have to reference UI back.
    /// </summary>
    public interface IDanceRadialMenu
    {
        bool IsOpen { get; }

        void Open(bool currentlyDancing);

        int Close();

        void Cancel();

        void RefreshSelection();
    }

    /// <summary>
    /// Registration point for the concrete radial menu implementation. The UI assembly installs
    /// <see cref="Factory"/> at startup; gameplay creates the menu for the local player only.
    /// </summary>
    public static class DanceRadialMenuHost
    {
        public static Func<GameObject, IDanceRadialMenu> Factory;

        public static IDanceRadialMenu Create(GameObject owner) =>
            owner == null ? null : Factory?.Invoke(owner);
    }
}
