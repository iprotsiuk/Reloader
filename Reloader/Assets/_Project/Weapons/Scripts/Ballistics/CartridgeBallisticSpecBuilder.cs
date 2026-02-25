using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public static class CartridgeBallisticSpecBuilder
    {
        public static CartridgeBallisticSpec Build(AmmoBallisticSnapshot snapshot, float rngSample01)
        {
            var stdDev = Mathf.Max(0f, snapshot.VelocityStdDevFps);
            var z = (Mathf.Clamp01(rngSample01) - 0.5f) * 2f;
            var sampledVelocity = Mathf.Max(1f, snapshot.MuzzleVelocityFps + (z * stdDev));
            var projectileMassGrains = Mathf.Max(1f, snapshot.ProjectileMassGrains);
            var ballisticCoefficient = Mathf.Max(0.01f, snapshot.BallisticCoefficientG1);
            var dispersionMoa = Mathf.Max(0f, snapshot.DispersionMoa);

            return new CartridgeBallisticSpec(sampledVelocity, projectileMassGrains, ballisticCoefficient, dispersionMoa);
        }
    }
}
