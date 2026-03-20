using Unity.Cinemachine;
using System.Linq;
using Reloader.Player.Viewmodel;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player.Editor
{
    public static class PlayerRigMenu
    {
        private const string ActionsAssetPath = "Assets/_Project/Player/InputSystem_Actions.inputactions";
        private const string ShadowModelPath = "Assets/ThirdParty/Lowpoly Animated Men Pack/Man/Male_Casual.fbx";
        private const string FpsArmsModelPath = "Assets/_Project/Player/Resources/Viewmodels/Characters/FPS_Arms.fbx";
        private const string ViewmodelControllerPath = "Assets/_Project/Player/Resources/Viewmodels/Characters/ViewmodelArms.controller";
        private const string IdleClipPath = "Assets/_Project/Player/Resources/Viewmodels/Characters/ViewmodelIdle.anim";
        private const string WalkClipPath = "Assets/_Project/Player/Resources/Viewmodels/Characters/ViewmodelWalk.anim";
        private const string ViewmodelLayerName = "Viewmodel";
        private const string PlayerArmsRootName = "PlayerArms";
        private const string PlayerArmsVisualName = "PlayerArmsVisual";
        private const string WeaponPresentationRootName = "WeaponPresentationRoot";
        private static readonly Vector3 FpsArmsOffsetLocalPosition = new(0f, -0.027f, 0.1f);
        private static readonly Vector3 FpsArmsOffsetLocalEuler = Vector3.zero;
        private static readonly Vector3 FpsArmsOffsetLocalScale = new(0.42f, 0.42f, 0.42f);

        [MenuItem("Reloader/Player/Create FPS Rig")]
        public static void CreateFpsRig()
        {
            var playerRoot = new GameObject("PlayerRoot");
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create FPS Rig");

            if (!HasAuthoredFirstPersonPresentation(playerRoot.transform))
            {
                Debug.LogWarning("Create FPS Rig requires an authored first-person presentation hierarchy.");
                Selection.activeGameObject = playerRoot;
                return;
            }

            var controller = playerRoot.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            var inputReader = playerRoot.AddComponent<PlayerInputReader>();
            inputReader.SetActionsAsset(LoadActionsAsset());
            playerRoot.AddComponent<PlayerCursorLockController>();
            var mover = playerRoot.AddComponent<PlayerMover>();
            mover.SetInputSource(inputReader);
            mover.SetCharacterController(controller);

            var lookController = playerRoot.AddComponent<PlayerLookController>();
            lookController.SetInputSource(inputReader);

            Selection.activeGameObject = playerRoot;
        }

        [MenuItem("Reloader/Player/Repair Selected FPS Rig")]
        public static void RepairSelectedFpsRig()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogWarning("Select a player root GameObject first.");
                return;
            }

            if (!HasAuthoredFirstPersonPresentation(root.transform))
            {
                Debug.LogWarning("Selected rig must already have an authored first-person presentation hierarchy.");
                return;
            }
            Selection.activeGameObject = root;
            Debug.Log("Selected FPS rig already has authored first-person presentation roots; no repair performed.");
        }

        [MenuItem("Reloader/Player/Configure FPS Arms Viewmodel On Selected Rig")]
        public static void ConfigureFpsArmsViewmodelOnSelectedRig()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                var inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
                root = inputReader != null ? inputReader.gameObject : GameObject.Find("PlayerRoot");
            }

            if (root == null)
            {
                Debug.LogWarning("Select a player root GameObject first, or ensure a PlayerRoot with PlayerInputReader exists in scene.");
                return;
            }

            var cameraPivot = root.transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                Debug.LogWarning("Selected object has no CameraPivot child.");
                return;
            }

            if (!root.TryGetComponent<PlayerCameraDefaults>(out var defaults) || !defaults.TryGetMainCamera(out var mainCamera))
            {
                Debug.LogWarning("Selected rig must already have an authored main camera reference.");
                return;
            }

            if (!TryRebuildFpsArmsViewmodel(cameraPivot, mainCamera))
            {
                Debug.LogWarning("Selected rig must already have authored PlayerArms, PlayerArmsVisual, and WeaponPresentationRoot roots.");
                return;
            }

            Debug.Log("FPS arms viewmodel configured on selected rig.");
        }

        [MenuItem("Reloader/Player/Rebuild FPS Arms Viewmodel In Active Scene")]
        public static void RebuildFpsArmsViewmodelInActiveScene()
        {
            var inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
            var root = inputReader != null ? inputReader.gameObject : GameObject.Find("PlayerRoot");
            if (root == null)
            {
                Debug.LogWarning("Could not find PlayerRoot/PlayerInputReader in active scene.");
                return;
            }

            var cameraPivot = root.transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                Debug.LogWarning("Found PlayerRoot but it has no CameraPivot child.");
                return;
            }

            if (!root.TryGetComponent<PlayerCameraDefaults>(out var defaults) || !defaults.TryGetMainCamera(out var mainCamera))
            {
                Debug.LogWarning("Active scene player rig must already have an authored main camera reference.");
                return;
            }

            if (!TryRebuildFpsArmsViewmodel(cameraPivot, mainCamera))
            {
                Debug.LogWarning("Active scene player rig must already have authored PlayerArms, PlayerArmsVisual, and WeaponPresentationRoot roots.");
                return;
            }

            Selection.activeGameObject = root;
            Debug.Log("FPS arms viewmodel rebuilt in active scene.");
        }

        private static InputActionAsset LoadActionsAsset()
        {
            var actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
            if (actionsAsset == null)
            {
                Debug.LogWarning($"Could not load input actions asset at: {ActionsAssetPath}");
            }

            return actionsAsset;
        }

        private static bool HasAuthoredFirstPersonPresentation(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            var defaults = root.GetComponent<PlayerCameraDefaults>();
            if (defaults == null)
            {
                return false;
            }

            if (!defaults.TryGetCameraPivot(out var cameraPivot))
            {
                return false;
            }

            if (!defaults.TryGetPlayerArmsRoot(out var playerArmsRoot))
            {
                return false;
            }

            if (playerArmsRoot.Find(PlayerArmsVisualName) == null)
            {
                return false;
            }

            if (!defaults.TryGetPlayerArmsAnimator(out _))
            {
                return false;
            }

            if (!defaults.TryGetWeaponPresentationRoot(out _))
            {
                return false;
            }

            if (!defaults.TryGetCameraLookTarget(out _))
            {
                return false;
            }

            if (!defaults.TryGetMainCamera(out _))
            {
                return false;
            }

            if (!defaults.TryGetViewmodelCamera(out _))
            {
                return false;
            }

            if (root.GetComponentInChildren<CinemachineCamera>(true) == null)
            {
                return false;
            }

            return cameraPivot != null;
        }

        private static Camera EnsureMainCamera(Transform cameraPivot)
        {
            var existing = Camera.main;
            if (existing != null && existing.transform.parent == cameraPivot)
            {
                return existing;
            }

            return null;
        }

        private static CinemachineCamera FindOrCreateCmCamera(Transform root)
        {
            var existing = root.GetComponentInChildren<CinemachineCamera>(true);
            if (existing != null)
            {
                return existing;
            }

            return null;
        }

        private static void EnsureCameraPipeline(CinemachineCamera camera)
        {
            var body = camera.GetCinemachineComponent(CinemachineCore.Stage.Body);
            if (body == null)
            {
                Undo.AddComponent<CinemachineHardLockToTarget>(camera.gameObject);
            }

            var aim = camera.GetCinemachineComponent(CinemachineCore.Stage.Aim);
            if (aim == null)
            {
                Undo.AddComponent<CinemachineHardLookAt>(camera.gameObject);
            }
        }

        private static Transform CreateOrFindCameraLookTarget(Transform cameraPivot)
        {
            var existing = cameraPivot.Find("CameraLookTarget");
            if (existing != null)
            {
                return existing;
            }

            return null;
        }

        private static void EnsureShadowBody(Transform playerRoot)
        {
            var shadowBody = playerRoot.Find("ShadowBody");
            if (shadowBody == null)
            {
                var shadowBodyGo = new GameObject("ShadowBody");
                Undo.RegisterCreatedObjectUndo(shadowBodyGo, "Create Shadow Body");
                shadowBodyGo.transform.SetParent(playerRoot, false);
                shadowBody = shadowBodyGo.transform;
            }

            if (shadowBody.childCount == 0)
            {
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowModelPath);
                if (modelAsset != null)
                {
                    var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                    modelInstance ??= Object.Instantiate(modelAsset);
                    modelInstance.name = "ManShadowModel";
                    Undo.RegisterCreatedObjectUndo(modelInstance, "Create Shadow Body Model");
                    modelInstance.transform.SetParent(shadowBody, false);
                }
                else
                {
                    var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    fallback.name = "ShadowBodyFallback";
                    Undo.RegisterCreatedObjectUndo(fallback, "Create Shadow Body Fallback");
                    fallback.transform.SetParent(shadowBody, false);
                    fallback.transform.localPosition = new Vector3(0f, 1f, 0f);
                    fallback.transform.localScale = new Vector3(0.75f, 1f, 0.75f);
                    Debug.LogWarning($"Shadow model not found at '{ShadowModelPath}'. Using fallback capsule.");
                }
            }

            foreach (var renderer in shadowBody.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.receiveShadows = false;
            }

            foreach (var collider in shadowBody.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private static bool EnsureFpsArmsViewmodel(Transform cameraPivot, Camera mainCamera)
        {
            ConfigureFpsArmsImporter();

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FpsArmsModelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"FPS arms model not found at '{FpsArmsModelPath}'.");
                return false;
            }

            var playerArmsRoot = cameraPivot.Find(PlayerArmsRootName);
            if (playerArmsRoot == null)
            {
                Debug.LogWarning($"FPS arms viewmodel requires authored '{PlayerArmsRootName}' under CameraPivot.");
                return false;
            }

            var armsVisual = playerArmsRoot.Find(PlayerArmsVisualName);
            if (armsVisual == null)
            {
                Debug.LogWarning($"FPS arms viewmodel requires authored '{PlayerArmsVisualName}' under '{PlayerArmsRootName}'.");
                return false;
            }

            var weaponPresentationRoot = cameraPivot.Find(WeaponPresentationRootName);
            if (weaponPresentationRoot == null)
            {
                Debug.LogWarning("WeaponPresentationRoot must already be authored before configuring FPS arms viewmodel.");
                return false;
            }

            playerArmsRoot.localPosition = FpsArmsOffsetLocalPosition;
            playerArmsRoot.localRotation = Quaternion.Euler(FpsArmsOffsetLocalEuler);
            playerArmsRoot.localScale = FpsArmsOffsetLocalScale;
            armsVisual.localPosition = Vector3.zero;
            armsVisual.localRotation = Quaternion.identity;
            armsVisual.localScale = Vector3.one;
            var viewmodelLayer = EnsureLayer(ViewmodelLayerName);
            if (viewmodelLayer < 0)
            {
                Debug.LogWarning("Could not assign Viewmodel layer. Cameras were not reconfigured.");
                return false;
            }

            var viewmodelCamera = ConfigureViewmodelCameras(cameraPivot, mainCamera, viewmodelLayer);
            EnsureUnitScale(cameraPivot.root, cameraPivot, mainCamera.transform, viewmodelCamera != null ? viewmodelCamera.transform : null);

            foreach (var collider in armsVisual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            SetLayerRecursively(playerArmsRoot.gameObject, viewmodelLayer);
            SetLayerRecursively(weaponPresentationRoot.gameObject, viewmodelLayer);
            EnsureViewmodelAnimator(armsVisual, cameraPivot.root);
            EnsureWeaponHandRigController(cameraPivot.root);
            LogViewmodelRendererType(armsVisual);
            return true;
        }

        private static bool TryRebuildFpsArmsViewmodel(Transform cameraPivot, Camera mainCamera)
        {
            return EnsureFpsArmsViewmodel(cameraPivot, mainCamera);
        }

        private static void ConfigureFpsArmsImporter()
        {
            var importer = AssetImporter.GetAtPath(FpsArmsModelPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            var avatars = AssetDatabase.LoadAllAssetsAtPath(FpsArmsModelPath).OfType<Avatar>();
            var hasValidHumanoid = avatars.Any(a => a != null && a.isValid && a.isHuman);
            if (hasValidHumanoid)
            {
                return;
            }

            importer = AssetImporter.GetAtPath(FpsArmsModelPath) as ModelImporter;
            if (importer == null || importer.animationType == ModelImporterAnimationType.Generic)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.LogWarning("FPS_Arms humanoid import was invalid. Switched import type to Generic.");
        }

        private static int EnsureLayer(string layerName)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            if (tagManager == null)
            {
                return -1;
            }

            var serialized = new SerializedObject(tagManager);
            var layersProp = serialized.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray)
            {
                return -1;
            }

            for (var i = 8; i < 32; i++)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(i);
                if (layerProp != null && string.Equals(layerProp.stringValue, layerName))
                {
                    return i;
                }
            }

            for (var i = 8; i < 32; i++)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(i);
                if (layerProp == null || !string.IsNullOrEmpty(layerProp.stringValue))
                {
                    continue;
                }

                layerProp.stringValue = layerName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                return i;
            }

            Debug.LogWarning($"No free user layer available for '{layerName}'.");
            return -1;
        }

        private static Camera ConfigureViewmodelCameras(Transform cameraPivot, Camera mainCamera, int viewmodelLayer)
        {
            if (mainCamera == null || cameraPivot == null)
            {
                return null;
            }

            var viewmodelCamera = cameraPivot.Find("ViewmodelCamera")?.GetComponent<Camera>();
            if (viewmodelCamera == null)
            {
                return null;
            }

            viewmodelCamera.clearFlags = CameraClearFlags.Depth;
            viewmodelCamera.cullingMask = 1 << viewmodelLayer;
            viewmodelCamera.nearClipPlane = 0.01f;
            viewmodelCamera.farClipPlane = 10f;
            viewmodelCamera.depth = mainCamera.depth + 1f;
            viewmodelCamera.fieldOfView = mainCamera.fieldOfView;
            if (viewmodelCamera.GetComponent<AudioListener>() != null)
            {
                Object.DestroyImmediate(viewmodelCamera.GetComponent<AudioListener>());
            }

            mainCamera.cullingMask &= ~(1 << viewmodelLayer);

            var mainCamData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            if (mainCamData == null)
            {
                mainCamData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            var viewmodelCamData = viewmodelCamera.GetComponent<UniversalAdditionalCameraData>();
            if (viewmodelCamData == null)
            {
                viewmodelCamData = viewmodelCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            mainCamData.renderType = CameraRenderType.Base;
            viewmodelCamData.renderType = CameraRenderType.Overlay;

            if (!mainCamData.cameraStack.Contains(viewmodelCamera))
            {
                mainCamData.cameraStack.Add(viewmodelCamera);
            }

            return viewmodelCamera;
        }

        private static void EnsureWeaponHandRigController(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return;
            }

            var controller = playerRoot.GetComponent<WeaponHandRigController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<WeaponHandRigController>(playerRoot.gameObject);
            }

            EditorUtility.SetDirty(controller);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void LogViewmodelRendererType(Transform armsRoot)
        {
            var skinned = armsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var mesh = armsRoot.GetComponentsInChildren<MeshRenderer>(true);
            Debug.Log($"FPS arms renderer check: skinned={skinned.Length}, meshRenderer={mesh.Length}");
        }

        private static void EnsureUnitScale(params Transform[] transforms)
        {
            foreach (var t in transforms)
            {
                if (t == null)
                {
                    continue;
                }

                t.localScale = Vector3.one;
            }
        }

        private static void EnsureViewmodelAnimator(Transform armsRoot, Transform playerRoot)
        {
            var animator = armsRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = armsRoot.gameObject.AddComponent<Animator>();
            }

            var controller = EnsureViewmodelAnimatorController();
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            var driver = playerRoot.GetComponent<FpsViewmodelAnimatorDriver>();
            if (driver == null)
            {
                driver = playerRoot.gameObject.AddComponent<FpsViewmodelAnimatorDriver>();
            }

            driver.Configure(animator, playerRoot.GetComponent<CharacterController>());
            var adapter = playerRoot.GetComponent<ViewmodelAnimationAdapter>();
            if (adapter == null)
            {
                adapter = playerRoot.gameObject.AddComponent<ViewmodelAnimationAdapter>();
            }
            adapter.Configure(animator);

            EditorUtility.SetDirty(playerRoot);
            EditorUtility.SetDirty(armsRoot);
        }

        private static AnimatorController EnsureViewmodelAnimatorController()
        {
            var idleClip = EnsureViewmodelClip(
                IdleClipPath,
                idle: true,
                amplitude: 0.006f,
                frequency: 1f);
            var walkClip = EnsureViewmodelClip(
                WalkClipPath,
                idle: false,
                amplitude: 0.02f,
                frequency: 2.2f);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ViewmodelControllerPath);
            if (controller != null)
            {
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(ViewmodelControllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.states = new ChildAnimatorState[0];

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
            rootStateMachine.defaultState = idleState;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip EnsureViewmodelClip(string path, bool idle, float amplitude, float frequency)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                ConfigureClipLoopSettings(clip);
                return clip;
            }

            clip = new AnimationClip
            {
                frameRate = 60f,
                name = idle ? "ViewmodelIdle" : "ViewmodelWalk",
                wrapMode = WrapMode.Loop
            };

            var duration = 1f;
            var yCurve = AnimationCurve.EaseInOut(0f, 0f, duration * 0.5f, amplitude);
            yCurve.AddKey(new Keyframe(duration, 0f));

            var xAmplitude = idle ? amplitude * 0.4f : amplitude;
            var xCurve = AnimationCurve.EaseInOut(0f, -xAmplitude, duration * 0.5f, xAmplitude);
            xCurve.AddKey(new Keyframe(duration, -xAmplitude));

            var zRotAmplitude = idle ? 0.8f : 2.5f;
            var zRot = AnimationCurve.EaseInOut(0f, -zRotAmplitude, duration * 0.5f, zRotAmplitude);
            zRot.AddKey(new Keyframe(duration, -zRotAmplitude));

            // Local bob/sway relative to authored base pose.
            clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", xCurve);
            clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", yCurve);
            clip.SetCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z", zRot);

            clip.legacy = false;
            AssetDatabase.CreateAsset(clip, path);
            ConfigureClipLoopSettings(clip);

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.importAnimation = true;
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void ConfigureClipLoopSettings(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            clip.wrapMode = WrapMode.Loop;
            clip.legacy = false;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
    }
}
