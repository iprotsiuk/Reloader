using UnityEngine;

namespace Reloader.Player
{
    public static class ShotCameraGameplayState
    {
        private static int _activeDepth;
        private static Camera _presentationCamera;

        public static bool IsActive => _activeDepth > 0;
        public static Camera PresentationCamera => _presentationCamera;

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
            _presentationCamera = null;
        }

        public static void SetPresentationCamera(Camera presentationCamera)
        {
            _presentationCamera = presentationCamera;
        }

        public static void ClearPresentationCamera(Camera presentationCamera)
        {
            if (_presentationCamera == presentationCamera)
            {
                _presentationCamera = null;
            }
        }
    }
}
