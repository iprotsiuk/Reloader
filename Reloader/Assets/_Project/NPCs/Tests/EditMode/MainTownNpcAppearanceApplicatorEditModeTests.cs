using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.World;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class MainTownNpcAppearanceApplicatorEditModeTests
    {
        [Test]
        public void Apply_WhenMaleAppearanceIsProvided_ActivatesOnlyMaleRigAndCompatibleModules()
        {
            var root = CreateTestRoot();
            try
            {
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    BaseBodyId = "male.body",
                    HairId = "hair.short",
                    BeardId = "beard4",
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "brous1",
                    OuterwearId = "openJacket"
                };

                applicator.Apply(record);

                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/Hair_Hair_Hair").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/beard4_beard4_beard4").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/T_shirt1__T_shirt1_T_shirt1").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/jacket_jacket_jacket").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/pants1_pants1_pants1").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot/brous1_brous1_brous1").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Apply_WhenFemaleAppearanceUsesUnsupportedBottom_NormalizesToSafeBottom()
        {
            var root = CreateTestRoot();
            try
            {
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    BaseBodyId = "female.body",
                    HairId = "hair.long",
                    BeardId = "beard7",
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "brous10",
                    OuterwearId = string.Empty
                };

                applicator.Apply(record);

                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleMaleRoot").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/hair3").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/T_shirt2").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/pants1").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/brous10").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyAuthoringAppearanceFromContext_WhenVendorIdsDiffer_UsesDifferentModuleSignatures()
        {
            var firstVendor = CreateTestRoot();
            var secondVendor = CreateTestRoot();
            try
            {
                SetVendorId(firstVendor.AddComponent<ShopVendorTarget>(), "vendor-weapon-store");
                SetVendorId(secondVendor.AddComponent<ShopVendorTarget>(), "vendor-ammo-store");

                var firstApplicator = firstVendor.AddComponent<MainTownNpcAppearanceApplicator>();
                var secondApplicator = secondVendor.AddComponent<MainTownNpcAppearanceApplicator>();

                firstApplicator.ApplyAuthoringAppearanceFromContext();
                secondApplicator.ApplyAuthoringAppearanceFromContext();

                var firstSignature = GetActiveModuleSignature(firstVendor);
                var secondSignature = GetActiveModuleSignature(secondVendor);

                Assert.That(firstSignature, Is.Not.Empty);
                Assert.That(secondSignature, Is.Not.Empty);
                Assert.That(firstSignature, Is.Not.EqualTo(secondSignature));
            }
            finally
            {
                Object.DestroyImmediate(firstVendor);
                Object.DestroyImmediate(secondVendor);
            }
        }

        [Test]
        public void ApplyAuthoringAppearanceFromContext_WhenVendorIdMatches_ReusesTheSameDeterministicAppearance()
        {
            var firstVendor = CreateTestRoot();
            var secondVendor = CreateTestRoot();
            try
            {
                SetVendorId(firstVendor.AddComponent<ShopVendorTarget>(), "vendor-reloading-store");
                SetVendorId(secondVendor.AddComponent<ShopVendorTarget>(), "vendor-reloading-store");

                var firstApplicator = firstVendor.AddComponent<MainTownNpcAppearanceApplicator>();
                var secondApplicator = secondVendor.AddComponent<MainTownNpcAppearanceApplicator>();

                firstApplicator.ApplyAuthoringAppearanceFromContext();
                secondApplicator.ApplyAuthoringAppearanceFromContext();

                Assert.That(GetActiveModuleSignature(firstVendor), Is.EqualTo(GetActiveModuleSignature(secondVendor)));
            }
            finally
            {
                Object.DestroyImmediate(firstVendor);
                Object.DestroyImmediate(secondVendor);
            }
        }

        private static GameObject CreateTestRoot()
        {
            var root = new GameObject("Npc");
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            var maleRoot = new GameObject("StyleMaleRoot");
            maleRoot.transform.SetParent(visualRoot.transform, false);
            CreateChild(maleRoot.transform, "root");
            CreateChild(maleRoot.transform, "Man_Man_Man");
            CreateChild(maleRoot.transform, "Eyes_Eyes_Eyes");
            CreateChild(maleRoot.transform, "boots1_boots1_boots1");
            CreateChild(maleRoot.transform, "Hair_Hair_Hair");
            CreateChild(maleRoot.transform, "Hair_02_hair_hair");
            CreateChild(maleRoot.transform, "hair3_hair3_hair3");
            CreateChild(maleRoot.transform, "hair4_hair4_hair4");
            CreateChild(maleRoot.transform, "beard4_beard4_beard4");
            CreateChild(maleRoot.transform, "T_shirt1__T_shirt1_T_shirt1");
            CreateChild(maleRoot.transform, "T_shirt2_T_shirt2_T_shirt2");
            CreateChild(maleRoot.transform, "shirt3_shirt3_shirt3");
            CreateChild(maleRoot.transform, "jacket_jacket_jacket");
            CreateChild(maleRoot.transform, "hoody_hoody_hoody");
            CreateChild(maleRoot.transform, "brous1_brous1_brous1");
            CreateChild(maleRoot.transform, "pants1_pants1_pants1");

            var femaleRoot = new GameObject("StyleFemaleRoot");
            femaleRoot.transform.SetParent(visualRoot.transform, false);
            CreateChild(femaleRoot.transform, "root");
            CreateChild(femaleRoot.transform, "woman");
            CreateChild(femaleRoot.transform, "Eyes");
            CreateChild(femaleRoot.transform, "boots1");
            CreateChild(femaleRoot.transform, "hair1");
            CreateChild(femaleRoot.transform, "hair3");
            CreateChild(femaleRoot.transform, "T_shirt1");
            CreateChild(femaleRoot.transform, "T_shirt2");
            CreateChild(femaleRoot.transform, "shirt3");
            CreateChild(femaleRoot.transform, "jacket");
            CreateChild(femaleRoot.transform, "hoody");
            CreateChild(femaleRoot.transform, "brous1_001");
            CreateChild(femaleRoot.transform, "brous7");
            CreateChild(femaleRoot.transform, "brous10");
            CreateChild(femaleRoot.transform, "pants1");

            return root;
        }

        private static void CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.SetActive(false);
        }

        private static void SetVendorId(ShopVendorTarget target, string vendorId)
        {
            var vendorIdField = typeof(ShopVendorTarget).GetField("_vendorId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(vendorIdField, Is.Not.Null);
            vendorIdField.SetValue(target, vendorId);
        }

        private static string GetActiveModuleSignature(GameObject root)
        {
            var activeNames = new List<string>();
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child == root.transform)
                {
                    continue;
                }

                if (child.gameObject.activeSelf)
                {
                    activeNames.Add(child.name);
                }
            }

            return string.Join("|", activeNames.OrderBy(name => name));
        }
    }
}
