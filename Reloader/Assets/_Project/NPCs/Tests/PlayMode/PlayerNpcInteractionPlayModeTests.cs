using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class PlayerNpcInteractionPlayModeTests
    {
        [Test]
        public void Resolver_LookingAtNpcAgent_ResolvesTarget()
        {
            var cameraRoot = new GameObject("CameraRoot");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.forward = Vector3.forward;

            var resolverRoot = new GameObject("ResolverRoot");
            var resolver = resolverRoot.AddComponent<PlayerNpcResolver>();
            resolver.SetCameraForTests(camera);

            var npc = CreateNpcWithCollider("npc-resolve", new Vector3(0f, 0f, 2.5f));

            try
            {
                var resolved = resolver.TryResolveNpcAgent(out var target);

                Assert.That(resolved, Is.True);
                Assert.That(target, Is.EqualTo(npc));
            }
            finally
            {
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(resolverRoot);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

        [Test]
        public void Resolver_WhenTargetIsShopVendor_IgnoresByDefault()
        {
            var cameraRoot = new GameObject("CameraRoot");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.forward = Vector3.forward;

            var resolverRoot = new GameObject("ResolverRoot");
            var resolver = resolverRoot.AddComponent<PlayerNpcResolver>();
            resolver.SetCameraForTests(camera);

            var npc = CreateNpcWithCollider("npc-vendor", new Vector3(0f, 0f, 2.5f));
            npc.gameObject.AddComponent<ShopVendorTarget>();

            try
            {
                var resolved = resolver.TryResolveNpcAgent(out var target);

                Assert.That(resolved, Is.False);
                Assert.That(target, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(resolverRoot);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

        [TestCase(DialogueCapability.ActionKey)]
        [TestCase(FrontDeskInteractionCapability.ActionKey)]
        [TestCase(EntryFeeInteractionCapability.ActionKey)]
        public void InteractInput_TargetedNpc_ExecutesExpectedAction(string expectedActionKey)
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();

            var camera = root.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.forward = Vector3.forward;

            var resolver = root.AddComponent<PlayerNpcResolver>();
            resolver.SetCameraForTests(camera);

            controller.Configure(input, resolver);

            var npc = CreateNpcWithCollider("npc-target", new Vector3(0f, 0f, 2.5f));
            AttachCapability(npc.gameObject, expectedActionKey);

            NpcActionExecutionResult interactionResult = default;
            var eventRaised = false;
            controller.InteractionProcessed += result =>
            {
                eventRaised = true;
                interactionResult = result;
            };

            try
            {
                input.PickupPressedThisFrame = true;
                controller.Tick();
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(eventRaised, Is.True);
            Assert.That(interactionResult.Success, Is.True);
            Assert.That(interactionResult.ActionKey, Is.EqualTo(expectedActionKey));
        }

        [Test]
        public void TryInteract_NoTarget_EmitsFailureWithoutThrowing()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(input, resolver);

            NpcActionExecutionResult interactionResult = default;
            var eventRaised = false;
            controller.InteractionProcessed += result =>
            {
                eventRaised = true;
                interactionResult = result;
            };

            try
            {
                Assert.DoesNotThrow(() => controller.TryInteract());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Assert.That(eventRaised, Is.True);
            Assert.That(interactionResult.Success, Is.False);
            Assert.That(interactionResult.Reason, Is.EqualTo("npc.interaction.no-target"));
        }

        [Test]
        public void InteractInput_TargetHasNoActions_EmitsFailureWithoutThrowing()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-no-actions").AddComponent<NpcAgent>();
            resolver.Target = npc;

            NpcActionExecutionResult interactionResult = default;
            var eventRaised = false;
            controller.InteractionProcessed += result =>
            {
                eventRaised = true;
                interactionResult = result;
            };

            try
            {
                input.PickupPressedThisFrame = true;
                Assert.DoesNotThrow(controller.Tick);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(eventRaised, Is.True);
            Assert.That(interactionResult.Success, Is.False);
            Assert.That(interactionResult.Reason, Is.EqualTo("npc.interaction.no-actions"));
        }

        [Test]
        public void TryInteract_UnknownExplicitActionKey_EmitsFailure()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            NpcActionExecutionResult interactionResult = default;
            var eventRaised = false;
            controller.InteractionProcessed += result =>
            {
                eventRaised = true;
                interactionResult = result;
            };

            try
            {
                var interacted = controller.TryInteract("npc.action.unknown", "payload:test");
                Assert.That(interacted, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(eventRaised, Is.True);
            Assert.That(interactionResult.Success, Is.False);
            Assert.That(interactionResult.ActionKey, Is.EqualTo("npc.action.unknown"));
            Assert.That(interactionResult.Reason, Is.EqualTo("npc.action.unhandled"));
        }

        [Test]
        public void Tick_TargetedNpc_EmitsHintWithDefaultActionVerb()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            InteractionHintPayload hinted = default;
            runtimeHub.OnInteractionHintShown += payload => hinted = payload;

            try
            {
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(hinted.ContextId, Is.EqualTo("npc"));
            Assert.That(hinted.ActionText, Is.EqualTo("Talk"));
        }

        [Test]
        public void Tick_NoNpcTargetAfterHint_ClearsInteractionHint()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var runtimeHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeHub;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            var clearCount = 0;
            runtimeHub.OnInteractionHintCleared += () => clearCount++;

            try
            {
                controller.Tick();
                resolver.Target = null;
                controller.Tick();
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(clearCount, Is.GreaterThanOrEqualTo(1));
        }

        private static NpcAgent CreateNpcWithCollider(string name, Vector3 position)
        {
            var npcGo = new GameObject(name);
            npcGo.transform.position = position;
            npcGo.AddComponent<SphereCollider>().radius = 0.4f;
            return npcGo.AddComponent<NpcAgent>();
        }

        private static void AttachCapability(GameObject npc, string actionKey)
        {
            if (actionKey == DialogueCapability.ActionKey)
            {
                npc.AddComponent<DialogueCapability>();
                return;
            }

            if (actionKey == FrontDeskInteractionCapability.ActionKey)
            {
                npc.AddComponent<FrontDeskInteractionCapability>();
                return;
            }

            if (actionKey == EntryFeeInteractionCapability.ActionKey)
            {
                npc.AddComponent<EntryFeeInteractionCapability>();
                return;
            }

            Assert.Fail("Unsupported action key in test setup: " + actionKey);
        }

        private sealed class TestNpcResolver : MonoBehaviour, IPlayerNpcResolver
        {
            public NpcAgent Target { get; set; }

            public bool TryResolveNpcAgent(out NpcAgent target)
            {
                target = Target;
                return target != null;
            }
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
