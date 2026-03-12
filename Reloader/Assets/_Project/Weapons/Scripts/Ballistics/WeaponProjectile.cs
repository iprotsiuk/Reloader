using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using UnityEngine;

namespace Reloader.Weapons.Ballistics
{
    public sealed class WeaponProjectile : MonoBehaviour
    {
        public interface IPathObserver
        {
            void RecordSegment(Vector3 startPoint, Vector3 endPoint);
            void Complete(Vector3 terminalPoint, bool didHit);
        }

        [SerializeField] private float _speed = 100f;
        [SerializeField] private float _gravityMultiplier = 1f;
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _despawnBelowWorldY = -500f;
        [SerializeField] private float _ballisticCoefficientG1 = 0.45f;
        [SerializeField] private float _dragCoefficient = 0.00012f;
        [SerializeField] private LayerMask _hitMask = ~0;
        [SerializeField] private bool _spawnImpactVfx = true;
        [SerializeField] private Color _impactVfxColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private float _impactVfxLifetimeSeconds = 5f;
        [SerializeField] private bool _spawnInFlightVisual = true;
        [SerializeField] private Color _projectileVisualColor = new Color(1f, 0.75f, 0.2f, 1f);
        [SerializeField] private ImpactAudioRouter _impactAudioRouter;

        private Vector3 _velocity;
        private string _itemId;
        private IWeaponEvents _weaponEvents;
        private bool _useRuntimeKernelWeaponEvents = true;
        private Collider[] _ignoredColliders = System.Array.Empty<Collider>();
        private Material _runtimeVisualMaterial;
        private Vector3 _sourcePoint;
        private IPathObserver _pathObserver;
        private bool _pathCompleted;
        public float InitialSpeedMetersPerSecond { get; private set; }
        public float CurrentSpeedMetersPerSecond => _velocity.magnitude;

        private void Awake()
        {
            _impactAudioRouter ??= ImpactAudioRouter.ResolveOrCreateRuntimeRouter();
            EnsureInFlightVisual();
            _velocity = transform.forward * _speed;
            _sourcePoint = transform.position;
        }

        private void OnDestroy()
        {
            if (_runtimeVisualMaterial != null)
            {
                Destroy(_runtimeVisualMaterial);
                _runtimeVisualMaterial = null;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            var stepDt = dt;
            _velocity += Physics.gravity * (_gravityMultiplier * stepDt);
            ApplyDrag(stepDt);
            var start = transform.position;
            var next = start + (_velocity * stepDt);
            var delta = next - start;

            if (TryResolveHit(start, delta, out var hit))
            {
                RecordObservedSegment(start, hit.point);
                transform.position = hit.point;
                var payload = new ProjectileImpactPayload(_itemId, hit.point, hit.normal, _damage, hit.collider.gameObject, _sourcePoint);
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(payload);
                SpawnImpactVfx(hit.point, hit.normal);
                _impactAudioRouter?.EmitImpact(hit.point, hit.collider);
                ResolveWeaponEvents()?.RaiseProjectileHit(_itemId, hit.point, _damage);
                CompleteObservedPath(hit.point, didHit: true);
                Destroy(gameObject);
                return;
            }

            RecordObservedSegment(start, next);
            transform.position = next;
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward = _velocity.normalized;
            }

            if (transform.position.y < _despawnBelowWorldY)
            {
                CompleteObservedPath(transform.position, didHit: false);
                Destroy(gameObject);
            }
        }

        public void Initialize(string itemId, Vector3 direction, float speed, float gravityMultiplier, float damage, float ballisticCoefficientG1 = 0.45f, Transform shooterRoot = null)
        {
            _itemId = itemId;
            _speed = speed;
            _gravityMultiplier = gravityMultiplier;
            _damage = damage;
            _ballisticCoefficientG1 = ballisticCoefficientG1;
            _velocity = direction.normalized * speed;
            _sourcePoint = transform.position;
            InitialSpeedMetersPerSecond = speed;
            _pathCompleted = false;
            _ignoredColliders = shooterRoot != null
                ? shooterRoot.GetComponentsInChildren<Collider>()
                : System.Array.Empty<Collider>();
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward = _velocity.normalized;
            }
        }

        public void Configure(IWeaponEvents weaponEvents = null)
        {
            _useRuntimeKernelWeaponEvents = weaponEvents == null;
            _weaponEvents = weaponEvents;
        }

        public void SetPathObserver(IPathObserver pathObserver)
        {
            _pathObserver = pathObserver;
            _pathCompleted = false;
        }

        private IWeaponEvents ResolveWeaponEvents()
        {
            return _useRuntimeKernelWeaponEvents ? RuntimeKernelBootstrapper.WeaponEvents : _weaponEvents;
        }

        private void ApplyDrag(float dt)
        {
            var speed = _velocity.magnitude;
            if (speed <= 0.0001f)
            {
                return;
            }

            var bc = Mathf.Max(0.01f, _ballisticCoefficientG1);
            var dragAcceleration = (_dragCoefficient / bc) * speed * speed;
            var speedDelta = dragAcceleration * dt;
            var nextSpeed = Mathf.Max(0f, speed - speedDelta);
            if (nextSpeed <= 0f)
            {
                _velocity = Vector3.zero;
                return;
            }

            _velocity = _velocity.normalized * nextSpeed;
        }

        private bool TryResolveHit(Vector3 start, Vector3 delta, out RaycastHit hit)
        {
            hit = default;
            var distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                return false;
            }

            var direction = delta / distance;
            var hits = Physics.RaycastAll(start, direction, distance, _hitMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (candidate.collider == null
                    || candidate.collider.isTrigger
                    || IsIgnoredCollider(candidate.collider))
                {
                    continue;
                }

                hit = candidate;
                return true;
            }

            return false;
        }

        private bool IsIgnoredCollider(Collider collider)
        {
            for (var i = 0; i < _ignoredColliders.Length; i++)
            {
                if (_ignoredColliders[i] == collider)
                {
                    return true;
                }
            }

            return false;
        }

        private void RecordObservedSegment(Vector3 startPoint, Vector3 endPoint)
        {
            if (_pathObserver == null || _pathCompleted)
            {
                return;
            }

            _pathObserver.RecordSegment(startPoint, endPoint);
        }

        private void CompleteObservedPath(Vector3 terminalPoint, bool didHit)
        {
            if (_pathObserver == null || _pathCompleted)
            {
                return;
            }

            _pathCompleted = true;
            _pathObserver.Complete(terminalPoint, didHit);
        }

        private void SpawnImpactVfx(Vector3 point, Vector3 normal)
        {
            if (!_spawnImpactVfx)
            {
                return;
            }

            var safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            var vfxGo = new GameObject("ProjectileImpactVfx");
            vfxGo.transform.position = point + (safeNormal * 0.005f);
            vfxGo.transform.rotation = Quaternion.LookRotation(safeNormal);

            var sparks = vfxGo.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.06f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.012f);
            main.startColor = _impactVfxColor;
            main.gravityModifier = 0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = sparks.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 16f;
            shape.radius = 0.01f;

            var colorOverLifetime = sparks.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(1f, 0.98f, 0.85f, 1f), 0f),
                        new GradientColorKey(_impactVfxColor, 0.4f),
                        new GradientColorKey(new Color(0.95f, 0.55f, 0.2f, 1f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            var sizeOverLifetime = sparks.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.75f),
                new Keyframe(1f, 0f)));

            var smokeGo = new GameObject("ImpactSmoke");
            smokeGo.transform.SetParent(vfxGo.transform, false);
            var smoke = smokeGo.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var smokeMain = smoke.main;
            smokeMain.playOnAwake = false;
            smokeMain.loop = false;
            smokeMain.duration = 0.5f;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.85f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            smokeMain.startColor = new Color(0.38f, 0.38f, 0.38f, 0.9f);
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var smokeEmission = smoke.emission;
            smokeEmission.enabled = true;
            smokeEmission.rateOverTime = 0f;
            smokeEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            var smokeShape = smoke.shape;
            smokeShape.enabled = true;
            smokeShape.shapeType = ParticleSystemShapeType.Hemisphere;
            smokeShape.radius = 0.015f;

            var smokeColor = smoke.colorOverLifetime;
            smokeColor.enabled = true;
            smokeColor.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.6f, 0.6f, 0.6f, 1f), 0f),
                        new GradientColorKey(new Color(0.22f, 0.22f, 0.22f, 1f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(0.4f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                });

            var flashGo = new GameObject("ImpactFlash");
            flashGo.transform.SetParent(vfxGo.transform, false);
            var flash = flashGo.AddComponent<ParticleSystem>();
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var flashMain = flash.main;
            flashMain.playOnAwake = false;
            flashMain.loop = false;
            flashMain.duration = 0.04f;
            flashMain.startLifetime = 0.04f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            flashMain.startColor = new Color(1f, 0.98f, 0.88f, 1f);
            flashMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var flashEmission = flash.emission;
            flashEmission.enabled = true;
            flashEmission.rateOverTime = 0f;
            flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var flashShape = flash.shape;
            flashShape.enabled = true;
            flashShape.shapeType = ParticleSystemShapeType.Sphere;
            flashShape.radius = 0.005f;

            sparks.Play(true);
            smoke.Play(true);
            flash.Play(true);
            Destroy(vfxGo, Mathf.Max(5f, _impactVfxLifetimeSeconds));
        }

        private void EnsureInFlightVisual()
        {
            if (!_spawnInFlightVisual || GetComponentInChildren<Renderer>() != null)
            {
                return;
            }

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "ProjectileVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.02f;
            var visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            var renderer = visual.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (renderer != null && shader != null)
            {
                _runtimeVisualMaterial = new Material(shader);
                if (_runtimeVisualMaterial.HasProperty("_BaseColor"))
                {
                    _runtimeVisualMaterial.SetColor("_BaseColor", _projectileVisualColor);
                }
                else if (_runtimeVisualMaterial.HasProperty("_Color"))
                {
                    _runtimeVisualMaterial.SetColor("_Color", _projectileVisualColor);
                }

                renderer.sharedMaterial = _runtimeVisualMaterial;
            }
        }
    }
}
