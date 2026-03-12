using System.Collections;
using NUnit.Framework;
using Reloader.DevTools.Data;
using Reloader.DevTools.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.DevTools.Tests.PlayMode
{
    public sealed class DevSpawnNpcCommandPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnNpcCommand_SpawnsConfiguredPrefabAtCrosshairHitPoint()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.identity;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            target.transform.localScale = Vector3.one;

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcPolicePrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.police", "Police Officer", prefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            yield return null;

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc npc.police"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("npc.police"));

            var spawned = GameObject.Find("NpcPolicePrefab(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(0f, 0f, 4.5f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(spawned);
        }

        [UnityTest]
        public IEnumerator SpawnNpcCommand_WithoutCrosshairHit_FallsBackInFrontOfCamera()
        {
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(1f, 2f, 3f);
            camera.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var catalog = ScriptableObject.CreateInstance<DevNpcSpawnCatalog>();
            var prefab = new GameObject("NpcClerkPrefab");
            prefab.SetActive(false);
            catalog.SetEntriesForTests(new[]
            {
                new DevNpcSpawnCatalog.Entry("npc.front-desk-clerk", "Front Desk Clerk", prefab)
            });

            var command = new DevSpawnNpcCommand(new DevNpcSpawnService(catalog), catalog);

            yield return null;

            var executed = command.TryExecute(DevCommandLineParser.Parse("spawn npc npc.front-desk-clerk"), out var resultMessage);

            Assert.That(executed, Is.True);
            Assert.That(resultMessage, Does.Contain("npc.front-desk-clerk"));

            var spawned = GameObject.Find("NpcClerkPrefab(Clone)");
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.transform.position, Is.EqualTo(new Vector3(4f, 2f, 3f)).Using(Vector3EqualityComparer.Instance));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(cameraGo);
            Object.DestroyImmediate(spawned);
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer
        {
            public static readonly Vector3EqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Vector3 left || y is not Vector3 right)
                {
                    return false;
                }

                return Vector3.Distance(left, right) <= 0.01f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}
