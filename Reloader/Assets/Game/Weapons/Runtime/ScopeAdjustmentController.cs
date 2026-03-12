using System;
using UnityEngine;

namespace Reloader.Game.Weapons
{
    public readonly struct ScopeAdjustmentSnapshot
    {
        public ScopeAdjustmentSnapshot(int windageClicks, int elevationClicks, int zeroSteps)
        {
            WindageClicks = windageClicks;
            ElevationClicks = elevationClicks;
            ZeroSteps = zeroSteps;
        }

        public int WindageClicks { get; }
        public int ElevationClicks { get; }
        public int ZeroSteps { get; }
    }

    public sealed class ScopeAdjustmentController : MonoBehaviour
    {
        [Header("Adjustment Limits")]
        [SerializeField] private int _minWindageClicks = -20;
        [SerializeField] private int _maxWindageClicks = 20;
        [SerializeField] private int _minElevationClicks = -20;
        [SerializeField] private int _maxElevationClicks = 20;
        [SerializeField] private int _minZeroSteps = -10;
        [SerializeField] private int _maxZeroSteps = 10;

        [Header("Defaults")]
        [SerializeField] private int _defaultWindageClicks;
        [SerializeField] private int _defaultElevationClicks;
        [SerializeField] private int _defaultZeroSteps;

        public event Action<ScopeAdjustmentSnapshot> AdjustmentChanged;

        public int CurrentWindageClicks { get; private set; }
        public int CurrentElevationClicks { get; private set; }
        public int CurrentZeroSteps { get; private set; }

        private void Awake()
        {
            ResetAdjustments();
        }

        public void ApplyWindageClicks(int clicks)
        {
            if (clicks == 0)
            {
                return;
            }

            CurrentWindageClicks = ClampWindage(CurrentWindageClicks + clicks);
            RaiseAdjustmentChanged();
        }

        public void AdjustWindageClicks(int clicks)
        {
            ApplyWindageClicks(clicks);
        }

        public void ApplyElevationClicks(int clicks)
        {
            if (clicks == 0)
            {
                return;
            }

            CurrentElevationClicks = ClampElevation(CurrentElevationClicks + clicks);
            RaiseAdjustmentChanged();
        }

        public void AdjustElevationClicks(int clicks)
        {
            ApplyElevationClicks(clicks);
        }

        public void ApplyZeroSteps(int steps)
        {
            if (steps == 0)
            {
                return;
            }

            CurrentZeroSteps = ClampZero(CurrentZeroSteps + steps);
            RaiseAdjustmentChanged();
        }

        public void AdjustZeroSteps(int steps)
        {
            ApplyZeroSteps(steps);
        }

        public ScopeAdjustmentSnapshot CaptureSnapshot()
        {
            return new ScopeAdjustmentSnapshot(CurrentWindageClicks, CurrentElevationClicks, CurrentZeroSteps);
        }

        public void RestoreSnapshot(ScopeAdjustmentSnapshot snapshot)
        {
            CurrentWindageClicks = ClampWindage(snapshot.WindageClicks);
            CurrentElevationClicks = ClampElevation(snapshot.ElevationClicks);
            CurrentZeroSteps = ClampZero(snapshot.ZeroSteps);
            RaiseAdjustmentChanged();
        }

        public void ConfigureLimits(
            int minWindageClicks,
            int maxWindageClicks,
            int minElevationClicks,
            int maxElevationClicks)
        {
            _minWindageClicks = minWindageClicks;
            _maxWindageClicks = maxWindageClicks;
            _minElevationClicks = minElevationClicks;
            _maxElevationClicks = maxElevationClicks;
            CurrentWindageClicks = ClampWindage(CurrentWindageClicks);
            CurrentElevationClicks = ClampElevation(CurrentElevationClicks);
            RaiseAdjustmentChanged();
        }

        public void ConfigureFromOptic(OpticDefinition optic)
        {
            if (optic == null)
            {
                return;
            }

            ConfigureLimits(
                optic.MinWindageClicks,
                optic.MaxWindageClicks,
                optic.MinElevationClicks,
                optic.MaxElevationClicks);
        }

        public void ResetAdjustments()
        {
            CurrentWindageClicks = ClampWindage(_defaultWindageClicks);
            CurrentElevationClicks = ClampElevation(_defaultElevationClicks);
            CurrentZeroSteps = ClampZero(_defaultZeroSteps);
            RaiseAdjustmentChanged();
        }

        private int ClampWindage(int value)
        {
            var min = Mathf.Min(_minWindageClicks, _maxWindageClicks);
            var max = Mathf.Max(_minWindageClicks, _maxWindageClicks);
            return Mathf.Clamp(value, min, max);
        }

        private int ClampElevation(int value)
        {
            var min = Mathf.Min(_minElevationClicks, _maxElevationClicks);
            var max = Mathf.Max(_minElevationClicks, _maxElevationClicks);
            return Mathf.Clamp(value, min, max);
        }

        private int ClampZero(int value)
        {
            var min = Mathf.Min(_minZeroSteps, _maxZeroSteps);
            var max = Mathf.Max(_minZeroSteps, _maxZeroSteps);
            return Mathf.Clamp(value, min, max);
        }

        private void RaiseAdjustmentChanged()
        {
            AdjustmentChanged?.Invoke(CaptureSnapshot());
        }
    }
}
