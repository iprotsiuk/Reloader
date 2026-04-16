using Reloader.NPCs.Combat;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.HealthHud
{
    public sealed class HealthHudController : MonoBehaviour, IUiController
    {
        private const float LowHealthThreshold = 0.35f;
        private const float CriticalHealthThreshold = 0.15f;
        private const float DamageFlashDurationSeconds = 0.18f;
        private const float ResolveRetryIntervalSeconds = 0.25f;

        [SerializeField] private HumanoidDamageReceiver _damageReceiver;

        private HumanoidDamageReceiver _subscribedDamageReceiver;
        private HealthHudViewBinder _viewBinder;
        private bool _pendingRefresh;
        private float _damageFlashUntilTime;
        private float _nextResolveAttemptAt;

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToDamageReceiver();
            QueueRefresh();
            Refresh();
        }

        private void LateUpdate()
        {
            var resolvedReceiverChanged = false;
            if (_damageReceiver == null && Time.unscaledTime >= _nextResolveAttemptAt)
            {
                _nextResolveAttemptAt = Time.unscaledTime + ResolveRetryIntervalSeconds;
                resolvedReceiverChanged = ResolveReferences();
            }

            var needsRefresh = _pendingRefresh || resolvedReceiverChanged;
            if (_damageFlashUntilTime > 0f)
            {
                needsRefresh = true;
                if (Time.unscaledTime > _damageFlashUntilTime)
                {
                    _damageFlashUntilTime = 0f;
                }
            }

            if (!needsRefresh)
            {
                return;
            }

            _pendingRefresh = false;
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromDamageReceiver();
            _pendingRefresh = false;
            _damageFlashUntilTime = 0f;
            _nextResolveAttemptAt = 0f;
        }

        public void SetDamageReceiver(HumanoidDamageReceiver damageReceiver)
        {
            SetDamageReceiverInternal(damageReceiver, queueRefresh: true);
        }

        public void SetViewBinder(HealthHudViewBinder binder)
        {
            _viewBinder = binder;
            QueueRefresh();
            Refresh();
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        public void Refresh()
        {
            if (_viewBinder == null)
            {
                _pendingRefresh = false;
                return;
            }

            _pendingRefresh = false;
            ResolveReferences();
            if (!IsReferenceAlive(_damageReceiver))
            {
                _damageReceiver = null;
                _damageFlashUntilTime = 0f;
                _viewBinder.Render(HealthHudUiState.Create(
                    "-- / --",
                    0f,
                    isVisible: false,
                    isLowHealth: false,
                    isCritical: false,
                    isDead: false,
                    isDamageFlashVisible: false));
                return;
            }

            var maxHealth = Mathf.Max(0.01f, _damageReceiver.MaxHealth);
            var currentHealth = Mathf.Clamp(_damageReceiver.CurrentHealth, 0f, maxHealth);
            var healthFraction = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            var isDead = _damageReceiver.IsDead || currentHealth <= 0f;
            var isLowHealth = !isDead && healthFraction <= LowHealthThreshold;
            var isCritical = isDead || healthFraction <= CriticalHealthThreshold;
            var damageFlashVisible = _damageFlashUntilTime > 0f && Time.unscaledTime <= _damageFlashUntilTime;
            var healthValueText = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)} ({Mathf.RoundToInt(healthFraction * 100f)}%)";

            _viewBinder.Render(HealthHudUiState.Create(
                healthValueText,
                healthFraction,
                isVisible: true,
                isLowHealth: isLowHealth,
                isCritical: isCritical,
                isDead: isDead,
                isDamageFlashVisible: damageFlashVisible));

            if (_damageFlashUntilTime > 0f && !damageFlashVisible)
            {
                _damageFlashUntilTime = 0f;
            }
        }

        private bool ResolveReferences()
        {
            var changed = false;
            if (!IsReferenceAlive(_damageReceiver))
            {
                changed |= SetDamageReceiverInternal(null, queueRefresh: false);
            }

            if (_damageReceiver == null)
            {
                var resolvedReceiver = HealthHudPlayerRootResolver.ResolvePlayerDamageReceiver();
                if (resolvedReceiver != null)
                {
                    changed |= SetDamageReceiverInternal(resolvedReceiver, queueRefresh: false);
                }
            }

            return changed;
        }

        private void HandleDamageStateChanged()
        {
            _damageFlashUntilTime = Time.unscaledTime + DamageFlashDurationSeconds;
            QueueRefresh();
        }

        private void HandleHealthStateChanged()
        {
            QueueRefresh();
        }

        private void SubscribeToDamageReceiver()
        {
            if (_damageReceiver == null || ReferenceEquals(_subscribedDamageReceiver, _damageReceiver))
            {
                return;
            }

            UnsubscribeFromDamageReceiver();
            _subscribedDamageReceiver = _damageReceiver;
            _subscribedDamageReceiver.HealthStateChanged += HandleHealthStateChanged;
            _subscribedDamageReceiver.ResultResolved += HandleDamageStateChanged;
            _subscribedDamageReceiver.Died += HandleDamageStateChanged;
        }

        private void UnsubscribeFromDamageReceiver()
        {
            var subscribedDamageReceiver = _subscribedDamageReceiver;
            if (ReferenceEquals(subscribedDamageReceiver, null))
            {
                return;
            }

            subscribedDamageReceiver.HealthStateChanged -= HandleHealthStateChanged;
            subscribedDamageReceiver.ResultResolved -= HandleDamageStateChanged;
            subscribedDamageReceiver.Died -= HandleDamageStateChanged;
            _subscribedDamageReceiver = null;
        }

        private void QueueRefresh()
        {
            _pendingRefresh = true;
        }

        private bool SetDamageReceiverInternal(HumanoidDamageReceiver damageReceiver, bool queueRefresh)
        {
            if (ReferenceEquals(_damageReceiver, damageReceiver) &&
                ReferenceEquals(_subscribedDamageReceiver, damageReceiver))
            {
                return false;
            }

            UnsubscribeFromDamageReceiver();
            _damageReceiver = damageReceiver;
            SubscribeToDamageReceiver();

            if (queueRefresh)
            {
                QueueRefresh();
            }

            return true;
        }

        private static bool IsReferenceAlive(UnityEngine.Object instance)
        {
            return instance != null;
        }
    }
}
