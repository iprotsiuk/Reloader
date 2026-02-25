using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Reloader.EditorTools
{
    public static class SceneMergeMaintenance
    {
        private const string ScenePath = "Assets/Scenes/MainWorld.unity";
        private const string ReflectionProbeOutputDir = "Assets/Scenes/MainWorld";
        private const string ReportPath = "Assets/Scenes/MainWorld/PostMergeReport.txt";
        private const string AutoNavMeshSurfaceObjectName = "__MainWorldNavMeshSurface_Auto";
        private const string AutoReflectionProbeObjectName = "__MainWorldReflectionProbe_Auto";

        [MenuItem("Tools/Reloader/Scene/Rebuild MainWorld + Post-Merge Checks")]
        public static void RebuildMainWorldFromMenu()
        {
            RunInternal(isBatchMode: false);
        }

        public static void RunMainWorldBatch()
        {
            RunInternal(isBatchMode: true);
        }

        private static void RunInternal(bool isBatchMode)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log($"[SceneMergeMaintenance] Opened scene: {scene.path}");

            var removedMissing = RemoveMissingScripts(scene);
            Debug.Log($"[SceneMergeMaintenance] Removed missing script components: {removedMissing}");

            var disabledDirectional = NormalizeDirectionalLights(scene);
            Debug.Log($"[SceneMergeMaintenance] Disabled extra directional lights: {disabledDirectional}");

            var reflectionProbeWasCreated = EnsureReflectionProbeExists(scene);
            if (reflectionProbeWasCreated)
            {
                Debug.Log("[SceneMergeMaintenance] Created auto reflection probe.");
            }

            var navSurfaceWasCreated = EnsureNavMeshSurfaceExists(scene);
            if (navSurfaceWasCreated)
            {
                Debug.Log("[SceneMergeMaintenance] Created auto NavMeshSurface.");
            }

            BakeLighting();
            BuildNavMesh(scene);
            BakeReflectionProbes(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.ForceReserializeAssets(new[] { ScenePath }, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var report = RunPostMergeChecks(scene, out var hasBlockingIssue);
            File.WriteAllText(ReportPath, report);
            AssetDatabase.ImportAsset(ReportPath);

            Debug.Log($"[SceneMergeMaintenance] Post-merge report written to {ReportPath}");
            Debug.Log(report);

            if (hasBlockingIssue)
            {
                throw new Exception("[SceneMergeMaintenance] Post-merge checks failed. See PostMergeReport.txt for details.");
            }

            if (isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void BakeLighting()
        {
            Debug.Log("[SceneMergeMaintenance] Baking lighting...");
            var bakeSucceeded = Lightmapping.Bake();
            if (!bakeSucceeded)
            {
                throw new Exception("[SceneMergeMaintenance] Lighting bake failed.");
            }

            Debug.Log("[SceneMergeMaintenance] Lighting bake complete.");
        }

        private static void BuildNavMesh(Scene scene)
        {
            Debug.Log("[SceneMergeMaintenance] Building navmesh...");
            var surfaces = GetNavMeshSurfaces(scene);
            if (surfaces.Count == 0)
            {
                throw new Exception("[SceneMergeMaintenance] No NavMeshSurface found for navmesh build.");
            }

            foreach (var surface in surfaces)
            {
                if (!surface.isActiveAndEnabled)
                {
                    continue;
                }

                surface.BuildNavMesh();
            }

            var triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
            {
                // Fallback for scenes where render meshes are excluded by setup.
                foreach (var surface in surfaces)
                {
                    if (!surface.isActiveAndEnabled)
                    {
                        continue;
                    }

                    surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                    surface.BuildNavMesh();
                }
            }

            Debug.Log("[SceneMergeMaintenance] Navmesh build complete.");
        }

        private static void BakeReflectionProbes(Scene scene)
        {
            var probes = new List<ReflectionProbe>();
            foreach (var root in scene.GetRootGameObjects())
            {
                probes.AddRange(root.GetComponentsInChildren<ReflectionProbe>(true));
            }

            if (probes.Count == 0)
            {
                Debug.Log("[SceneMergeMaintenance] No reflection probes found in scene.");
                return;
            }

            Directory.CreateDirectory(ReflectionProbeOutputDir);
            var bakedCount = 0;

            for (var i = 0; i < probes.Count; i++)
            {
                var probe = probes[i];
                if (probe.mode != UnityEngine.Rendering.ReflectionProbeMode.Baked)
                {
                    continue;
                }

                var outputPath = $"{ReflectionProbeOutputDir}/ReflectionProbe-{i}.exr";
                var ok = Lightmapping.BakeReflectionProbe(probe, outputPath);
                if (!ok)
                {
                    throw new Exception($"[SceneMergeMaintenance] Failed baking reflection probe '{probe.name}' to '{outputPath}'.");
                }

                bakedCount++;
            }

            Debug.Log($"[SceneMergeMaintenance] Reflection probe bake complete. Baked {bakedCount} probe(s).");
        }

        private static int RemoveMissingScripts(Scene scene)
        {
            var removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (count <= 0)
                    {
                        continue;
                    }

                    removed += count;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                }
            }

            return removed;
        }

        private static int NormalizeDirectionalLights(Scene scene)
        {
            var directionalLights = new List<Light>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLights.Add(light);
                    }
                }
            }

            if (directionalLights.Count == 0)
            {
                return 0;
            }

            Light keep = null;
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional)
            {
                keep = RenderSettings.sun;
            }

            if (keep == null)
            {
                keep = directionalLights[0];
            }

            keep.enabled = true;
            if (!keep.gameObject.activeSelf)
            {
                keep.gameObject.SetActive(true);
            }
            RenderSettings.sun = keep;

            var disabled = 0;
            foreach (var light in directionalLights)
            {
                if (light == keep)
                {
                    continue;
                }

                if (light.enabled)
                {
                    light.enabled = false;
                    disabled++;
                }
            }

            return disabled;
        }

        private static bool EnsureReflectionProbeExists(Scene scene)
        {
            var probes = new List<ReflectionProbe>();
            foreach (var root in scene.GetRootGameObjects())
            {
                probes.AddRange(root.GetComponentsInChildren<ReflectionProbe>(true));
            }

            if (probes.Count > 0)
            {
                return false;
            }

            var hasBounds = false;
            var combinedBounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds)
                    {
                        combinedBounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            var probeObject = new GameObject(AutoReflectionProbeObjectName);
            SceneManager.MoveGameObjectToScene(probeObject, scene);
            probeObject.transform.position = combinedBounds.center;

            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.boxProjection = true;
            probe.size = combinedBounds.size + Vector3.one * 20f;
            probe.center = Vector3.zero;
            probe.intensity = 1f;
            probe.importance = 1;

            return true;
        }

        private static bool EnsureNavMeshSurfaceExists(Scene scene)
        {
            var surfaces = GetNavMeshSurfaces(scene);
            if (surfaces.Count > 0)
            {
                return false;
            }

            var navObject = new GameObject(AutoNavMeshSurfaceObjectName);
            SceneManager.MoveGameObjectToScene(navObject, scene);
            var surface = navObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.layerMask = ~0;
            surface.defaultArea = 0;
            surface.overrideTileSize = false;
            surface.overrideVoxelSize = false;
            surface.ignoreNavMeshAgent = true;
            surface.ignoreNavMeshObstacle = true;

            return true;
        }

        private static List<NavMeshSurface> GetNavMeshSurfaces(Scene scene)
        {
            var surfaces = new List<NavMeshSurface>();
            foreach (var root in scene.GetRootGameObjects())
            {
                surfaces.AddRange(root.GetComponentsInChildren<NavMeshSurface>(true));
            }

            return surfaces;
        }

        private static string RunPostMergeChecks(Scene scene, out bool hasBlockingIssue)
        {
            var lines = new List<string>
            {
                "=== MainWorld Post-Merge Checks ===",
                $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                $"Scene: {scene.path}"
            };

            var roots = scene.GetRootGameObjects();
            var missingScriptCount = 0;
            var missingScriptObjects = new HashSet<string>();
            var missingMaterialRefs = 0;
            var nullShaderCount = 0;
            var unsupportedShaderCount = 0;
            var rendererCount = 0;
            var reflectionProbeCount = 0;
            var bakedProbeCount = 0;
            var directionalLightCount = 0;
            var directionalLightObjects = new List<string>();

            foreach (var root in roots)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    var components = go.GetComponents<Component>();
                    for (var i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingScriptCount++;
                            missingScriptObjects.Add(GetHierarchyPath(go.transform));
                        }
                    }
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    rendererCount++;
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        var mat = materials[i];
                        if (mat == null)
                        {
                            missingMaterialRefs++;
                            continue;
                        }

                        if (mat.shader == null)
                        {
                            nullShaderCount++;
                            continue;
                        }

                        if (!mat.shader.isSupported)
                        {
                            unsupportedShaderCount++;
                        }
                    }
                }

                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional && light.enabled && light.gameObject.activeInHierarchy)
                    {
                        directionalLightCount++;
                        directionalLightObjects.Add(GetHierarchyPath(light.transform));
                    }
                }

                foreach (var probe in root.GetComponentsInChildren<ReflectionProbe>(true))
                {
                    reflectionProbeCount++;
                    if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Baked)
                    {
                        bakedProbeCount++;
                    }
                }
            }

            var triangulation = NavMesh.CalculateTriangulation();
            var navMeshVertexCount = triangulation.vertices?.Length ?? 0;
            var navMeshTriangleIndexCount = triangulation.indices?.Length ?? 0;
            var lightmapCount = LightmapSettings.lightmaps?.Length ?? 0;
            var hasLightingData = Lightmapping.lightingDataAsset != null;

            lines.Add($"Missing scripts: {missingScriptCount}");
            lines.Add($"Renderers scanned: {rendererCount}");
            lines.Add($"Missing material refs: {missingMaterialRefs}");
            lines.Add($"Null shaders: {nullShaderCount}");
            lines.Add($"Unsupported shaders: {unsupportedShaderCount}");
            lines.Add($"Directional lights (enabled): {directionalLightCount}");
            if (directionalLightObjects.Count > 0)
            {
                lines.Add("Directional light objects:");
                foreach (var path in directionalLightObjects)
                {
                    lines.Add($"- {path}");
                }
            }

            lines.Add($"Reflection probes (total/baked): {reflectionProbeCount}/{bakedProbeCount}");
            lines.Add($"Lightmap count: {lightmapCount}");
            lines.Add($"Lighting data asset assigned: {hasLightingData}");
            lines.Add($"NavMesh vertices: {navMeshVertexCount}");
            lines.Add($"NavMesh triangle indices: {navMeshTriangleIndexCount}");

            if (missingScriptObjects.Count > 0)
            {
                lines.Add("GameObjects with missing scripts:");
                foreach (var path in missingScriptObjects)
                {
                    lines.Add($"- {path}");
                }
            }

            hasBlockingIssue =
                missingScriptCount > 0 ||
                missingMaterialRefs > 0 ||
                nullShaderCount > 0 ||
                unsupportedShaderCount > 0;

            lines.Add(hasBlockingIssue ? "Result: FAIL (blocking issues found)." : "Result: PASS (no blocking issues found).");

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            var current = transform;

            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", parts);
        }
    }
}
