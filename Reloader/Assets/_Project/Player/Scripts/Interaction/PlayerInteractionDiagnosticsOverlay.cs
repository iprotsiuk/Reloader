using UnityEngine;

namespace Reloader.Player.Interaction
{
    public sealed class PlayerInteractionDiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionCoordinator _coordinator;
        [SerializeField] private bool _showOverlay = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F8;
        [SerializeField] private Vector2 _position = new Vector2(12f, 12f);
        [SerializeField] private Vector2 _size = new Vector2(560f, 72f);

        private GUIStyle _style;

        private void Awake()
        {
            if (_coordinator == null)
            {
                _coordinator = GetComponent<PlayerInteractionCoordinator>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showOverlay = !_showOverlay;
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay || _coordinator == null)
            {
                return;
            }

            _style ??= BuildStyle();
            var rect = new Rect(_position.x, _position.y, _size.x, _size.y);
            GUI.Box(rect, GUIContent.none);
            var textRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f);
            GUI.Label(textRect, _coordinator.CaptureDebugSnapshot(), _style);
        }

        private static GUIStyle BuildStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(0.94f, 0.97f, 1f, 0.95f)
                }
            };
        }
    }
}
