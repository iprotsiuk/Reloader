namespace Reloader.Player
{
    public static class ShotCameraGameplayState
    {
        private static int _activeDepth;

        public static bool IsActive => _activeDepth > 0;

        public static void PushActive()
        {
            _activeDepth++;
        }

        public static void PopActive()
        {
            if (_activeDepth > 0)
            {
                _activeDepth--;
            }
        }

        public static void Reset()
        {
            _activeDepth = 0;
        }
    }
}
