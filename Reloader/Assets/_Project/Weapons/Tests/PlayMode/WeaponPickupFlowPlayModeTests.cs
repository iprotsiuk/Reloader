using System.Collections;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponPickupFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator PickupFlow_StoresWeaponItemAndDisablesTarget()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var inventory = root.AddComponent<PlayerInventoryController>();

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.position = Vector3.zero;
            cameraGo.transform.forward = Vector3.forward;
            var camera = cameraGo.AddComponent<Camera>();

            var resolver = root.AddComponent<PlayerWeaponPickupResolver>();
            resolver.SetCameraForTests(camera);
            inventory.Configure(input, resolver, new PlayerInventoryRuntime());

            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupGo.transform.position = new Vector3(0f, 0f, 2f);
            var pickupTarget = pickupGo.AddComponent<WeaponPickupTarget>();
            pickupTarget.SetItemIdForTests("weapon-kar98k");

            input.PickupPressedThisFrame = true;
            inventory.Tick();
            yield return null;

            Assert.That(inventory.Runtime.BeltSlotItemIds[0], Is.EqualTo("weapon-kar98k"));
            Assert.That(pickupGo.activeSelf, Is.False);

            Object.Destroy(root);
            Object.Destroy(cameraGo);
            Object.Destroy(pickupGo);
        }

        [UnityTest]
        public IEnumerator PickupFlow_IgnoresBlockingColliderAndFindsPickupBehindIt()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var inventory = root.AddComponent<PlayerInventoryController>();

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.position = Vector3.zero;
            cameraGo.transform.forward = Vector3.forward;
            var camera = cameraGo.AddComponent<Camera>();

            var resolver = root.AddComponent<PlayerWeaponPickupResolver>();
            resolver.SetCameraForTests(camera);
            inventory.Configure(input, resolver, new PlayerInventoryRuntime());

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(0f, 0f, 1f);

            var pickupGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupGo.transform.position = new Vector3(0f, 0f, 2f);
            var pickupTarget = pickupGo.AddComponent<WeaponPickupTarget>();
            pickupTarget.SetItemIdForTests("weapon-kar98k");

            input.PickupPressedThisFrame = true;
            inventory.Tick();
            yield return null;

            Assert.That(inventory.Runtime.BeltSlotItemIds[0], Is.EqualTo("weapon-kar98k"));
            Assert.That(pickupGo.activeSelf, Is.False);

            Object.Destroy(root);
            Object.Destroy(cameraGo);
            Object.Destroy(blocker);
            Object.Destroy(pickupGo);
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool PickupPressedThisFrame;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;

            public bool ConsumePickupPressed()
            {
                if (!PickupPressedThisFrame)
                {
                    return false;
                }

                PickupPressedThisFrame = false;
                return true;
            }
        }
    }
}
