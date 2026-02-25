using System;
using NUnit.Framework;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Tests.EditMode
{
    public class UiContractGuardTests
    {
        [Test]
        public void UiIntent_WhenKeyIsBlank_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new UiIntent(" "));
        }

        [Test]
        public void UiIntent_WhenCreated_StoresKeyAndPayload()
        {
            var intent = new UiIntent("inventory.slot.primary", 3);

            Assert.That(intent.Key, Is.EqualTo("inventory.slot.primary"));
            Assert.That(intent.Payload, Is.EqualTo(3));
        }

        [Test]
        public void UiContractGuard_BindRoutesIntentFromViewToController()
        {
            var view = new TestBinder();
            var controller = new TestController();

            using var binding = UiContractGuard.Bind(controller, view);
            view.Emit(new UiIntent("inventory.slot.primary", 2));

            Assert.That(controller.ReceivedCount, Is.EqualTo(1));
            Assert.That(controller.LastIntent.Key, Is.EqualTo("inventory.slot.primary"));
        }

        [Test]
        public void UiContractGuard_TryResolveCommand_WhenMissingActionMap_ReturnsFalse()
        {
            var map = new Reloader.UI.Toolkit.Runtime.UiActionMapConfig();
            map.Set("inventory.slot.primary", "InspectItem");

            var resolved = UiContractGuard.TryResolveCommand(new UiIntent("inventory.slot.secondary"), map, out var commandName);

            Assert.That(resolved, Is.False);
            Assert.That(commandName, Is.Null);
        }

        private sealed class TestBinder : IUiViewBinder
        {
            public event Action<UiIntent> IntentRaised;

            public UiRenderState LastState { get; private set; }

            public void Emit(UiIntent intent)
            {
                IntentRaised?.Invoke(intent);
            }

            public void Render(UiRenderState state)
            {
                LastState = state;
            }
        }

        private sealed class TestController : IUiController
        {
            public int ReceivedCount { get; private set; }
            public UiIntent LastIntent { get; private set; }

            public void HandleIntent(UiIntent intent)
            {
                ReceivedCount++;
                LastIntent = intent;
            }
        }
    }
}
