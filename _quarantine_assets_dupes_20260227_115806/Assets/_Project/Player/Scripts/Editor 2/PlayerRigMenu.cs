using Unity.Cinemachine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player.Editor
{
    public static class PlayerRigMenu
    {
        private const string ActionsAssetPath = "Assets/_Project/Player/InputSystem_Actions.inputactions";

        [MenuItem("Reloader/Player/Create FPS Rig")]
        public static void CreateFpsRig()
        {
            var playerRoot = new GameObject("PlayerRoot");
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create FPS Rig");

            var controller = playerRoot.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);
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

            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(playerRoot.transform, false);
            cameraPivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            lookController.SetPitchTransform(cameraPivot.transform);
            var cameraLookTarget = CreateOrFindCameraLookTarget(cameraPivot.transform);

            var camera = EnsureMainCamera(cameraPivot.transform);
            var brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            }

            var cmCameraGo = new GameObject("CM_PlayerCamera");
            cmCameraGo.transform.SetParent(playerRoot.transform, false);
            var cmCamera = cmCameraGo.AddComponent<CinemachineCamera>();
            cmCamera.Follow = cameraPivot.transform;
            cmCamera.LookAt = cameraLookTarget;
            EnsureCameraPipeline(cmCamera);

            var defaults = playerRoot.AddComponent<PlayerCameraDefaults>();
            var defaultsSo = new SerializedObject(defaults);
            defaultsSo.FindProperty("_mainCamera").objectReferenceValue = camera;
            defaultsSo.FindProperty("_brain").objectReferenceValue = brain;
            defaultsSo.FindProperty("_cinemachineCamera").objectReferenceValue = cmCamera;
            defaultsSo.FindProperty("_cameraFollowTarget").objectReferenceValue = cameraPivot.transform;
            defaultsSo.FindProperty("_cameraLookTarget").objectReferenceValue = cameraLookTarget;
            defaultsSo.ApplyModifiedPropertiesWithoutUndo();
            defaults.ApplyDefaults();

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

            var controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<CharacterController>(root);
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.stepOffset = 0.3f;
                controller.slopeLimit = 45f;
            }

            var inputReader = root.GetComponent<PlayerInputReader>();
            if (inputReader == null)
            {
                inputReader = Undo.AddComponent<PlayerInputReader>(root);
            }

            inputReader.SetActionsAsset(LoadActionsAsset());

            if (root.GetComponent<PlayerCursorLockController>() == null)
            {
                Undo.AddComponent<PlayerCursorLockController>(root);
            }

            var mover = root.GetComponent<PlayerMover>();
            if (mover == null)
            {
                mover = Undo.AddComponent<PlayerMover>(root);
            }

            mover.SetInputSource(inputReader);
            mover.SetCharacterController(controller);

            var lookController = root.GetComponent<PlayerLookController>();
            if (lookController == null)
            {
                lookController = Undo.AddComponent<PlayerLookController>(root);
            }

            lookController.SetInputSource(inputReader);

            var cameraPivot = root.transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                var pivotGo = new GameObject("CameraPivot");
                Undo.RegisterCreatedObjectUndo(pivotGo, "Create Camera Pivot");
                pivotGo.transform.SetParent(root.transform, false);
                pivotGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                cameraPivot = pivotGo.transform;
            }

            lookController.SetPitchTransform(cameraPivot);
            var cameraLookTarget = CreateOrFindCameraLookTarget(cameraPivot);

            var cmCamera = FindOrCreateCmCamera(root.transform);
            cmCamera.Follow = cameraPivot;
            cmCamera.LookAt = cameraLookTarget;
            EnsureCameraPipeline(cmCamera);

            var camera = EnsureMainCamera(cameraPivot);
            var brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            }

            var defaults = root.GetComponent<PlayerCameraDefaults>();
            if (defaults == null)
            {
                defaults = Undo.AddComponent<PlayerCameraDefaults>(root);
            }

            var defaultsSo = new SerializedObject(defaults);
            defaultsSo.FindProperty("_mainCamera").objectReferenceValue = camera;
            defaultsSo.FindProperty("_brain").objectReferenceValue = brain;
            defaultsSo.FindProperty("_cinemachineCamera").objectReferenceValue = cmCamera;
            defaultsSo.FindProperty("_cameraFollowTarget").objectReferenceValue = cameraPivot;
            defaultsSo.FindProperty("_cameraLookTarget").objectReferenceValue = cameraLookTarget;
            defaultsSo.ApplyModifiedPropertiesWithoutUndo();
            defaults.ApplyDefaults();

            Selection.activeGameObject = root;
            Debug.Log("Selected FPS rig repaired and wired.");
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

        private static Camera EnsureMainCamera(Transform cameraPivot)
        {
            var existing = Camera.main;
            if (existing != null)
            {
                return existing;
            }

            var mainCameraGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(mainCameraGo, "Create Main Camera");
            mainCameraGo.transform.SetPositionAndRotation(cameraPivot.position, cameraPivot.rotation);

            var camera = mainCameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            mainCameraGo.AddComponent<AudioListener>();
            mainCameraGo.AddComponent<UniversalAdditionalCameraData>();
            return camera;
        }

        private static CinemachineCamera FindOrCreateCmCamera(Transform root)
        {
            var existing = root.GetComponentInChildren<CinemachineCamera>(true);
            if (existing != null)
            {
                return existing;
            }

            var cmCameraGo = new GameObject("CM_PlayerCamera");
            Undo.RegisterCreatedObjectUndo(cmCameraGo, "Create CM Player Camera");
            cmCameraGo.transform.SetParent(root, false);
            return cmCameraGo.AddComponent<CinemachineCamera>();
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

            var lookTargetGo = new GameObject("CameraLookTarget");
            Undo.RegisterCreatedObjectUndo(lookTargetGo, "Create Camera Look Target");
            lookTargetGo.transform.SetParent(cameraPivot, false);
            lookTargetGo.transform.localPosition = new Vector3(0f, 0f, 10f);
            return lookTargetGo.transform;
        }
    }
}
