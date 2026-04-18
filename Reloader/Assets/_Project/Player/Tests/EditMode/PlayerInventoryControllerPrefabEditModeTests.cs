using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.Inventory;
using UnityEditor;
using UnityEngine;

namespace Reloader.Player.Tests.EditMode
{
    public sealed class PlayerInventoryControllerPrefabEditModeTests
    {
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";
        private const string SpecialtyAmmoItemPath = "Assets/_Project/Inventory/Data/Items/Ammo_Specialty_308_150_AP.asset";

        [Test]
        public void PlayerRootPrefab_RegistersSpecialty308ApAmmoDefinitionForRuntimeDiscovery()
        {
            var playerRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRootPrefabPath);
            Assert.That(playerRootPrefab, Is.Not.Null, "Expected the canonical player prefab asset to exist.");

            var specialtyAmmo = AssetDatabase.LoadAssetAtPath<ItemDefinition>(SpecialtyAmmoItemPath);
            Assert.That(specialtyAmmo, Is.Not.Null, "Expected the specialty ammo item asset to exist.");

            var prefabContents = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);
            try
            {
                var inventoryController = prefabContents.GetComponentInChildren<PlayerInventoryController>(true);
                Assert.That(inventoryController, Is.Not.Null, "Expected PlayerRoot prefab to contain PlayerInventoryController.");

                var registrySnapshot = inventoryController!.GetItemDefinitionRegistrySnapshot();
                Assert.That(registrySnapshot, Is.Not.Null);
                Assert.That(registrySnapshot, Does.Contain(specialtyAmmo));

                Assert.That(specialtyAmmo!.DefinitionId, Is.EqualTo("ammo-specialty-308-150-ap"));
                Assert.That(specialtyAmmo.Category, Is.EqualTo(ItemCategory.Bullet));
                Assert.That(specialtyAmmo.MaxStack, Is.EqualTo(999));
                Assert.That(specialtyAmmo.DisplayName, Is.EqualTo(".308 150gr AP"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }
    }
}
