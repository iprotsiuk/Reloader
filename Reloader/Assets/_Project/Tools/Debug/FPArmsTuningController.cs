using Reloader.Weapons.PackRuntime;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Tools.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FPArmsTuningController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Camera _camera;
        [SerializeField] private Animator _armsAnimator;
        [SerializeField] private GameObject _riflePreview;
        [SerializeField] private RuntimeAnimatorController _rifleArmsController;
        [SerializeField] private string _rifleIdleStateName = "A_FP_PCH_AR_01_Idle_Pose";
        [SerializeField] private bool _forceRifleIdleClipPose = true;
        [SerializeField] private AnimationClip _rifleIdleClip;
        [SerializeField] private string _preferredGripBoneName = "MiddleHand.R";
        [SerializeField] private Vector3 _gripLocalPosition = new Vector3(0.02f, -0.06f, 0.18f);
        [SerializeField] private Vector3 _gripLocalEuler = new Vector3(0f, 180f, -90f);
        [SerializeField] private bool _editorPreviewEnabled = true;
        [SerializeField] private string _editorRiflePrefabPath = "Assets/_Project/Weapons/Prefabs/RifleView.prefab";

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 3.2f;
        [SerializeField] private float _sprintSpeed = 5.8f;
        [SerializeField] private float _lookSensitivity = 2.0f;

        [Header("Weapon Preview")]
        [SerializeField] private bool _startEquipped = true;
        [SerializeField] private bool _forceEquippedPose = true;
        [SerializeField] private float _fireIntervalSeconds = 0.1f;
        [SerializeField] private bool _lockCursorOnPlay = true;
        [SerializeField] private PackWeaponPresentationConfig _presentationConfig = new PackWeaponPresentationConfig();

        private CharacterController _characterController;
        private PackWeaponRuntimeDriver _driver;
        private PackWeaponRuntimeState _state;
        private float _pitch;
        private float _baseFov = 60f;
        private bool _rifleIdlePlayed;
        private PlayableGraph _idlePoseGraph;
        private bool _idlePoseGraphCreated;
        private bool _rifleAttachedToGrip;
        private bool _editorPreviewApplied;

        private void Awake()
        {
            ResolveReferences();

            _state = new PackWeaponRuntimeState("fp-arms-tuning");
            _driver = new PackWeaponRuntimeDriver(_state, _presentationConfig, _armsAnimator);
            _driver.SetEquipped(_startEquipped);

            if (_camera != null)
            {
                _baseFov = _camera.fieldOfView;
            }
        }

        private void Start()
        {
            ResolveDefaultRifleController();
            if (_armsAnimator != null && _rifleArmsController != null)
            {
                _armsAnimator.runtimeAnimatorController = _rifleArmsController;
            }

            // Re-apply on Start to ensure animator/controller is fully initialized.
            _driver?.SetEquipped(_startEquipped);
            EnsureDefaultEquippedVisuals();
            TryPlayRifleIdleState();
            TryForceIdleClipPose();
            TryAttachRifleToGripBone();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ApplyEditorPreviewPose();
                return;
            }

            SetCursorLocked(_lockCursorOnPlay);
        }

        private void OnDisable()
        {
            DisposeIdlePoseGraph();
            if (Application.isPlaying)
            {
                SetCursorLocked(false);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (_editorPreviewEnabled && !_editorPreviewApplied)
                {
                    ApplyEditorPreviewPose();
                }

                return;
            }

            if (_cameraPivot == null || _camera == null || _driver == null)
            {
                return;
            }

            TickLook();
            TickMove();
            TickWeaponPresentation();
            if (_forceEquippedPose)
            {
                EnsureDefaultEquippedVisuals();
                if (!_rifleIdlePlayed)
                {
                    TryPlayRifleIdleState();
                }

                if (!_rifleAttachedToGrip)
                {
                    TryAttachRifleToGripBone();
                }
            }
        }

        private void ResolveReferences()
        {
            if (_cameraPivot == null)
            {
                var pivot = transform.Find("CameraPivot");
                if (pivot != null)
                {
                    _cameraPivot = pivot;
                }
            }

            if (_camera == null && _cameraPivot != null)
            {
                _camera = _cameraPivot.GetComponentInChildren<Camera>(true);
            }

            if (_armsAnimator == null)
            {
                _armsAnimator = GetComponentInChildren<Animator>(true);
            }

            if (_riflePreview == null)
            {
                var preview = transform.Find("CameraPivot/Camera/PlayerArms/WeaponMount/RiflePreview");
                if (preview != null)
                {
                    _riflePreview = preview.gameObject;
                }
            }

            if (_riflePreview == null && _armsAnimator != null)
            {
                var previewUnderArms = FindChildRecursive(_armsAnimator.transform, "RiflePreview");
                if (previewUnderArms != null)
                {
                    _riflePreview = previewUnderArms.gameObject;
                }
            }

            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
                _characterController.height = 2f;
                _characterController.radius = 0.3f;
                _characterController.center = new Vector3(0f, 1f, 0f);
                _characterController.skinWidth = 0.02f;
                _characterController.minMoveDistance = 0f;
                _characterController.slopeLimit = 60f;
                _characterController.stepOffset = 0.35f;
            }
        }

        private void ApplyEditorPreviewPose()
        {
            if (!_editorPreviewEnabled)
            {
                return;
            }

            ResolveReferences();
            EnsureEditorRiflePreview();
            ResolveDefaultRifleController();
            ResolveDefaultRifleIdleClip();

            if (_armsAnimator != null)
            {
                if (_rifleArmsController != null)
                {
                    _armsAnimator.runtimeAnimatorController = _rifleArmsController;
                }

                _armsAnimator.SetBool("Holstered", false);
                _armsAnimator.SetBool("IsEquipped", true);
                _armsAnimator.SetBool("IsAiming", false);
                _armsAnimator.SetFloat("Aiming", 0f);
                _armsAnimator.SetFloat("Aiming Speed Multiplier", 1f);
                if (!string.IsNullOrWhiteSpace(_rifleIdleStateName))
                {
                    _armsAnimator.Play(_rifleIdleStateName, 0, 0f);
                }

                _armsAnimator.Update(0f);
            }

            TryAttachRifleToGripBone();
            EnsureDefaultEquippedVisuals();
            _editorPreviewApplied = true;
        }

        private void EnsureEditorRiflePreview()
        {
#if UNITY_EDITOR
            if (_riflePreview != null || _armsAnimator == null || string.IsNullOrWhiteSpace(_editorRiflePrefabPath))
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_editorRiflePrefabPath);
            if (prefab == null)
            {
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, gameObject.scene) as GameObject;
            if (instance == null)
            {
                return;
            }

            instance.name = "RiflePreview";
            instance.transform.SetParent(_armsAnimator.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            _riflePreview = instance;
#endif
        }

        private void TickLook()
        {
            var look = ReadLookInput() * _lookSensitivity;
            var mouseX = look.x;
            var mouseY = look.y;

            transform.Rotate(0f, mouseX, 0f, Space.Self);

            _pitch = Mathf.Clamp(_pitch - mouseY, -89f, 89f);
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void TickMove()
        {
            var input = ReadMoveInput();
            var inputMagnitude = Mathf.Clamp01(input.magnitude);
            var sprint = IsSprintHeld();
            var speed = sprint ? _sprintSpeed : _walkSpeed;

            var move = (transform.right * input.x + transform.forward * input.y);
            _characterController.Move(move.normalized * (speed * inputMagnitude * Time.deltaTime));

            if (_armsAnimator != null)
            {
                _armsAnimator.SetFloat("Movement", inputMagnitude);
                _armsAnimator.SetBool("Running", sprint && inputMagnitude > 0.01f);
            }
        }

        private void TickWeaponPresentation()
        {
            if (IsToggleEquipPressed())
            {
                _driver.SetEquipped(!_state.IsEquipped);
            }

            var aimHeld = _state.IsEquipped && IsAimHeld();
            _camera.fieldOfView = _driver.TickAimFov(aimHeld, _camera.fieldOfView, _baseFov, Time.deltaTime);

            if (IsFireHeld() && _driver.CanFire(Time.time))
            {
                _driver.NotifyFire(Time.time, _fireIntervalSeconds);
            }

            if (IsReloadPressed())
            {
                _driver.TryStartReload(Time.time, _presentationConfig.ReloadDurationSeconds);
            }

            _driver.TryCompleteReload(Time.time);
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var x = 0f;
                var y = 0f;
                if (Keyboard.current.aKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed) x += 1f;
                if (Keyboard.current.sKey.isPressed) y -= 1f;
                if (Keyboard.current.wKey.isPressed) y += 1f;
                var keyInput = new Vector2(x, y);
                if (keyInput.sqrMagnitude > 0f)
                {
                    return Vector2.ClampMagnitude(keyInput, 1f);
                }
            }
#endif
            return Vector2.zero;
        }

        private static Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                var delta = Mouse.current.delta.ReadValue();
                if (delta.sqrMagnitude > 0f)
                {
                    // Mouse delta from the new input system is pixels/frame; scale to roughly match legacy axes.
                    return delta * 0.02f;
                }
            }
#endif
            return Vector2.zero;
        }

        private static bool IsFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return false;
#endif
        }

        private static bool IsAimHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return false;
#endif
        }

        private static bool IsReloadPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        private static bool IsToggleEquipPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.hKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        private static bool IsSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#else
            return false;
#endif
        }

        private void EnsureDefaultEquippedVisuals()
        {
            if (_riflePreview != null && !_riflePreview.activeSelf)
            {
                _riflePreview.SetActive(true);
            }

            if (_armsAnimator == null)
            {
                return;
            }

            _armsAnimator.SetBool("Holstered", false);
            _armsAnimator.SetBool("IsEquipped", true);
            _armsAnimator.SetBool("IsAiming", false);
            _armsAnimator.SetFloat("Aiming", 0f);
            _armsAnimator.SetFloat("Aiming Speed Multiplier", 1f);
        }

        private void TryAttachRifleToGripBone()
        {
            if (_rifleAttachedToGrip || _armsAnimator == null || _riflePreview == null)
            {
                return;
            }

            var grip = ResolveGripBone();
            if (grip == null)
            {
                return;
            }

            var rifleTransform = _riflePreview.transform;
            rifleTransform.SetParent(grip, false);
            rifleTransform.localPosition = _gripLocalPosition;
            rifleTransform.localRotation = Quaternion.Euler(_gripLocalEuler);
            rifleTransform.localScale = Vector3.one;
            _rifleAttachedToGrip = true;
        }

        private Transform ResolveGripBone()
        {
            if (_armsAnimator == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(_preferredGripBoneName))
            {
                var preferred = FindChildRecursive(_armsAnimator.transform, _preferredGripBoneName);
                if (preferred != null)
                {
                    return preferred;
                }
            }

            var candidates = new[]
            {
                "MiddleHand.R",
                "Palm.R",
                "Fingers.R",
                "LowerArm.R",
                "RightHand",
                "Hand.R",
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var found = FindChildRecursive(_armsAnimator.transform, candidates[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var match = FindChildRecursive(child, targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void TryPlayRifleIdleState()
        {
            if (_armsAnimator == null || string.IsNullOrWhiteSpace(_rifleIdleStateName))
            {
                return;
            }

            if (TryPlayStateOnAnyLayer(_rifleIdleStateName)
                || TryPlayStateOnAnyLayer("Layer Item.AR Idle")
                || TryPlayStateOnAnyLayer("AR Idle")
                || TryPlayStateOnAnyLayer("Layer Actions.Idle")
                || TryPlayStateOnAnyLayer("Idle"))
            {
                _rifleIdlePlayed = true;
            }
        }

        private bool TryPlayStateOnAnyLayer(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName) || _armsAnimator == null)
            {
                return false;
            }

            var hash = Animator.StringToHash(stateName);
            for (var layer = 0; layer < _armsAnimator.layerCount; layer++)
            {
                if (!_armsAnimator.HasState(layer, hash))
                {
                    continue;
                }

                _armsAnimator.Play(stateName, layer, 0f);
                return true;
            }

            return false;
        }

        private void ResolveDefaultRifleController()
        {
            if (_rifleArmsController != null)
            {
                return;
            }

#if UNITY_EDITOR
            _rifleArmsController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Animators/Character/OC_LPSP_PCH_AR_01.overrideController");
#endif
        }

        private void TryForceIdleClipPose()
        {
            if (!_forceRifleIdleClipPose || _armsAnimator == null)
            {
                return;
            }

            ResolveDefaultRifleIdleClip();
            if (_rifleIdleClip == null)
            {
                return;
            }

            DisposeIdlePoseGraph();
            _idlePoseGraph = PlayableGraph.Create("FPArmsTuningIdlePose");
            _idlePoseGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(_idlePoseGraph, "FPArmsOutput", _armsAnimator);
            var playable = AnimationClipPlayable.Create(_idlePoseGraph, _rifleIdleClip);
            playable.SetApplyFootIK(false);
            output.SetSourcePlayable(playable);
            _idlePoseGraph.Play();
            _idlePoseGraphCreated = true;
        }

        private void ResolveDefaultRifleIdleClip()
        {
            if (_rifleIdleClip != null)
            {
                return;
            }

#if UNITY_EDITOR
            _rifleIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations/Character/ARs/A_FP_PCH_AR_01_Idle_Pose.fbx");
#endif
        }

        private void DisposeIdlePoseGraph()
        {
            if (!_idlePoseGraphCreated)
            {
                return;
            }

            _idlePoseGraph.Destroy();
            _idlePoseGraphCreated = false;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
