using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Reloading.World;
using UnityEngine;

namespace Reloader.Reloading.Tests.PlayMode
{
    public class WorkbenchContainerLinkPlayModeTests
    {
        [Test]
        public void WorkbenchContainerLink_EnumeratesLinkedContainerIds_InDeterministicOrder()
        {
            var go = new GameObject("Workbench");
            var link = go.AddComponent<WorkbenchContainerLink>();

            SetLinkedContainerIds(link, " chest.alpha ", "", "chest.alpha", "chest.beta", "  ", "chest.beta", "chest.gamma");

            var ids = link.EnumerateLinkedContainerIds().ToArray();

            Assert.That(ids, Is.EqualTo(new[]
            {
                "chest.alpha",
                "chest.beta",
                "chest.gamma"
            }));

            UnityEngine.Object.Destroy(go);
        }

        [Test]
        public void StorageWorkbenchLinkQuery_EnumeratesLinkedContainerItems_InContainerThenSlotOrder()
        {
            var go = new GameObject("Workbench");
            var link = go.AddComponent<WorkbenchContainerLink>();
            SetLinkedContainerIds(link, "chest.beta", "chest.missing", "chest.alpha");

            var containers = new Dictionary<string, InventoryContainerState>(StringComparer.Ordinal)
            {
                {
                    "chest.alpha",
                    CreateContainerWithItems((0, "item-a", 2, 20), (2, "item-b", 5, 20))
                },
                {
                    "chest.beta",
                    CreateContainerWithItems((1, "item-c", 9, 20))
                }
            };

            var rows = StorageWorkbenchLinkQuery
                .EnumerateLinkedItems(
                    link.EnumerateLinkedContainerIds(),
                    containerId => containers.TryGetValue(containerId, out var state) ? state : null)
                .ToArray();

            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[0].ContainerId, Is.EqualTo("chest.beta"));
            Assert.That(rows[0].SlotIndex, Is.EqualTo(1));
            Assert.That(rows[0].Stack.ItemId, Is.EqualTo("item-c"));
            Assert.That(rows[0].Stack.Quantity, Is.EqualTo(9));

            Assert.That(rows[1].ContainerId, Is.EqualTo("chest.alpha"));
            Assert.That(rows[1].SlotIndex, Is.EqualTo(0));
            Assert.That(rows[1].Stack.ItemId, Is.EqualTo("item-a"));
            Assert.That(rows[1].Stack.Quantity, Is.EqualTo(2));

            Assert.That(rows[2].ContainerId, Is.EqualTo("chest.alpha"));
            Assert.That(rows[2].SlotIndex, Is.EqualTo(2));
            Assert.That(rows[2].Stack.ItemId, Is.EqualTo("item-b"));
            Assert.That(rows[2].Stack.Quantity, Is.EqualTo(5));

            UnityEngine.Object.Destroy(go);
        }

        private static InventoryContainerState CreateContainerWithItems(params (int slotIndex, string itemId, int quantity, int maxStack)[] items)
        {
            var container = new InventoryContainerState(
                InventoryContainerType.Storage,
                slotCount: 4,
                ContainerPermissions.PlayerMutable);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                container.TrySetSlot(item.slotIndex, new ItemStackState(item.itemId, item.quantity, item.maxStack));
            }

            return container;
        }

        private static void SetLinkedContainerIds(WorkbenchContainerLink link, params string[] ids)
        {
            var field = typeof(WorkbenchContainerLink).GetField("_linkedContainerIds", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(link, ids);
        }
    }
}
