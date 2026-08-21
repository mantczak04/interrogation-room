using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the obsolete uGUI Main Menu builder visible as an intentionally
/// disabled legacy command. The production MainMenu scene uses UI Toolkit and
/// must not be rebuilt by the old destructive workflow.
/// </summary>
public static class MainMenuSetup
{
    private const string MenuPath = "Tools/Legacy/Setup Main Menu Scene (Disabled)";

    [MenuItem(MenuPath)]
    public static void SetupScene()
    {
        const string message =
            "The legacy Main Menu builder is disabled because the production " +
            "MainMenu scene uses UI Toolkit. This command does not modify the scene.";

        Debug.LogWarning($"[MainMenuSetup] {message}");
        EditorUtility.DisplayDialog("Legacy Main Menu builder disabled", message, "OK");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateSetupScene()
    {
        return false;
    }
}
