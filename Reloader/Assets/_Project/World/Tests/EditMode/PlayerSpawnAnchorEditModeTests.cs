using NUnit.Framework;
using Reloader.World.Travel;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public sealed class PlayerSpawnAnchorEditModeTests
    {
        [Test]
        public void Configure_SetsTrimmedAnchorId_AndKind()
        {
            var gameObject = new GameObject("SpawnAnchor");
            var anchor = gameObject.AddComponent<PlayerSpawnAnchor>();

            try
            {
                anchor.Configure(" entry.maintown.spawn ", PlayerSpawnAnchorKind.Spawn);

                Assert.That(anchor.AnchorId, Is.EqualTo("entry.maintown.spawn"));
                Assert.That(anchor.AnchorKind, Is.EqualTo(PlayerSpawnAnchorKind.Spawn));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneEntryPoint_EnsureSpawnAnchorContract_SynchronizesSiblingAnchor()
        {
            var gameObject = new GameObject("MainTownEntry_Spawn");
            var entryPoint = gameObject.AddComponent<SceneEntryPoint>();
            var anchor = gameObject.AddComponent<PlayerSpawnAnchor>();

            try
            {
                entryPoint.Configure(" entry.maintown.spawn ", PlayerSpawnAnchorKind.Spawn);

                Assert.That(anchor.AnchorId, Is.EqualTo("entry.maintown.spawn"));
                Assert.That(anchor.AnchorKind, Is.EqualTo(PlayerSpawnAnchorKind.Spawn));
                Assert.That(entryPoint.PlayerSpawnAnchor, Is.SameAs(anchor));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SceneEntryPoint_EnsureSpawnAnchorContract_ReplacesForeignAnchorReferenceWithSiblingAnchor()
        {
            var gameObject = new GameObject("MainTownEntry_Return");
            var foreignGameObject = new GameObject("ForeignAnchor");
            var entryPoint = gameObject.AddComponent<SceneEntryPoint>();
            var siblingAnchor = gameObject.AddComponent<PlayerSpawnAnchor>();
            var foreignAnchor = foreignGameObject.AddComponent<PlayerSpawnAnchor>();

            try
            {
                entryPoint.Configure("entry.maintown.return", PlayerSpawnAnchorKind.Return);

                var serializedEntryPoint = new SerializedObject(entryPoint);
                serializedEntryPoint.FindProperty("_playerSpawnAnchor")!.objectReferenceValue = foreignAnchor;
                serializedEntryPoint.ApplyModifiedPropertiesWithoutUndo();

                entryPoint.EnsureSpawnAnchorContract();

                serializedEntryPoint.Update();
                Assert.That(serializedEntryPoint.FindProperty("_playerSpawnAnchor")!.objectReferenceValue, Is.SameAs(siblingAnchor));
                Assert.That(siblingAnchor.AnchorId, Is.EqualTo("entry.maintown.return"));
                Assert.That(siblingAnchor.AnchorKind, Is.EqualTo(PlayerSpawnAnchorKind.Return));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(foreignGameObject);
            }
        }
    }
}
