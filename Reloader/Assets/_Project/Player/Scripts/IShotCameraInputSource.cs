namespace Reloader.Player
{
    public interface IShotCameraInputSource
    {
        bool ShotCameraSpeedUpHeld { get; }
        bool ConsumeShotCameraCancelPressed();
    }
}
