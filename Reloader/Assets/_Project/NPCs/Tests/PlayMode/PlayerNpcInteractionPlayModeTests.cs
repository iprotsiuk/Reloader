using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.NPCs.Tests.PlayMode
{
    public class PlayerNpcInteractionPlayModeTests
    {
        private const string FrontDeskDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_FrontDeskClerk.asset";
        private const string FrontDeskClerkPrefabPath = "Assets/_Project/NPCs/Prefabs/Roles/Npc_FrontDeskClerk.prefab";

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
            var dialogueDefinition = AttachCapability(npc.gameObject, expectedActionKey);
            GameObject dialogueRuntimeRoot = null;
            if (dialogueDefinition != null)
            {
                dialogueRuntimeRoot = new GameObject("DialogueRuntime");
                dialogueRuntimeRoot.AddComponent<DialogueRuntimeController>();
            }

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
                if (dialogueDefinition != null)
                {
                    Object.DestroyImmediate(dialogueDefinition);
                }

                if (dialogueRuntimeRoot != null)
                {
                    Object.DestroyImmediate(dialogueRuntimeRoot);
                }

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
            var dialogueDefinition = AttachDialogueCapabilityWithDefinition(npc.gameObject);
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
            var dialogueDefinition = AttachDialogueCapabilityWithDefinition(npc.gameObject);
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
                Object.DestroyImmediate(dialogueDefinition);
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

        [Test]
        public void TryGetInteractionCandidate_WhenConversationIsActive_ReturnsFalse()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            var conversationMode = root.AddComponent<DialogueConversationModeController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            var focusTarget = new GameObject("focus-target");

            try
            {
                conversationMode.EnterConversation(focusTarget.transform);

                var hasCandidate = controller.TryGetInteractionCandidate(out _);

                Assert.That(hasCandidate, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(focusTarget);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

        [Test]
        public void Tick_WhenConversationIsActive_DoesNotExecuteNpcAction()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            var conversationMode = root.AddComponent<DialogueConversationModeController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            var capability = npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            var focusTarget = new GameObject("focus-target");
            var raised = false;
            controller.InteractionProcessed += _ => raised = true;

            try
            {
                conversationMode.EnterConversation(focusTarget.transform);
                input.PickupPressedThisFrame = true;

                controller.Tick();

                Assert.That(raised, Is.False);
                Assert.That(capability.InteractionCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(focusTarget);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

        [Test]
        public void TryGetInteractionCandidate_WhenDialogueRuntimeHasActiveConversation_ReturnsFalse()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            var runtime = root.AddComponent<DialogueRuntimeController>();
            controller.Configure(input, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            var dialogueDefinition = AttachDialogueCapabilityWithDefinition(npc.gameObject);
            resolver.Target = npc;

            try
            {
                Assert.That(runtime.TryOpenConversation(dialogueDefinition, npc.transform, out _), Is.True);

                var hasCandidate = controller.TryGetInteractionCandidate(out _);

                Assert.That(hasCandidate, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(dialogueDefinition);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void FrontDeskClerkPrefab_InteractOpensDialogueAndConfirmingReplyProducesAuthoredOutcome()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrontDeskClerkPrefabPath);
            var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(FrontDeskDialogueAssetPath);
            Assert.That(prefab, Is.Not.Null, $"Expected prefab at {FrontDeskClerkPrefabPath}.");
            Assert.That(dialogueAsset, Is.Not.Null, $"Expected dialogue asset at {FrontDeskDialogueAssetPath}.");

            var root = new GameObject("PlayerRoot");
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.3f;
            root.transform.position = Vector3.zero;

            var input = root.AddComponent<TestInputSource>();
            var camera = root.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1f, 0f);
            camera.transform.forward = Vector3.forward;

            var resolver = root.AddComponent<PlayerNpcResolver>();
            resolver.SetCameraForTests(camera);
            var interactionController = root.AddComponent<PlayerNpcInteractionController>();
            interactionController.Configure(input, resolver);

            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings
            {
                WalkSpeed = 6f,
                SprintSpeed = 9f,
                Acceleration = 100f,
                Gravity = -25f,
                JumpHeight = 1.25f
            });

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            var cursor = root.AddComponent<PlayerCursorLockController>();
            cursor.LockCursor();

            var runtime = root.AddComponent<DialogueRuntimeController>();
            var conversationMode = root.AddComponent<DialogueConversationModeController>();

            var npcInstance = Object.Instantiate(prefab);
            var npcAgent = npcInstance.GetComponent<NpcAgent>();
            NpcActionExecutionResult interactionResult = default;
            var interactionRaised = false;
            interactionController.InteractionProcessed += result =>
            {
                interactionRaised = true;
                interactionResult = result;
            };

            try
            {
                Assert.That(npcAgent, Is.Not.Null);
                npcInstance.transform.position = new Vector3(0f, 0f, 2.5f);

                input.PickupPressedThisFrame = true;
                interactionController.Tick();
                conversationMode.RefreshConversationMode();

                Assert.That(interactionRaised, Is.True);
                Assert.That(interactionResult.Success, Is.True);
                Assert.That(interactionResult.ActionKey, Is.EqualTo(DialogueCapability.ActionKey));
                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(conversationMode.IsConversationActive, Is.True);
                Assert.That(conversationMode.ActiveFocusTarget, Is.EqualTo(npcInstance.transform));
                Assert.That(dialogueAsset.TryGetEntryNode(out var entryNode), Is.True);
                Assert.That(runtime.ActiveConversation.CurrentNode.SpeakerText, Is.EqualTo(entryNode.SpeakerText));
                Assert.That(runtime.ActiveConversation.CurrentNode.Replies.Length, Is.EqualTo(entryNode.Replies.Length));

                Assert.That(runtime.TrySelectReply(1), Is.True);
                Assert.That(runtime.TryConfirmSelectedReply(out var confirmResult), Is.True);
                conversationMode.RefreshConversationMode();

                Assert.That(confirmResult.Success, Is.True);
                Assert.That(runtime.HasActiveConversation, Is.False);
                Assert.That(conversationMode.IsConversationActive, Is.False);
                Assert.That(runtime.LastOutcome.ReplyId, Is.EqualTo("reply.frontdesk.lockers"));
                Assert.That(runtime.LastOutcome.ActionId, Is.EqualTo("dialogue.frontdesk.ask-lockers"));
                Assert.That(runtime.LastOutcome.Payload, Is.EqualTo("lockers"));
            }
            finally
            {
                Object.DestroyImmediate(cameraPivot);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npcInstance);
            }
        }
#endif

        private static NpcAgent CreateNpcWithCollider(string name, Vector3 position)
        {
            var npcGo = new GameObject(name);
            npcGo.transform.position = position;
            npcGo.AddComponent<SphereCollider>().radius = 0.4f;
            return npcGo.AddComponent<NpcAgent>();
        }

        private static DialogueDefinition AttachCapability(GameObject npc, string actionKey)
        {
            if (actionKey == DialogueCapability.ActionKey)
            {
                return AttachDialogueCapabilityWithDefinition(npc);
            }

            if (actionKey == FrontDeskInteractionCapability.ActionKey)
            {
                npc.AddComponent<FrontDeskInteractionCapability>();
                return null;
            }

            if (actionKey == EntryFeeInteractionCapability.ActionKey)
            {
                npc.AddComponent<EntryFeeInteractionCapability>();
                return null;
            }

            Assert.Fail("Unsupported action key in test setup: " + actionKey);
            return null;
        }

        private static DialogueDefinition AttachDialogueCapabilityWithDefinition(GameObject npc)
        {
            var capability = npc.AddComponent<DialogueCapability>();
            var definition = CreateDialogueDefinition(
                "dialogue.test.greeting",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Need something?",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(capability, "_definition", definition);
            return definition;
        }

        private static DialogueDefinition CreateDialogueDefinition(string dialogueId, string entryNodeId, params DialogueNodeDefinition[] nodes)
        {
            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            SetField(definition, "_dialogueId", dialogueId);
            SetField(definition, "_entryNodeId", entryNodeId);
            SetField(definition, "_nodes", nodes);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
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
