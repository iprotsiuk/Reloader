using NUnit.Framework;
using Reloader.Reloading.Runtime;

namespace Reloader.Reloading.Tests.EditMode
{
    public class ReloadingFlowControllerEditModeTests
    {
        [Test]
        public void CompleteRound_HappyPath_CompletesOnSeatBullet()
        {
            var controller = new ReloadingFlowController();

            controller.TryApply(ReloadingOperationType.ResizeCase);
            controller.TryApply(ReloadingOperationType.PrimeCase);
            controller.TryApply(ReloadingOperationType.ChargePowder);
            var seat = controller.TryApply(ReloadingOperationType.SeatBullet);

            Assert.That(seat.Success, Is.True);
            Assert.That(controller.TryBuildCompletedRound(out var result), Is.True);
            Assert.That(result.IsRoundComplete, Is.True);
        }

        [Test]
        public void ChargeAfterSeat_Fails_WithPowderWaste()
        {
            var controller = BuildCompletedRoundController();

            var operation = controller.TryApply(ReloadingOperationType.ChargePowder);

            Assert.That(operation.Success, Is.False);
            Assert.That(operation.MaterialDelta.PowderGrainsWasted, Is.GreaterThan(0f));
            Assert.That(operation.TimeCostSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void SeatWithoutResize_Fails_ConsumesBullet()
        {
            var controller = new ReloadingFlowController();
            controller.TryApply(ReloadingOperationType.PrimeCase);
            controller.TryApply(ReloadingOperationType.ChargePowder);

            var seat = controller.TryApply(ReloadingOperationType.SeatBullet);

            Assert.That(seat.Success, Is.False);
            Assert.That(seat.MaterialDelta.BulletsConsumed, Is.EqualTo(1));
            Assert.That(seat.TimeCostSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void SkipInspectAndLube_StillCompletes_WithPenaltyFlags()
        {
            var controller = new ReloadingFlowController();
            controller.TryApply(ReloadingOperationType.ResizeCase);
            controller.TryApply(ReloadingOperationType.PrimeCase);
            controller.TryApply(ReloadingOperationType.ChargePowder);
            controller.TryApply(ReloadingOperationType.SeatBullet);

            var completed = controller.TryBuildCompletedRound(out var result);

            Assert.That(completed, Is.True);
            Assert.That(result.ConsequenceFlags, Does.Contain(MockConsequenceFlags.SkippedInspect));
            Assert.That(result.ConsequenceFlags, Does.Contain(MockConsequenceFlags.SkippedLube));
        }

        [Test]
        public void ChargeBeforePrime_Fails_DoesNotCompleteRound()
        {
            var controller = new ReloadingFlowController();
            controller.TryApply(ReloadingOperationType.ResizeCase);

            var charge = controller.TryApply(ReloadingOperationType.ChargePowder);

            Assert.That(charge.Success, Is.False);
            Assert.That(controller.TryBuildCompletedRound(out _), Is.False);
        }

        private static ReloadingFlowController BuildCompletedRoundController()
        {
            var controller = new ReloadingFlowController();
            controller.TryApply(ReloadingOperationType.ResizeCase);
            controller.TryApply(ReloadingOperationType.PrimeCase);
            controller.TryApply(ReloadingOperationType.ChargePowder);
            controller.TryApply(ReloadingOperationType.SeatBullet);
            return controller;
        }
    }
}
