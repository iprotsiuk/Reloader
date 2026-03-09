using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using Reloader.Player;
using UnityEngine;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class DialogueConversationModePlayModeTests
    {
        [Test]
        public void EnterConversation_LocksMovementFocusesCameraAndUnlocksCursor()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;

            var root = new GameObject("PlayerRoot");
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

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

            var conversationMode = root.AddComponent<DialogueConversationModeController>();
            var npcTarget = new GameObject("NpcTarget");
            npcTarget.transform.position = new Vector3(10f, 1f, 0f);

            try
            {
                var startingPosition = root.transform.position;
                conversationMode.EnterConversation(npcTarget.transform);
                mover.Tick(0.1f);
                look.Tick(0.1f);

                var horizontalDisplacement = root.transform.position - startingPosition;
                horizontalDisplacement.y = 0f;
                Assert.That(horizontalDisplacement.sqrMagnitude, Is.LessThan(0.0001f));
                Assert.That(root.transform.eulerAngles.y, Is.EqualTo(90f).Within(0.5f));
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(npcTarget);
                Object.DestroyImmediate(root);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void ExitConversation_RestoresMovementAndCursorLock()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;

            var root = new GameObject("PlayerRoot");
            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.3f;

            var input = root.AddComponent<TestInputSource>();
            input.Move = Vector2.up;

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

            var conversationMode = root.AddComponent<DialogueConversationModeController>();
            var npcTarget = new GameObject("NpcTarget");
            npcTarget.transform.position = new Vector3(10f, 1f, 0f);

            try
            {
                conversationMode.EnterConversation(npcTarget.transform);
                conversationMode.ExitConversation();
                mover.Tick(0.1f);

                Assert.That(root.transform.position.z, Is.GreaterThan(0.2f));
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(npcTarget);
                Object.DestroyImmediate(root);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
        }

        [Test]
        public void RefreshConversationMode_WhenDialogueRuntimeIsActive_FollowsRuntimeSpeakerAndRestoresOnClose()
        {
            var previousLockState = Cursor.lockState;
            var previousVisible = Cursor.visible;

            var root = new GameObject("PlayerRoot");
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

            var runtime = root.AddComponent<DialogueRuntimeController>();
            var conversationMode = root.AddComponent<DialogueConversationModeController>();
            var definition = CreateDialogueDefinition(
                "dialogue.runtime",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Stay focused.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Understood.", string.Empty, string.Empty, string.Empty)
                    }));
            var npcTarget = new GameObject("NpcTarget");
            npcTarget.transform.position = new Vector3(6f, 1f, 0f);

            try
            {
                Assert.That(runtime.TryOpenConversation(definition, npcTarget.transform, out _), Is.True);

                conversationMode.RefreshConversationMode();
                mover.Tick(0.1f);
                look.Tick(0.1f);

                Assert.That(conversationMode.IsConversationActive, Is.True);
                Assert.That(conversationMode.ActiveFocusTarget, Is.SameAs(npcTarget.transform));
                Assert.That(root.transform.eulerAngles.y, Is.EqualTo(90f).Within(0.5f));
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));

                runtime.CloseConversation();
                conversationMode.RefreshConversationMode();

                Assert.That(conversationMode.IsConversationActive, Is.False);
                Assert.That(conversationMode.ActiveFocusTarget, Is.Null);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(npcTarget);
                Object.DestroyImmediate(root);
                Cursor.lockState = previousLockState;
                Cursor.visible = previousVisible;
            }
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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public Vector2 Move;
            public Vector2 Look;

            public Vector2 MoveInput => Move;
            public Vector2 LookInput => Look;
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
        }
    }
}
