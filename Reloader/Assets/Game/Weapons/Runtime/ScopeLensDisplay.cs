using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeLensDisplay : MonoBehaviour
    {
        private const float MinimumUvSpanForDirectSampling = 0.25f;
        private const float ProxySurfaceDepthOffset = 0.0005f;
        private const float ProxyEyepieceFillFactor = 0.96f;
        private const float ProxyFallbackTowardSightAnchorFraction = 0.6f;
        private const string SightAnchorName = "SightAnchor";

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private Material _displayMaterialTemplate;
        [SerializeField] private Vector3 _proxyDisplayLocalOffset;
        [SerializeField] private float _proxyDisplayScaleMultiplier = 0.95f;

        private MaterialPropertyBlock _propertyBlock;
        private Material[] _originalSharedMaterials;
        private Material _runtimeDisplayMaterial;
        private bool _displayMaterialApplied;
        private GameObject _proxySurface;
        private Renderer _proxyRenderer;
        private MeshFilter _proxyMeshFilter;
        private Mesh _runtimeProxyMesh;
        private bool _isUsingProxySurface;

        public Texture CurrentTexture { get; private set; }
        public bool IsUsingProxySurface => _isUsingProxySurface;

        private void Awake()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }
        }

        private void OnDestroy()
        {
            RestoreProxySurface();
            RestoreOriginalMaterials();
            ReleaseRuntimeProxyMesh();

            if (_runtimeDisplayMaterial != null)
            {
                Destroy(_runtimeDisplayMaterial);
                _runtimeDisplayMaterial = null;
            }
        }

        public bool TrySetTexture(Texture texture)
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = GetComponent<Renderer>();
            }

            if (_targetRenderer == null)
            {
                return false;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            if (texture == null)
            {
                RestoreProxySurface();
                RestoreOriginalMaterials();
                _propertyBlock.Clear();
            }
            else
            {
                if (ShouldUseProxySurface())
                {
                    RestoreOriginalMaterials();
                    if (EnsureProxySurface())
                    {
                        ApplyTextureToRenderer(_proxyRenderer, texture);
                        _proxyRenderer.enabled = true;
                        _targetRenderer.enabled = false;
                        _isUsingProxySurface = true;
                    }
                    else
                    {
                        RestoreProxySurface();
                        EnsureDisplayMaterial();
                        ApplyTextureToRenderer(_targetRenderer, texture);
                        _isUsingProxySurface = false;
                    }
                }
                else
                {
                    RestoreProxySurface();
                    EnsureDisplayMaterial();
                    ApplyTextureToRenderer(_targetRenderer, texture);
                    _isUsingProxySurface = false;
                }
            }

            if (!_isUsingProxySurface)
            {
                _targetRenderer.SetPropertyBlock(_propertyBlock);
            }

            CurrentTexture = texture;
            return true;
        }

        private void EnsureDisplayMaterial()
        {
            if (_displayMaterialApplied)
            {
                return;
            }

            _originalSharedMaterials = _targetRenderer.sharedMaterials;
            _runtimeDisplayMaterial ??= CreateDisplayMaterial();
            if (_runtimeDisplayMaterial == null)
            {
                return;
            }

            var materialCount = _originalSharedMaterials != null && _originalSharedMaterials.Length > 0
                ? _originalSharedMaterials.Length
                : 1;
            var displayMaterials = new Material[materialCount];
            for (var i = 0; i < materialCount; i++)
            {
                displayMaterials[i] = _runtimeDisplayMaterial;
            }

            _targetRenderer.sharedMaterials = displayMaterials;
            _displayMaterialApplied = true;
        }

        private void RestoreOriginalMaterials()
        {
            if (!_displayMaterialApplied || _targetRenderer == null)
            {
                return;
            }

            if (_originalSharedMaterials != null && _originalSharedMaterials.Length > 0)
            {
                _targetRenderer.sharedMaterials = _originalSharedMaterials;
            }

            _displayMaterialApplied = false;
        }

        private bool EnsureProxySurface()
        {
            if (_proxySurface != null &&
                _proxyRenderer != null &&
                _proxyMeshFilter != null &&
                _proxyMeshFilter.sharedMesh != null)
            {
                return true;
            }

            _runtimeDisplayMaterial ??= CreateDisplayMaterial();
            if (_runtimeDisplayMaterial == null)
            {
                return false;
            }

            if (_proxySurface == null)
            {
                _proxySurface = new GameObject("ScopeDisplayProxy");
            }

            var proxyParent = ResolveProxyParent();
            _proxySurface.transform.SetParent(proxyParent, false);
            _proxySurface.layer = _targetRenderer.gameObject.layer;

            _proxyMeshFilter ??= _proxySurface.GetComponent<MeshFilter>();
            if (_proxyMeshFilter == null)
            {
                _proxyMeshFilter = _proxySurface.AddComponent<MeshFilter>();
            }

            _proxyRenderer ??= _proxySurface.GetComponent<MeshRenderer>();
            if (_proxyRenderer == null)
            {
                _proxyRenderer = _proxySurface.AddComponent<MeshRenderer>();
            }

            var sourceMeshFilter = _targetRenderer.GetComponent<MeshFilter>();
            var sourceMesh = sourceMeshFilter != null ? sourceMeshFilter.sharedMesh : null;
            ReleaseRuntimeProxyMesh();
            var proxyMesh = CreateProxyMesh(sourceMesh);
            if (proxyMesh == null)
            {
                return false;
            }

            _proxyMeshFilter.sharedMesh = proxyMesh;
            _proxyRenderer.sharedMaterial = _runtimeDisplayMaterial;
            _proxyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _proxyRenderer.receiveShadows = false;
            _proxyRenderer.enabled = false;

            AlignProxySurface();
            return true;
        }

        private void RestoreProxySurface()
        {
            if (_proxyRenderer != null)
            {
                _proxyRenderer.enabled = false;
                _proxyRenderer.SetPropertyBlock(null);
            }

            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = true;
            }

            _isUsingProxySurface = false;
        }

        private void ReleaseRuntimeProxyMesh()
        {
            if (_runtimeProxyMesh == null)
            {
                return;
            }

            if (_proxyMeshFilter != null && _proxyMeshFilter.sharedMesh == _runtimeProxyMesh)
            {
                _proxyMeshFilter.sharedMesh = null;
            }

            Destroy(_runtimeProxyMesh);
            _runtimeProxyMesh = null;
        }

        private bool ShouldUseProxySurface()
        {
            if (_targetRenderer is not MeshRenderer meshRenderer)
            {
                return false;
            }

            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
            {
                return false;
            }

            if (!mesh.isReadable)
            {
                return true;
            }

            var uv = mesh.uv;
            if (uv == null || uv.Length == 0)
            {
                return true;
            }

            var minX = uv[0].x;
            var maxX = uv[0].x;
            var minY = uv[0].y;
            var maxY = uv[0].y;
            for (var i = 1; i < uv.Length; i++)
            {
                var sample = uv[i];
                minX = Mathf.Min(minX, sample.x);
                maxX = Mathf.Max(maxX, sample.x);
                minY = Mathf.Min(minY, sample.y);
                maxY = Mathf.Max(maxY, sample.y);
            }

            return maxX - minX < MinimumUvSpanForDirectSampling
                || maxY - minY < MinimumUvSpanForDirectSampling;
        }

        private void ApplyTextureToRenderer(Renderer renderer, Texture texture)
        {
            if (renderer == null)
            {
                return;
            }

            _propertyBlock.Clear();
            _propertyBlock.SetTexture(BaseMapId, texture);
            _propertyBlock.SetTexture(MainTexId, texture);
            _propertyBlock.SetColor(BaseColorId, Color.white);
            _propertyBlock.SetColor(ColorId, Color.white);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void AlignProxySurface()
        {
            if (_proxySurface == null || _targetRenderer == null)
            {
                return;
            }

            var meshFilter = _targetRenderer.GetComponent<MeshFilter>();
            var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            var sightAnchor = ResolveSightAnchor();
            if (TryAlignProxyToEyepiece(mesh, sightAnchor))
            {
                return;
            }

            if (sightAnchor != null)
            {
                var proxyScale = ResolveProxyScale(mesh);
                var lensToEye = sightAnchor.position - _targetRenderer.transform.position;
                var proxyWorldPosition = lensToEye.sqrMagnitude > 0.000001f
                    ? Vector3.Lerp(_targetRenderer.transform.position, sightAnchor.position, ProxyFallbackTowardSightAnchorFraction)
                    : _targetRenderer.transform.position;
                SetProxySurfaceTransform(proxyWorldPosition, _targetRenderer.transform.rotation, proxyScale);
                return;
            }

            if (mesh != null)
            {
                var bounds = mesh.bounds;
                var proxyWorldPosition = TransformProxyLocalPoint(bounds.center + new Vector3(0f, 0f, ProxySurfaceDepthOffset));
                SetProxySurfaceTransform(proxyWorldPosition, _targetRenderer.transform.rotation, ResolveProxyScale(mesh));
                return;
            }

            var fallbackWorldPosition = TransformProxyLocalPoint(new Vector3(0f, 0f, ProxySurfaceDepthOffset));
            SetProxySurfaceTransform(fallbackWorldPosition, _targetRenderer.transform.rotation, Vector3.one);
        }

        private bool TryAlignProxyToEyepiece(Mesh lensMesh, Transform sightAnchor)
        {
            if (_proxySurface == null || _targetRenderer == null || sightAnchor == null)
            {
                return false;
            }

            var proxyParent = _proxySurface.transform.parent;
            if (proxyParent == null)
            {
                return false;
            }

            var bodyMeshFilter = proxyParent.GetComponent<MeshFilter>();
            var bodyMesh = bodyMeshFilter != null ? bodyMeshFilter.sharedMesh : null;
            if (bodyMesh == null)
            {
                return false;
            }

            var eyeDirectionWorld = sightAnchor.position - _targetRenderer.transform.position;
            if (eyeDirectionWorld.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            var eyeDirectionLocal = proxyParent.InverseTransformDirection(eyeDirectionWorld).normalized;
            var lensLocalPosition = proxyParent.InverseTransformPoint(_targetRenderer.transform.position);
            var proxyLocalPosition = ResolveEyepieceLocalPosition(bodyMesh.bounds, lensLocalPosition, eyeDirectionLocal);
            var proxyScale = ResolveEyepieceScale(bodyMesh.bounds, lensMesh, eyeDirectionLocal);

            var proxyWorldPosition = proxyParent.TransformPoint(proxyLocalPosition + (eyeDirectionLocal * ProxySurfaceDepthOffset));
            SetProxySurfaceTransform(proxyWorldPosition, _targetRenderer.transform.rotation, proxyScale);
            return true;
        }

        private Transform ResolveProxyParent()
        {
            var sightAnchor = ResolveSightAnchor();
            if (sightAnchor != null && sightAnchor.parent != null)
            {
                return sightAnchor.parent;
            }

            return _targetRenderer.transform.parent != null
                ? _targetRenderer.transform.parent
                : _targetRenderer.transform;
        }

        private Transform ResolveSightAnchor()
        {
            var current = transform;
            while (current != null)
            {
                var transforms = current.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == SightAnchorName)
                    {
                        return transforms[i];
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private Vector3 ResolveProxyScale(Mesh mesh)
        {
            if (mesh == null)
            {
                return Vector3.one;
            }

            var lensPlaneSize = ResolveProjectedPlaneSize(mesh);
            return new Vector3(
                Mathf.Max(0.0001f, lensPlaneSize.x),
                Mathf.Max(0.0001f, lensPlaneSize.y),
                1f);
        }

        private Vector3 TransformProxyLocalPoint(Vector3 localPosition)
        {
            var proxyParent = _proxySurface != null ? _proxySurface.transform.parent : null;
            return proxyParent != null ? proxyParent.TransformPoint(localPosition) : localPosition;
        }

        private void SetProxySurfaceTransform(Vector3 worldPosition, Quaternion rotation, Vector3 localScale)
        {
            if (_proxySurface == null)
            {
                return;
            }

            _proxySurface.transform.SetPositionAndRotation(ApplyProxyLocalOffset(worldPosition), rotation);
            _proxySurface.transform.localScale = ApplyProxyScaleMultiplier(localScale);
        }

        private Vector3 ApplyProxyLocalOffset(Vector3 worldPosition)
        {
            if (_targetRenderer == null || _proxyDisplayLocalOffset == Vector3.zero)
            {
                return worldPosition;
            }

            return worldPosition
                + (_targetRenderer.transform.right * _proxyDisplayLocalOffset.x)
                + (_targetRenderer.transform.up * _proxyDisplayLocalOffset.y)
                + (_targetRenderer.transform.forward * _proxyDisplayLocalOffset.z);
        }

        private Vector3 ApplyProxyScaleMultiplier(Vector3 localScale)
        {
            var multiplier = Mathf.Max(0.01f, _proxyDisplayScaleMultiplier);
            return new Vector3(
                localScale.x * multiplier,
                localScale.y * multiplier,
                localScale.z);
        }

        private static Vector3 ResolveEyepieceLocalPosition(Bounds bodyBounds, Vector3 lensLocalPosition, Vector3 eyeDirectionLocal)
        {
            var min = bodyBounds.min;
            var max = bodyBounds.max;
            var clampedLensCenter = new Vector3(
                Mathf.Clamp(lensLocalPosition.x, min.x, max.x),
                Mathf.Clamp(lensLocalPosition.y, min.y, max.y),
                Mathf.Clamp(lensLocalPosition.z, min.z, max.z));

            var dominantAxis = ResolveDominantAxis(eyeDirectionLocal);
            return dominantAxis switch
            {
                Axis.X => new Vector3(eyeDirectionLocal.x >= 0f ? max.x : min.x, clampedLensCenter.y, clampedLensCenter.z),
                Axis.Y => new Vector3(clampedLensCenter.x, eyeDirectionLocal.y >= 0f ? max.y : min.y, clampedLensCenter.z),
                _ => new Vector3(clampedLensCenter.x, clampedLensCenter.y, eyeDirectionLocal.z >= 0f ? max.z : min.z),
            };
        }

        private Vector3 ResolveEyepieceScale(Bounds bodyBounds, Mesh lensMesh, Vector3 eyeDirectionLocal)
        {
            var lensScale = ResolveProxyScale(lensMesh);
            var dominantAxis = ResolveDominantAxis(eyeDirectionLocal);
            Vector2 bodyPlaneSize = dominantAxis switch
            {
                Axis.X => new Vector2(bodyBounds.size.z, bodyBounds.size.y),
                Axis.Y => new Vector2(bodyBounds.size.x, bodyBounds.size.z),
                _ => new Vector2(bodyBounds.size.x, bodyBounds.size.y),
            };

            var scaledBodyPlane = bodyPlaneSize * ProxyEyepieceFillFactor;
            var fitScale = Mathf.Min(
                scaledBodyPlane.x / Mathf.Max(0.0001f, lensScale.x),
                scaledBodyPlane.y / Mathf.Max(0.0001f, lensScale.y));
            var fittedPlaneSize = new Vector2(lensScale.x * fitScale, lensScale.y * fitScale);
            return new Vector3(
                Mathf.Max(lensScale.x, fittedPlaneSize.x),
                Mathf.Max(lensScale.y, fittedPlaneSize.y),
                1f);
        }

        private Vector2 ResolveProjectedPlaneSize(Mesh mesh)
        {
            if (mesh != null && mesh.isReadable && _targetRenderer != null)
            {
                var size = MeasureProjectedPlaneSize(mesh, _targetRenderer.transform);
                if (size.x > 0.0001f && size.y > 0.0001f)
                {
                    return size;
                }
            }

            return mesh != null ? ResolveProjectedPlaneSize(mesh.bounds) : Vector2.one;
        }

        private static Vector2 ResolveProjectedPlaneSize(Bounds bounds)
        {
            var flattenedAxis = ResolveFlattenedAxis(bounds);
            return flattenedAxis switch
            {
                Axis.X => new Vector2(bounds.size.z, bounds.size.y),
                Axis.Y => new Vector2(bounds.size.x, bounds.size.z),
                _ => new Vector2(bounds.size.x, bounds.size.y),
            };
        }

        private static Axis ResolveFlattenedAxis(Bounds bounds)
        {
            var size = bounds.size;
            if (size.x < size.y && size.x < size.z)
            {
                return Axis.X;
            }

            if (size.y < size.z)
            {
                return Axis.Y;
            }

            return Axis.Z;
        }

        private static Axis ResolveDominantAxis(Vector3 direction)
        {
            var absX = Mathf.Abs(direction.x);
            var absY = Mathf.Abs(direction.y);
            var absZ = Mathf.Abs(direction.z);

            if (absX > absY && absX > absZ)
            {
                return Axis.X;
            }

            if (absY > absZ)
            {
                return Axis.Y;
            }

            return Axis.Z;
        }

        private enum Axis
        {
            X,
            Y,
            Z
        }

        private Mesh CreateProxyMesh(Mesh sourceMesh)
        {
            if (sourceMesh != null && sourceMesh.isReadable)
            {
                var flattenedMesh = CreateFlattenedProxyMesh(sourceMesh);
                if (flattenedMesh != null)
                {
                    _runtimeProxyMesh = flattenedMesh;
                    return flattenedMesh;
                }
            }

            _runtimeProxyMesh = null;
            var temporaryQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                var meshFilter = temporaryQuad.GetComponent<MeshFilter>();
                return meshFilter != null ? meshFilter.sharedMesh : null;
            }
            finally
            {
                Destroy(temporaryQuad);
            }
        }

        private Mesh CreateFlattenedProxyMesh(Mesh sourceMesh)
        {
            var sourceVertices = sourceMesh.vertices;
            var sourceTriangles = sourceMesh.triangles;
            if (sourceVertices == null || sourceVertices.Length == 0 || sourceTriangles == null || sourceTriangles.Length == 0)
            {
                return null;
            }

            var projectedExtents = MeasureProjectedExtents(sourceMesh, _targetRenderer != null ? _targetRenderer.transform : null);
            var projectedMin = projectedExtents.min;
            var projectedSize = projectedExtents.size;
            var width = Mathf.Max(0.0001f, projectedSize.x);
            var height = Mathf.Max(0.0001f, projectedSize.y);

            var flattenedVertices = new Vector3[sourceVertices.Length];
            var flattenedUv = new Vector2[sourceVertices.Length];
            for (var i = 0; i < sourceVertices.Length; i++)
            {
                var projected = ProjectVertex(sourceVertices[i], _targetRenderer != null ? _targetRenderer.transform : null);
                var normalizedX = (projected.x - projectedMin.x) / width;
                var normalizedY = (projected.y - projectedMin.y) / height;
                flattenedVertices[i] = new Vector3(normalizedX - 0.5f, normalizedY - 0.5f, 0f);
                flattenedUv[i] = new Vector2(normalizedX, normalizedY);
            }

            var flattenedMesh = new Mesh
            {
                name = $"{sourceMesh.name}_ScopeProxy"
            };
            flattenedMesh.vertices = flattenedVertices;
            flattenedMesh.triangles = sourceTriangles;
            flattenedMesh.uv = flattenedUv;

            var normals = new Vector3[flattenedVertices.Length];
            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.forward;
            }

            flattenedMesh.normals = normals;
            flattenedMesh.RecalculateBounds();
            return flattenedMesh;
        }

        private static Vector2 MeasureProjectedPlaneSize(Mesh mesh, Transform targetTransform)
        {
            var projectedExtents = MeasureProjectedExtents(mesh, targetTransform);
            return projectedExtents.size;
        }

        private static (Vector2 min, Vector2 size) MeasureProjectedExtents(Mesh mesh, Transform targetTransform)
        {
            if (mesh == null || targetTransform == null || !mesh.isReadable)
            {
                return (Vector2.zero, Vector2.zero);
            }

            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                return (Vector2.zero, Vector2.zero);
            }

            var min = ProjectVertex(vertices[0], targetTransform);
            var max = min;
            for (var i = 1; i < vertices.Length; i++)
            {
                var projected = ProjectVertex(vertices[i], targetTransform);
                min = Vector2.Min(min, projected);
                max = Vector2.Max(max, projected);
            }

            return (min, max - min);
        }

        private static Vector2 ProjectVertex(Vector3 vertex, Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return Vector2.zero;
            }

            var world = targetTransform.TransformPoint(vertex);
            var center = targetTransform.TransformPoint(Vector3.zero);
            var relativeWorld = world - center;
            return new Vector2(
                Vector3.Dot(relativeWorld, targetTransform.right),
                Vector3.Dot(relativeWorld, targetTransform.up));
        }

        private Material CreateDisplayMaterial()
        {
            if (_displayMaterialTemplate != null)
            {
                var material = new Material(_displayMaterialTemplate);
                material.name = $"{_displayMaterialTemplate.name}_ScopeDisplay";
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            var runtimeMaterial = new Material(shader)
            {
                name = "RuntimeScopeDisplay"
            };
            if (runtimeMaterial.HasProperty(BaseColorId))
            {
                runtimeMaterial.SetColor(BaseColorId, Color.white);
            }

            if (runtimeMaterial.HasProperty(ColorId))
            {
                runtimeMaterial.SetColor(ColorId, Color.white);
            }

            return runtimeMaterial;
        }
    }
}
