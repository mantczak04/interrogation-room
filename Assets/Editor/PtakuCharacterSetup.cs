using System;
using System.Linq;
using InterrogationRoom.Gameplay.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PtakuCharacterSetup
{
    private const string CharacterFolder = "Assets/Characters/Ptaku";
    // The standalone generated model is a static mesh. The Idle export is the
    // single With Skin FBX and therefore owns the animated mesh and Humanoid rig.
    private const string ModelPath = CharacterFolder + "/Ptaku_Idle.fbx";
    private const string TexturePath = CharacterFolder + "/Ptaku_Color.jpg";
    private const string ControllerPath = CharacterFolder + "/PtakuCharacter.controller";
    private const string MaterialPath = CharacterFolder + "/Ptaku_Material.mat";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const float ImportScale = 0.01631579f;

    [MenuItem("Tools/Interrogation Room/Log Ptaku Sources")]
    public static void LogSources()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CharacterFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            int rendererCount = source == null ? 0 : source.GetComponentsInChildren<Renderer>(true).Length;
            int skinnedRendererCount = source == null
                ? 0
                : source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            Debug.Log($"Ptaku source '{path}': renderers={rendererCount}, skinned={skinnedRendererCount}.");
        }
    }

    [MenuItem("Tools/Interrogation Room/Setup Ptaku Character")]
    public static void Setup()
    {
        try
        {
            Material material = CreateOrUpdateMaterial();
            ConfigureModelImporter();
            ConfigureModelMaterialRemap(material);
            RequireSkinnedRenderer(ModelPath);
            Avatar avatar = LoadAvatar();
            ValidateAvatarScale(avatar);

            AnimationClip idle = ConfigureAnimation("Ptaku_Idle.fbx", avatar, true);
            AnimationClip walking = ConfigureAnimation("Ptaku_Walking.fbx", avatar, true);
            AnimationClip sitting = ConfigureAnimation("Ptaku_Sitting.fbx", avatar, true);
            AnimationClip punchA = ConfigureAnimation("Ptaku_Uppercut_A.fbx", avatar, false);
            AnimationClip punchB = ConfigureAnimation("Ptaku_Uppercut_B.fbx", avatar, false);
            AnimationClip death = ConfigureAnimation("Ptaku_Death.fbx", avatar, false);
            AnimationClip dance = ConfigureAnimation("Ptaku_Dance.fbx", avatar, true);

            AnimatorController controller = CreateAnimatorController(
                idle,
                walking,
                sitting,
                punchA,
                punchB,
                death,
                dance);

            ConfigurePlayerPrefab(avatar, controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Ptaku character setup completed successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    [MenuItem("Tools/Interrogation Room/Remove Incomplete Ptaku Character")]
    public static void RemoveIncompleteCharacter()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform modelRoot = prefabRoot.transform.Find("Visuals/Ptaku") ??
                                  prefabRoot.transform.Find("Ptaku");
            if (modelRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(modelRoot.gameObject);
            }

            PlayerController playerController = prefabRoot.GetComponent<PlayerController>();
            SerializedObject serializedPlayer = new(playerController);
            SerializedProperty visuals = serializedPlayer.FindProperty("characterVisuals");
            int elementIndex = FindCharacterVisual(visuals, CharacterId.Ptaku);
            if (elementIndex >= 0)
            {
                visuals.DeleteArrayElementAtIndex(elementIndex);
                serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Incomplete Ptaku character removed from the Player prefab.");
    }

    private static void ConfigureModelImporter()
    {
        ModelImporter importer = RequireImporter(ModelPath);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.globalScale = ImportScale;
        importer.useFileScale = false;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        NormalizeHumanoidSkeleton(importer);
    }

    private static void NormalizeHumanoidSkeleton(ModelImporter importer)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (source == null)
        {
            throw new InvalidOperationException($"Model prefab not found at '{ModelPath}'.");
        }

        var transformsByName = source.GetComponentsInChildren<Transform>(true)
            .GroupBy(transform => transform.name)
            .ToDictionary(group => group.Key, group => group.First());

        HumanDescription description = importer.humanDescription;
        SkeletonBone[] skeleton = description.skeleton;
        int matchedBones = 0;

        for (int index = 0; index < skeleton.Length; index++)
        {
            SkeletonBone bone = skeleton[index];
            if (!transformsByName.TryGetValue(bone.name, out Transform sourceTransform))
            {
                continue;
            }

            bone.position = sourceTransform.localPosition;
            bone.rotation = sourceTransform.localRotation;
            bone.scale = sourceTransform.localScale;
            skeleton[index] = bone;
            matchedBones++;
        }

        if (matchedBones == 0)
        {
            throw new InvalidOperationException("Ptaku Humanoid skeleton could not be normalized.");
        }

        description.skeleton = skeleton;
        importer.humanDescription = description;
        importer.SaveAndReimport();
    }

    private static void ConfigureModelMaterialRemap(Material material)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Renderer sourceRenderer = source == null ? null : source.GetComponentInChildren<Renderer>(true);
        Material sourceMaterial = sourceRenderer == null ? null : sourceRenderer.sharedMaterial;
        if (sourceMaterial == null)
        {
            throw new InvalidOperationException($"No source material was imported from '{ModelPath}'.");
        }

        ModelImporter importer = RequireImporter(ModelPath);
        var identifier = new AssetImporter.SourceAssetIdentifier
        {
            type = typeof(Material),
            name = sourceMaterial.name
        };
        importer.AddRemap(identifier, material);
        importer.SaveAndReimport();
    }

    private static AnimationClip ConfigureAnimation(string fileName, Avatar avatar, bool loop)
    {
        string path = CharacterFolder + "/" + fileName;
        ModelImporter importer = RequireImporter(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.globalScale = ImportScale;
        importer.useFileScale = false;
        if (path == ModelPath)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        else
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
        }
        importer.importAnimation = true;
        importer.materialImportMode = path == ModelPath
            ? ModelImporterMaterialImportMode.ImportStandard
            : ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips.Length == 0)
        {
            throw new InvalidOperationException($"No animation clip was imported from '{path}'.");
        }

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loop = loop;
            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        return LoadAnimationClip(path);
    }

    private static AnimatorController CreateAnimatorController(
        AnimationClip idle,
        AnimationClip walking,
        AnimationClip sitting,
        AnimationClip punchA,
        AnimationClip punchB,
        AnimationClip death,
        AnimationClip dance)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("LookPitch", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsSeated", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Punch", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PunchVariant", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Dance", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        BlendTree locomotionBlend = new()
        {
            name = "Locomotion Blend",
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(locomotionBlend, controller);
        locomotionBlend.AddChild(idle, 0f);
        locomotionBlend.AddChild(walking, 1f);

        AnimatorState locomotion = stateMachine.AddState("Locomotion");
        locomotion.motion = locomotionBlend;
        stateMachine.defaultState = locomotion;

        AnimatorState sittingState = AddState(stateMachine, "Sitting", sitting);
        AnimatorState punchAState = AddState(stateMachine, "Uppercut A", punchA);
        AnimatorState punchBState = AddState(stateMachine, "Uppercut B", punchB);
        AnimatorState deathState = AddState(stateMachine, "Death", death);
        AnimatorState danceState = AddState(stateMachine, "Dance", dance);

        AddBoolTransition(locomotion, sittingState, "IsSeated", true, 0.15f);
        AddBoolTransition(sittingState, locomotion, "IsSeated", false, 0.15f);
        AddBoolTransition(locomotion, danceState, "Dance", true, 0.15f);
        AddBoolTransition(danceState, locomotion, "Dance", false, 0.15f);

        AddPunchTransition(stateMachine, punchAState, 0);
        AddPunchTransition(stateMachine, punchBState, 1);
        AddExitTransition(punchAState, locomotion, 0.9f, 0.12f);
        AddExitTransition(punchBState, locomotion, 0.9f, 0.12f);

        AnimatorStateTransition sitTransition = stateMachine.AddAnyStateTransition(sittingState);
        ConfigureImmediateTransition(sitTransition, 0.12f);
        sitTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsSeated");
        sitTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        AnimatorStateTransition deathTransition = stateMachine.AddAnyStateTransition(deathState);
        ConfigureImmediateTransition(deathTransition, 0.08f);
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion)
    {
        AnimatorState state = stateMachine.AddState(name);
        state.motion = motion;
        return state;
    }

    private static void AddBoolTransition(
        AnimatorState source,
        AnimatorState destination,
        string parameter,
        bool expected,
        float duration)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureImmediateTransition(transition, duration);
        transition.AddCondition(
            expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            parameter);
    }

    private static void AddPunchTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        int variant)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        ConfigureImmediateTransition(transition, 0.08f);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Punch");
        transition.AddCondition(AnimatorConditionMode.Equals, variant, "PunchVariant");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSeated");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
    }

    private static void AddExitTransition(
        AnimatorState source,
        AnimatorState destination,
        float exitTime,
        float duration)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static void ConfigureImmediateTransition(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
    }

    private static Material CreateOrUpdateMaterial()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture == null)
        {
            throw new InvalidOperationException($"Texture not found at '{TexturePath}'.");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("No supported Lit shader is available.");
            }

            material = new Material(shader) { name = "Ptaku Material" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigurePlayerPrefab(
        Avatar avatar,
        RuntimeAnimatorController controller)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform visualsRoot = prefabRoot.transform.Find("Visuals");
            if (visualsRoot == null)
            {
                throw new InvalidOperationException("Player prefab does not contain a Visuals root.");
            }

            Transform existing = visualsRoot.Find("Ptaku") ?? prefabRoot.transform.Find("Ptaku");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException($"Model prefab not found at '{ModelPath}'.");
            }

            GameObject modelRoot = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, visualsRoot);
            modelRoot.name = "Ptaku";
            modelRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            modelRoot.transform.localScale = Vector3.one;

            Animator nestedAnimator = modelRoot.GetComponent<Animator>();
            if (nestedAnimator != null)
            {
                UnityEngine.Object.DestroyImmediate(nestedAnimator);
            }

            modelRoot.SetActive(false);
            PlayerController playerController = prefabRoot.GetComponent<PlayerController>();
            if (playerController == null)
            {
                throw new InvalidOperationException("Player prefab does not contain PlayerController.");
            }

            SerializedObject serializedPlayer = new(playerController);
            SerializedProperty visuals = serializedPlayer.FindProperty("characterVisuals");
            int elementIndex = FindCharacterVisual(visuals, CharacterId.Ptaku);
            if (elementIndex < 0)
            {
                elementIndex = visuals.arraySize;
                visuals.InsertArrayElementAtIndex(elementIndex);
            }

            SerializedProperty visual = visuals.GetArrayElementAtIndex(elementIndex);
            visual.FindPropertyRelative("characterId").enumValueIndex = (int)CharacterId.Ptaku;
            visual.FindPropertyRelative("modelRoot").objectReferenceValue = modelRoot;
            visual.FindPropertyRelative("animatorController").objectReferenceValue = controller;
            visual.FindPropertyRelative("avatar").objectReferenceValue = avatar;
            visual.FindPropertyRelative("supportsDance").boolValue = true;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int FindCharacterVisual(SerializedProperty visuals, CharacterId characterId)
    {
        for (int index = 0; index < visuals.arraySize; index++)
        {
            SerializedProperty visual = visuals.GetArrayElementAtIndex(index);
            if (visual.FindPropertyRelative("characterId").enumValueIndex == (int)characterId)
            {
                return index;
            }
        }

        return -1;
    }

    private static ModelImporter RequireImporter(string path)
    {
        if (AssetImporter.GetAtPath(path) is ModelImporter importer)
        {
            return importer;
        }

        throw new InvalidOperationException($"ModelImporter not found for '{path}'.");
    }

    private static void RequireSkinnedRenderer(string path)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (source == null || source.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
        {
            throw new InvalidOperationException(
                $"'{path}' has no SkinnedMeshRenderer. Download one Mixamo animation With Skin.");
        }
    }

    private static Avatar LoadAvatar()
    {
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<Avatar>()
            .FirstOrDefault(candidate => candidate.isValid && candidate.isHuman);
        return avatar ?? throw new InvalidOperationException("Ptaku model did not produce a valid Humanoid Avatar.");
    }

    private static void ValidateAvatarScale(Avatar avatar)
    {
        var validationObject = new GameObject("Ptaku Avatar Scale Validation");
        try
        {
            Animator validationAnimator = validationObject.AddComponent<Animator>();
            validationAnimator.avatar = avatar;
            if (validationAnimator.humanScale < 0.25f || validationAnimator.humanScale > 2f)
            {
                throw new InvalidOperationException(
                    $"Ptaku Humanoid scale is invalid: {validationAnimator.humanScale:F4}.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(validationObject);
        }
    }

    private static AnimationClip LoadAnimationClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
        return clip ?? throw new InvalidOperationException($"AnimationClip not found in '{path}'.");
    }
}
