using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class DialogueInitiationPlayModeTests
    {
        [Test]
        public void ProximityInitiator_PlayerNearby_UsesSharedRuntimeAndConversationMode()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;

            var root = new GameObject("PlayerRoot");
            root.transform.position = Vector3.zero;
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.SetParent(root.transform);

            var mover = root.AddComponent<PlayerMover>();
            mover.Configure(input, new PlayerMovementSettings());

            var look = root.AddComponent<PlayerLookController>();
            look.Configure(input, cameraPivot.transform);
            var cursor = root.AddComponent<PlayerCursorLockController>();
            cursor.LockCursor();

            root.AddComponent<PlayerNpcInteractionController>();
            var runtime = root.AddComponent<DialogueRuntimeController>();
            var conversationMode = root.AddComponent<DialogueConversationModeController>();

            var npc = new GameObject("NpcSpeaker");
            npc.transform.position = new Vector3(1.5f, 0f, 0f);
            var initiator = npc.AddComponent<DialogueProximityInitiator>();
            var definition = CreateDefinition(
                "dialogue.playmode",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Close enough.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(initiator, "_definition", definition);
            SetField(initiator, "_playerTransformOverride", root.transform);
            SetField(initiator, "_triggerDistanceMeters", 2f);

            try
            {
                initiator.Tick();
                conversationMode.RefreshConversationMode();
                look.Tick(0.1f);

                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(conversationMode.IsConversationActive, Is.True);
                Assert.That(conversationMode.ActiveFocusTarget, Is.SameAs(npc.transform));
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(npc);
                Object.DestroyImmediate(root);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        private static DialogueDefinition CreateDefinition(string dialogueId, string entryNodeId, params DialogueNodeDefinition[] nodes)
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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
        }
    }
}
