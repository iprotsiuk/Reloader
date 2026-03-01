namespace Reloader.Reloading.Runtime
{
    public static class ReloadingWorkbenchUiContextStore
    {
        private static readonly object Gate = new object();
        private static ReloadingWorkbenchUiSnapshot _latest;

        public static void Publish(ReloadingWorkbenchUiSnapshot snapshot)
        {
            lock (Gate)
            {
                _latest = snapshot?.Clone();
            }
        }

        public static void Clear()
        {
            lock (Gate)
            {
                _latest = null;
            }
        }

        public static bool TryGetLatest(out ReloadingWorkbenchUiSnapshot snapshot)
        {
            lock (Gate)
            {
                if (_latest == null)
                {
                    snapshot = null;
                    return false;
                }

                snapshot = _latest.Clone();
                return true;
            }
        }
    }
}
