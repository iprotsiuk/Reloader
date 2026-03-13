using System;
using System.Text;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.CompassHud
{
    public sealed class CompassHudViewBinder : IUiViewBinder, IDisposable
    {
        private const float DefaultLaneWidth = 260f;

        private VisualElement _root;
        private VisualElement _entriesRoot;
        private CompassHudUiState _lastState;
        private EventCallback<AttachToPanelEvent> _attachToPanelCallback;
        private EventCallback<DetachFromPanelEvent> _detachFromPanelCallback;
        private EventCallback<GeometryChangedEvent> _geometryChangedCallback;
        private IVisualElementScheduledItem _relayoutScheduler;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            TearDownResponsiveCallbacks();
            _root = root?.Q<VisualElement>("compass-hud__root") ?? root;
            _entriesRoot = root?.Q<VisualElement>("compass-hud__entries");
            RegisterRelayoutCallbacks();
        }

        public void Render(UiRenderState state)
        {
            if (state is not CompassHudUiState compassState)
            {
                return;
            }

            _lastState = compassState;
            ApplyState();
        }

        private void ApplyState()
        {
            if (_lastState == null)
            {
                return;
            }

            if (_root != null)
            {
                _root.style.display = _lastState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_entriesRoot == null || !_lastState.IsVisible)
            {
                _entriesRoot?.Clear();
                return;
            }

            _entriesRoot.Clear();
            var laneWidth = ResolveLaneWidth();
            var halfWidth = laneWidth * 0.5f;

            for (var i = 0; i < _lastState.Entries.Count; i++)
            {
                var entry = _lastState.Entries[i];
                if (!entry.IsVisible)
                {
                    continue;
                }

                var element = CreateEntryElement(entry, i);
                var normalized = Mathf.Clamp(entry.SignedAngleDeltaDegrees / _lastState.VisibleHalfAngleDegrees, -1f, 1f);
                element.style.left = halfWidth + (normalized * halfWidth);
                _entriesRoot.Add(element);
            }
        }

        private VisualElement CreateEntryElement(CompassHudUiState.EntryState entry, int index)
        {
            var entryKey = string.IsNullOrWhiteSpace(entry.Key) ? index.ToString() : entry.Key;
            var elementKey = BuildElementKey(entryKey);
            if (entry.Kind == CompassHudUiState.EntryKind.Marker)
            {
                var marker = new VisualElement
                {
                    name = $"compass-hud__entry-marker-{elementKey}",
                    viewDataKey = entryKey,
                    pickingMode = PickingMode.Ignore
                };
                marker.AddToClassList("compass-hud__entry");
                marker.AddToClassList("compass-hud__entry--marker");

                var label = new Label(entry.Label)
                {
                    name = $"compass-hud__entry-marker-label-{elementKey}",
                    viewDataKey = string.Concat(entryKey, ":label"),
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList("compass-hud__entry-marker-label");
                marker.Add(label);
                return marker;
            }

            var cardinal = new Label(entry.Label)
            {
                name = $"compass-hud__entry-cardinal-{elementKey}",
                viewDataKey = entryKey,
                pickingMode = PickingMode.Ignore
            };
            cardinal.AddToClassList("compass-hud__entry");
            cardinal.AddToClassList("compass-hud__entry--cardinal");
            return cardinal;
        }

        private static string BuildElementKey(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            var builder = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("X2"));
            }

            return builder.ToString();
        }

        private void RegisterRelayoutCallbacks()
        {
            if (_root == null)
            {
                return;
            }

            _attachToPanelCallback ??= _ => EnsureResponsiveLayoutSchedulerRunning();
            _detachFromPanelCallback ??= _ => _relayoutScheduler?.Pause();
            _geometryChangedCallback ??= _ => ApplyState();
            _root.RegisterCallback(_attachToPanelCallback);
            _root.RegisterCallback(_detachFromPanelCallback);
            _root.RegisterCallback(_geometryChangedCallback);
            if (_entriesRoot != null)
            {
                _entriesRoot.RegisterCallback(_geometryChangedCallback);
            }

            EnsureResponsiveLayoutSchedulerRunning();
        }

        public void Dispose()
        {
            TearDownResponsiveCallbacks();
        }

        private void EnsureResponsiveLayoutSchedulerRunning()
        {
            if (_root == null)
            {
                return;
            }

            if (_relayoutScheduler == null)
            {
                _relayoutScheduler = _root.schedule.Execute(ApplyState).Every(250);
                return;
            }

            _relayoutScheduler.Resume();
        }

        private void TearDownResponsiveCallbacks()
        {
            if (_root != null)
            {
                if (_attachToPanelCallback != null)
                {
                    _root.UnregisterCallback(_attachToPanelCallback);
                }

                if (_detachFromPanelCallback != null)
                {
                    _root.UnregisterCallback(_detachFromPanelCallback);
                }

                if (_geometryChangedCallback != null)
                {
                    _root.UnregisterCallback(_geometryChangedCallback);
                }
            }

            if (_entriesRoot != null && _geometryChangedCallback != null)
            {
                _entriesRoot.UnregisterCallback(_geometryChangedCallback);
            }

            _relayoutScheduler?.Pause();
            _relayoutScheduler = null;
        }

        private float ResolveLaneWidth()
        {
            if (_entriesRoot == null)
            {
                return DefaultLaneWidth;
            }

            var resolved = _entriesRoot.resolvedStyle.width;
            if (resolved > 1f)
            {
                return resolved;
            }

            var layoutWidth = _entriesRoot.layout.width;
            return layoutWidth > 1f ? layoutWidth : DefaultLaneWidth;
        }
    }
}
