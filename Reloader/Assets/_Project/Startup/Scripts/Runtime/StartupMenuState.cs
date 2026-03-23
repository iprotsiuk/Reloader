using System.IO;

namespace Reloader.Startup.Runtime
{
    public sealed class StartupMenuState
    {
        public static StartupMenuState Empty { get; } = new(string.Empty, string.Empty, false, "No save found.");

        public StartupMenuState(string latestSavePath, string currentScenePath, bool canContinue, string statusMessage, string currentAnchorId = "")
        {
            LatestSavePath = latestSavePath ?? string.Empty;
            CurrentScenePath = currentScenePath ?? string.Empty;
            CurrentAnchorId = currentAnchorId ?? string.Empty;
            CanContinue = canContinue;
            StatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "No save found." : statusMessage;
        }

        public string LatestSavePath { get; }

        public string CurrentScenePath { get; }

        public string CurrentSceneName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentScenePath))
                {
                    return string.Empty;
                }

                return Path.GetFileNameWithoutExtension(CurrentScenePath.Trim());
            }
        }

        public string CurrentAnchorId { get; }

        public bool CanContinue { get; }

        public string StatusMessage { get; }
    }
}
