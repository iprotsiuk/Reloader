using System.IO;
using System.Reflection;

namespace Reloader.Player
{
    public interface IPlayerRecoveryTravelCoordinator
    {
        bool TryTravelToSceneEntry(string sceneName, string entryPointId);
        bool TryMoveRuntimePlayerToLoadedEntryPoint(string scenePath, string entryPointId);
    }

    internal sealed class WorldPlayerRecoveryTravelCoordinator : IPlayerRecoveryTravelCoordinator
    {
        public static WorldPlayerRecoveryTravelCoordinator Instance { get; } = new WorldPlayerRecoveryTravelCoordinator();

        public bool TryTravelToSceneEntry(string sceneName, string entryPointId)
        {
            var coordinatorType = ResolveCoordinatorType();
            var method = coordinatorType?.GetMethod("TryLoadSceneAtEntry", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            return (bool)method.Invoke(null, new object[] { sceneName, entryPointId });
        }

        public bool TryMoveRuntimePlayerToLoadedEntryPoint(string scenePath, string entryPointId)
        {
            var coordinatorType = ResolveCoordinatorType();
            var method = coordinatorType?.GetMethod("TryMoveRuntimePlayerToLoadedEntryPoint", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            return (bool)method.Invoke(null, new object[] { scenePath, entryPointId });
        }

        public static string GetSceneNameFromPath(string scenePath)
        {
            return string.IsNullOrWhiteSpace(scenePath)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(scenePath.Trim());
        }

        private static System.Type ResolveCoordinatorType()
        {
            return System.Type.GetType("Reloader.World.Travel.WorldTravelCoordinator, Reloader.World");
        }
    }
}
