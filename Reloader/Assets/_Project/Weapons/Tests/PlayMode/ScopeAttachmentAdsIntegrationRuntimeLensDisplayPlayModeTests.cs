using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScopeLensDisplay_CollapsedUvMesh_UsesProxySurfaceInsteadOfSamplingSingleTexel()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayTestRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);
            lensDisplayGo.layer = 23;
            lensDisplayGo.transform.localPosition = new Vector3(0f, 0f, -0.006f);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var sourceMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            var collapsedUvMesh = UnityEngine.Object.Instantiate(sourceMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True,
                    "Collapsed lens UVs should trigger the runtime proxy surface so PiP does not collapse into a solid color.");
                Assert.That(meshRenderer.enabled, Is.False, "The broken authored renderer should be hidden while the proxy surface is active.");
                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Proxy display surface should be created when collapsed UVs require fallback rendering.");
                Assert.That(proxySurface.gameObject.layer, Is.EqualTo(lensDisplayGo.layer),
                    "Proxy display surface must inherit the lens layer so scope cameras that exclude the viewmodel layer do not recursively render it.");
                Assert.That(
                    Vector3.Distance(proxySurface.position, sightAnchor.transform.position),
                    Is.LessThan(Vector3.Distance(proxySurface.position, lensDisplayGo.transform.position)),
                    "Proxy display surface should be placed near the authored sight anchor on the eyepiece side.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_PartialProxyState_RebuildsUsableProxyBeforeHidingAuthoredLens()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayPartialProxyRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collapsedUvMesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var staleProxy = new GameObject("ScopeDisplayProxy");
            staleProxy.transform.SetParent(root.transform, false);
            var staleRenderer = staleProxy.AddComponent<MeshRenderer>();

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);
                SetField(lensDisplay, "_proxySurface", staleProxy);
                SetField(lensDisplay, "_proxyRenderer", staleRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True);

                var proxyMeshFilter = staleProxy.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null, "Proxy setup should rebuild a missing MeshFilter instead of hiding the authored lens behind an unusable proxy.");
                Assert.That(proxyMeshFilter!.sharedMesh, Is.Not.Null, "Proxy setup should only hide the authored lens once a usable proxy mesh exists.");
                Assert.That(meshRenderer.enabled, Is.False, "Authored lens should only be hidden after the rebuilt proxy becomes usable.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_Destroy_ReleasesGeneratedProxyMesh()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayDestroyRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collapsedUvMesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyMeshFilter = proxySurface!.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null);
                Assert.That(proxyMeshFilter!.sharedMesh, Is.Not.Null);
                var proxyMesh = proxyMeshFilter.sharedMesh;
                Assert.That(proxyMesh.name, Does.Contain("_ScopeProxy"), "Readable-collapsed UV meshes should allocate a flattened runtime proxy mesh.");

                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                yield return null;

                Assert.That(proxyMesh == null, Is.True, "Destroying the lens display should release the generated runtime proxy mesh.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

    }
}
