using UnityEngine;

namespace Reloader.World
{
    [CreateAssetMenu(menuName = "Reloader/World/Main Town Terrain Generator Preset", fileName = "MainTownTerrainGeneratorPreset")]
    public sealed class MainTownTerrainGeneratorPreset : ScriptableObject
    {
        [SerializeField] private int seed = 1337;
        [SerializeField] private bool rerollSeedOnRegenerate = true;
        [SerializeField] private float waterLevelMeters = 20f;
        [SerializeField] private float terrainWidthMeters = 3000f;
        [SerializeField] private float terrainDepthMeters = 4000f;
        [SerializeField] private float terrainHeightMeters = 1100f;
        [SerializeField] private int heightmapResolution = 1025;
        [SerializeField] private int alphamapResolution = 1024;
        [SerializeField] private int baseMapResolution = 1024;
        [SerializeField] private int detailResolution = 1024;
        [SerializeField] private int detailResolutionPerPatch = 16;
        [SerializeField] private float landFootprintMeters = 2350f;
        [SerializeField] private float shorelineFalloffMeters = 340f;
        [SerializeField] private float landCoverage = 0.82f;
        [SerializeField] private float coastlineBreakupStrength = 0.45f;
        [SerializeField] private float satelliteChance = 0.35f;
        [SerializeField] private float satelliteSizeRatio = 0.28f;
        [SerializeField] private float hillsAmplitudeMeters = 90f;
        [SerializeField] private float mountainAmplitudeMeters = 430f;
        [SerializeField] private float cliffSharpening = 1.45f;
        [SerializeField] private int riverCount = 1;
        [SerializeField] private float riverHalfWidthMeters = 95f;
        [SerializeField] private float riverDepthMeters = 115f;
        [SerializeField] private float riverMeanderMeters = 220f;
        [SerializeField] private float beachBlendMeters = 10f;
        [SerializeField] private float detailAmplitudeMeters = 18f;
        [SerializeField] private float detailNoiseFrequency = 0.0032f;
        [SerializeField] private float pitAmplitudeMeters = 10f;
        [SerializeField] private float pitNoiseFrequency = 0.0044f;
        [SerializeField] private float cliffDetailStrengthMeters = 34f;
        [SerializeField] private float cliffDetailFrequency = 0.0038f;
        [SerializeField] private bool autoRepaintLayersOnHeightChange = true;
        [SerializeField] private float sandBandOffsetMeters = 4f;
        [SerializeField] private float sandBandWidthMeters = 16f;
        [SerializeField] private float sandMaxSlopeDegrees = 26f;
        [SerializeField] private float rockSlopeStartDegrees = 34f;
        [SerializeField] private float rockSlopeFullDegrees = 55f;
        [SerializeField] private float rockHeightStartMeters = 125f;
        [SerializeField] private float rockHeightFullMeters = 240f;
    }
}
