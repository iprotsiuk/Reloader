using UnityEngine;
using UnityEngine.UI;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeMaskController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _maskRoot;
        [SerializeField] private Graphic[] _outsideDarkenGraphics;
        [SerializeField] private Image _reticleImage;
        [SerializeField] private RectTransform _reticleTransform;

        [Header("Reticle Scaling")]
        [SerializeField] private float _minMagnification = 1f;
        [SerializeField] private float _maxMagnification = 40f;
        [SerializeField] private float _reticleScaleAt1x = 1f;
        [SerializeField] private float _reticleScaleAt40x = 0.4f;
        private bool _lastVisible;
        private float _lastAlpha = -1f;
        private float _lastReticleScale = -1f;
        private Sprite _lastReticleSprite;
        private bool _hasAppliedState;

        public void SetReticleSprite(Sprite reticleSprite)
        {
            if (_reticleImage == null)
            {
                return;
            }

            if (_lastReticleSprite == reticleSprite)
            {
                return;
            }

            _lastReticleSprite = reticleSprite;
            _reticleImage.sprite = reticleSprite;
            _reticleImage.enabled = reticleSprite != null;
        }

        public void SetState(bool enabled, float magnification, float adsT)
        {
            var alpha = enabled ? Mathf.Clamp01(adsT) : 0f;
            if (_canvasGroup != null)
            {
                if (!Mathf.Approximately(_lastAlpha, alpha))
                {
                    _canvasGroup.alpha = alpha;
                    _lastAlpha = alpha;
                }

                if (_canvasGroup.interactable)
                {
                    _canvasGroup.interactable = false;
                }

                if (_canvasGroup.blocksRaycasts)
                {
                    _canvasGroup.blocksRaycasts = false;
                }
            }

            if (_maskRoot != null)
            {
                if (!_hasAppliedState || _lastVisible != enabled)
                {
                    _maskRoot.gameObject.SetActive(enabled);
                }
            }

            if (_outsideDarkenGraphics != null)
            {
                for (var i = 0; i < _outsideDarkenGraphics.Length; i++)
                {
                    var graphic = _outsideDarkenGraphics[i];
                    if (graphic == null)
                    {
                        continue;
                    }

                    if (!_hasAppliedState || _lastVisible != enabled)
                    {
                        graphic.gameObject.SetActive(enabled);
                    }

                    if (!Mathf.Approximately(graphic.color.a, alpha))
                    {
                        var color = graphic.color;
                        color.a = alpha;
                        graphic.color = color;
                    }
                }
            }

            if (_reticleTransform != null)
            {
                var magT = Mathf.InverseLerp(_minMagnification, _maxMagnification, Mathf.Clamp(magnification, _minMagnification, _maxMagnification));
                var scale = Mathf.Lerp(_reticleScaleAt1x, _reticleScaleAt40x, magT);
                var finalScale = Mathf.Max(0.01f, scale);
                if (!Mathf.Approximately(_lastReticleScale, finalScale))
                {
                    _reticleTransform.localScale = Vector3.one * finalScale;
                    _lastReticleScale = finalScale;
                }
            }

            _lastVisible = enabled;
            _hasAppliedState = true;
        }
    }
}
