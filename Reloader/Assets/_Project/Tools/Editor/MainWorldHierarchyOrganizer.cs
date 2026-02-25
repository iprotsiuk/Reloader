using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainWorldHierarchyOrganizer
{
    private const string MenuPath = "Tools/Scene/Organize Active Scene Hierarchy";

    [MenuItem(MenuPath)]
    private static void OrganizeActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[MainWorldHierarchyOrganizer] No active loaded scene.");
            return;
        }

        var worldRoot = GetOrCreateRoot("_World", scene);
        var gameplayRoot = GetOrCreateRoot("_Gameplay", scene);
        var systemsRoot = GetOrCreateRoot("_Systems", scene);
        var playerRoot = GetOrCreateRoot("_Player", scene);
        var unsortedRoot = GetOrCreateRoot("_Unsorted", scene);

        var worldStatic = GetOrCreateChild(worldRoot, "Static");
        var worldProps = GetOrCreateChild(worldRoot, "Props");
        var worldVehicles = GetOrCreateChild(worldRoot, "Vehicles");
        var gameplayInteractables = GetOrCreateChild(gameplayRoot, "Interactables");
        var gameplaySpawnPoints = GetOrCreateChild(gameplayRoot, "SpawnPoints");

        var roots = scene.GetRootGameObjects();
        var movedCount = 0;

        foreach (var go in roots)
        {
            if (go == null || IsOrganizerRoot(go))
            {
                continue;
            }

            var targetParent = Classify(go.name,
                worldStatic,
                worldProps,
                worldVehicles,
                gameplayInteractables,
                gameplaySpawnPoints,
                systemsRoot,
                playerRoot,
                unsortedRoot);

            if (targetParent == null || go.transform.parent == targetParent.transform)
            {
                continue;
            }

            Undo.SetTransformParent(go.transform, targetParent.transform, "Organize Scene Hierarchy");
            movedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[MainWorldHierarchyOrganizer] Organized '{scene.name}'. Reparented {movedCount} root objects.");
    }

    private static GameObject Classify(
        string name,
        GameObject worldStatic,
        GameObject worldProps,
        GameObject worldVehicles,
        GameObject gameplayInteractables,
        GameObject gameplaySpawnPoints,
        GameObject systemsRoot,
        GameObject playerRoot,
        GameObject unsortedRoot)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return unsortedRoot;
        }

        var n = name.Trim();

        if (MatchesAny(n, "Vehicle", "Car", "Truck", "Van", "Bus", "Bike", "PitCap"))
        {
            return worldVehicles;
        }

        if (MatchesAny(n, "Player", "FPS", "Character", "CameraRig"))
        {
            return playerRoot;
        }

        if (MatchesAny(n, "Manager", "System", "EventSystem", "Lighting", "Directional Light", "PostProcess", "Volume", "Audio"))
        {
            return systemsRoot;
        }

        if (MatchesAny(n, "Spawn", "Respawn", "Checkpoint", "Waypoint", "Patrol"))
        {
            return gameplaySpawnPoints;
        }

        if (MatchesAny(n, "Door", "Pickup", "Loot", "Interact", "Trigger", "Container", "Chest", "NPC", "Quest", "Shop", "Workbench"))
        {
            return gameplayInteractables;
        }

        if (MatchesAny(n, "Ground", "Terrain", "Road", "Street", "Floor", "Wall", "Building", "House"))
        {
            return worldStatic;
        }

        if (LooksLikeAssetInstanceName(n))
        {
            return worldProps;
        }

        return unsortedRoot;
    }

    private static bool LooksLikeAssetInstanceName(string objectName)
    {
        // Typical DCC-exported names like Chair_02_A, Vehicle_03_A, etc.
        return Regex.IsMatch(objectName, @"^[A-Za-z]+(_[0-9]{1,3})?_[A-Za-z0-9]+$");
    }

    private static bool MatchesAny(string source, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (source.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject GetOrCreateRoot(string name, Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create Root '{name}'");
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static GameObject GetOrCreateChild(GameObject parent, string childName)
    {
        var child = parent.transform.Find(childName);
        if (child != null)
        {
            return child.gameObject;
        }

        var go = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(go, $"Create Group '{childName}'");
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static bool IsOrganizerRoot(GameObject go)
    {
        return string.Equals(go.name, "_World", StringComparison.Ordinal)
            || string.Equals(go.name, "_Gameplay", StringComparison.Ordinal)
            || string.Equals(go.name, "_Systems", StringComparison.Ordinal)
            || string.Equals(go.name, "_Player", StringComparison.Ordinal)
            || string.Equals(go.name, "_Unsorted", StringComparison.Ordinal);
    }
}
