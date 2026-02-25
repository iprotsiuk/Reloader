using System;

namespace Reloader.Reloading.Runtime
{
    public sealed class ReloadingFlowController
    {
        private const float SuccessfulActionSeconds = 4f;
        private const float FailedActionSeconds = 3f;
        private const float FailedChargePowderWasteGrains = 8f;

        private readonly MockReloadSessionState _state = new MockReloadSessionState();

        public MockReloadSessionState SessionState => _state;

        public MockOperationResult TryApply(ReloadingOperationType operation)
        {
            switch (operation)
            {
                case ReloadingOperationType.InspectCase:
                    _state.Inspected = true;
                    return Success(operation, "Case inspected.");

                case ReloadingOperationType.CleanCase:
                    _state.Cleaned = true;
                    return Success(operation, "Case cleaned.");

                case ReloadingOperationType.LubeCase:
                    _state.Lubed = true;
                    return Success(operation, "Case lubed.");

                case ReloadingOperationType.ResizeCase:
                    if (_state.BulletSeated)
                    {
                        return Failure(operation, "Cannot resize after bullet seating.");
                    }

                    _state.Resized = true;
                    return Success(operation, "Case resized.");

                case ReloadingOperationType.PrimeCase:
                    if (!_state.Resized)
                    {
                        return Failure(operation, "Case must be resized before priming.");
                    }

                    if (_state.BulletSeated)
                    {
                        return Failure(operation, "Cannot prime after bullet seating.");
                    }

                    _state.Primed = true;
                    return Success(operation, "Primer seated.");

                case ReloadingOperationType.ChargePowder:
                    if (_state.BulletSeated)
                    {
                        return Failure(operation, "Cannot charge powder after bullet seating.", new MockMaterialDelta
                        {
                            PowderGrainsWasted = FailedChargePowderWasteGrains
                        });
                    }

                    if (!_state.Primed)
                    {
                        return Failure(operation, "Case must be primed before charging.");
                    }

                    _state.Charged = true;
                    return Success(operation, "Powder charged.");

                case ReloadingOperationType.SeatBullet:
                    if (_state.BulletSeated)
                    {
                        return Failure(operation, "Bullet already seated.");
                    }

                    if (!_state.Resized)
                    {
                        return Failure(operation, "Cannot seat bullet: case not resized.", new MockMaterialDelta
                        {
                            BulletsConsumed = 1
                        });
                    }

                    if (!_state.Primed)
                    {
                        return Failure(operation, "Cannot seat bullet: case not primed.", new MockMaterialDelta
                        {
                            BulletsConsumed = 1
                        });
                    }

                    if (!_state.Charged)
                    {
                        return Failure(operation, "Cannot seat bullet: no powder charge.", new MockMaterialDelta
                        {
                            BulletsConsumed = 1
                        });
                    }

                    _state.BulletSeated = true;
                    return Success(operation, "Bullet seated. Round complete.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported operation type.");
            }
        }

        public bool TryBuildCompletedRound(out MockAmmoResult result)
        {
            if (!_state.BulletSeated)
            {
                result = null;
                return false;
            }

            result = new MockAmmoResult
            {
                IsRoundComplete = true,
                IsFunctional = true,
                MisfireChance = _state.Inspected ? 0.01f : 0.08f,
                PressureRisk = _state.Cleaned ? 0.04f : 0.10f,
                AccuracyPenalty = _state.Lubed ? 0.02f : 0.08f
            };

            if (!_state.Inspected)
            {
                result.ConsequenceFlags.Add(MockConsequenceFlags.SkippedInspect);
            }

            if (!_state.Cleaned)
            {
                result.ConsequenceFlags.Add(MockConsequenceFlags.SkippedClean);
            }

            if (!_state.Lubed)
            {
                result.ConsequenceFlags.Add(MockConsequenceFlags.SkippedLube);
            }

            return true;
        }

        private static MockOperationResult Success(ReloadingOperationType operation, string message)
        {
            return new MockOperationResult(true, operation, message, SuccessfulActionSeconds, default);
        }

        private static MockOperationResult Failure(ReloadingOperationType operation, string message, MockMaterialDelta materialDelta = default)
        {
            return new MockOperationResult(false, operation, message, FailedActionSeconds, materialDelta);
        }
    }
}
