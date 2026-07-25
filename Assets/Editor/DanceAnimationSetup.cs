using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DanceAnimationSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string DanceParameter = "Dance";
    private const string DanceIndexParameter = "DanceIndex";

    private static readonly string[] DancePaths =
    {
        "Assets/Characters/Animations/Dance/Dancing Twerk.fbx",
        "Assets/Characters/Animations/Dance/Silly Dancing.fbx",
        "Assets/Characters/Animations/Dance/Samba Dancing.fbx",
        "Assets/Characters/Animations/Dance/Hokey Pokey.fbx"
    };

    private static readonly string[] ControllerPaths =
    {
        "Assets/Characters/Malpa/MalpaCharacter.controller",
        "Assets/Characters/Wieprz/WieprzCharacter.controller",
        "Assets/Characters/Jak/JakCharacter.controller",
        "Assets/Characters/Karton/KartonCharacter.controller",
        "Assets/Characters/Ptaku/PtakuCharacter.controller"
    };

    [MenuItem("Tools/Interrogation Room/Configure Dance Radial Menu")]
    public static void Configure()
    {
        AnimationClip[] danceClips = DancePaths
            .Select(ConfigureDanceClip)
            .ToArray();

        foreach (string controllerPath in ControllerPaths)
            ConfigureController(controllerPath, danceClips);

        EnableDanceForEveryCharacter();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"Dance setup completed: {danceClips.Length} clips, " +
            $"{ControllerPaths.Length} character controllers.");
    }

    private static AnimationClip ConfigureDanceClip(string path)
    {
        if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            throw new InvalidOperationException($"Dance FBX importer not found at '{path}'.");

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips.Length == 0)
            throw new InvalidOperationException($"No animation clip was imported from '{path}'.");

        string clipName = System.IO.Path.GetFileNameWithoutExtension(path);
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.name = clipName;
            clip.loop = true;
            clip.loopTime = true;
            clip.loopPose = true;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = false;
            clip.keepOriginalPositionXZ = true;
            clip.heightFromFeet = true;
            clip.heightOffset = 0f;
            clip.wrapMode = WrapMode.Loop;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Avatar>()
            .FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException($"Dance FBX at '{path}' has no valid Humanoid avatar.");

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate =>
                !candidate.name.StartsWith("__preview__", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Dance clip not found in '{path}'.");
    }

    private static void ConfigureController(
        string path,
        IReadOnlyList<AnimationClip> danceClips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            throw new InvalidOperationException($"Animator Controller not found at '{path}'.");

        EnsureParameter(controller, DanceParameter, AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, DanceIndexParameter, AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState locomotion = stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state.name == "Locomotion");
        if (locomotion == null)
            throw new InvalidOperationException($"Controller '{path}' has no Locomotion state.");

        RemoveExistingDanceGraph(stateMachine);

        var danceBlend = new BlendTree
        {
            name = "Dance Blend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = DanceIndexParameter,
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(danceBlend, controller);
        for (int index = 0; index < danceClips.Count; index++)
            danceBlend.AddChild(danceClips[index], index);

        AnimatorState danceState = stateMachine.AddState("Dance", new Vector3(650f, 180f));
        danceState.motion = danceBlend;
        danceState.iKOnFeet = true;

        AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(danceState);
        ConfigureImmediateTransition(enter, 0.12f);
        enter.AddCondition(AnimatorConditionMode.If, 0f, DanceParameter);
        enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSeated");
        enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        AnimatorStateTransition exit = danceState.AddTransition(locomotion);
        ConfigureImmediateTransition(exit, 0.12f);
        exit.AddCondition(AnimatorConditionMode.IfNot, 0f, DanceParameter);

        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
    }

    private static void EnsureParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        for (int index = controller.parameters.Length - 1; index >= 0; index--)
        {
            AnimatorControllerParameter parameter = controller.parameters[index];
            if (parameter.name != name)
                continue;

            if (parameter.type == type)
                return;

            controller.RemoveParameter(index);
        }

        controller.AddParameter(name, type);
    }

    private static void RemoveExistingDanceGraph(AnimatorStateMachine stateMachine)
    {
        BlendTree[] danceBlendTrees = stateMachine.states
            .Where(child => IsDanceState(child.state))
            .Select(child => child.state.motion)
            .OfType<BlendTree>()
            .ToArray();

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            if (UsesDanceParameter(transition) || IsDanceState(transition.destinationState))
                stateMachine.RemoveAnyStateTransition(transition);
        }

        foreach (ChildAnimatorState child in stateMachine.states.ToArray())
        {
            foreach (AnimatorStateTransition transition in child.state.transitions.ToArray())
            {
                if (UsesDanceParameter(transition) || IsDanceState(transition.destinationState))
                    child.state.RemoveTransition(transition);
            }
        }

        foreach (ChildAnimatorState child in stateMachine.states.ToArray())
        {
            if (IsDanceState(child.state))
                stateMachine.RemoveState(child.state);
        }

        foreach (BlendTree danceBlendTree in danceBlendTrees)
        {
            if (danceBlendTree != null)
                UnityEngine.Object.DestroyImmediate(danceBlendTree, true);
        }
    }

    private static bool UsesDanceParameter(AnimatorStateTransition transition) =>
        transition.conditions.Any(condition =>
            condition.parameter == DanceParameter ||
            condition.parameter == DanceIndexParameter);

    private static bool IsDanceState(AnimatorState state) =>
        state != null &&
        (state.name == "Dance" ||
         state.name.StartsWith("Dance - ", StringComparison.Ordinal));

    private static void ConfigureImmediateTransition(
        AnimatorStateTransition transition,
        float duration)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
    }

    private static void EnableDanceForEveryCharacter()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            PlayerController playerController = prefabRoot.GetComponent<PlayerController>();
            if (playerController == null)
                throw new InvalidOperationException("Player prefab has no PlayerController.");

            var serializedPlayer = new SerializedObject(playerController);
            SerializedProperty visuals = serializedPlayer.FindProperty("characterVisuals");
            for (int index = 0; index < visuals.arraySize; index++)
            {
                visuals
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("supportsDance")
                    .boolValue = true;
            }

            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
