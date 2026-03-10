using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class MainTownNpcAppearanceApplicatorEditModeTests
    {
        [Test]
        public void Apply_WhenMaleAppearanceIsProvided_NormalizesToRuntimeSafeBottom()
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
        public void Apply_WhenFemaleAppearanceUsesCuratedBottom_NormalizesToRuntimeSafeBottom()
        {
            var root = CreateTestRoot();
            try
            {
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    BaseBodyId = "female.body",
                    HairId = "hair.long",
                    BeardId = string.Empty,
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "brous7",
                    OuterwearId = string.Empty
                };

                applicator.Apply(record);

                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/pants1").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/brous7").gameObject.activeSelf, Is.False);
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
        public void Apply_WhenHoodyOuterwearIsSelected_DoesNotLayerExtraBaseTop()
        {
            var root = CreateTestRoot();
            try
            {
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    BaseBodyId = "female.body",
                    HairId = "hair.long",
                    BeardId = string.Empty,
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "pants1",
                    OuterwearId = "hoody"
                };

                applicator.Apply(record);

                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/hoody").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/T_shirt1").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Apply_WhenOuterwearIdIsUnknown_FallsBackToBaseTop()
        {
            var root = CreateTestRoot();
            try
            {
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    BaseBodyId = "female.body",
                    HairId = "hair.long",
                    BeardId = string.Empty,
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "pants1",
                    OuterwearId = "legacy.coat"
                };

                applicator.Apply(record);

                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/T_shirt2").gameObject.activeSelf, Is.True);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/hoody").gameObject.activeSelf, Is.False);
                Assert.That(root.transform.Find("VisualRoot/StyleFemaleRoot/jacket").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GetDeterministicSeed_WhenHashOverflowsToIntMinValue_ReturnsNonNegativeSeed()
        {
            var method = typeof(MainTownNpcAppearanceApplicator).GetMethod(
                "GetDeterministicSeed",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            const string intMinValueHashSeed = "\u0706\t\u001E\f\u0002";
            var seed = (int)method.Invoke(null, new object[] { intMinValueHashSeed });

            Assert.That(seed, Is.GreaterThanOrEqualTo(0));
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

        [Test]
        public void ApplyAuthoringAppearanceFromContext_WhenSameVendorIsReapplied_RestoresStaleVisualState()
        {
            var root = CreateTestRoot();
            try
            {
                SetVendorId(root.AddComponent<ShopVendorTarget>(), "vendor-reloading-store");
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var firstApplied = applicator.ApplyAuthoringAppearanceFromContext();
                Assert.That(firstApplied, Is.True);

                var signatureBefore = GetActiveModuleSignature(root);
                Assert.That(signatureBefore, Is.Not.Empty);

                var staleChild = root.GetComponentsInChildren<Transform>(includeInactive: false)
                    .FirstOrDefault(child => child != root.transform && child.childCount == 0 && child.name != "root");
                Assert.That(staleChild, Is.Not.Null);
                staleChild.gameObject.SetActive(false);

                var reapplied = applicator.ApplyAuthoringAppearanceFromContext();
                Assert.That(reapplied, Is.True);
                Assert.That(GetActiveModuleSignature(root), Is.EqualTo(signatureBefore));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyAuthoringAppearanceFromContext_WhenNpcDefinitionExists_AppliesAuthoringModules()
        {
            var root = CreateTestRoot();
            var definition = ScriptableObject.CreateInstance<NpcDefinition>();
            try
            {
                root.name = "ContractTarget_Volkov";
                SetNpcDefinition(root.AddComponent<NpcAgent>(), definition, "npc.target.volkov");
                var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();

                var applied = applicator.ApplyAuthoringAppearanceFromContext();

                Assert.That(applied, Is.True);
                Assert.That(GetActiveModuleSignature(root), Is.Not.Empty);
                Assert.That(HasActiveBottom(root, "pants1_pants1_pants1"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Apply_WhenRecordDiffers_AssignsDifferentGarmentMaterials()
        {
            var firstRoot = CreateTestRoot();
            var secondRoot = CreateTestRoot();
            try
            {
                var firstApplicator = firstRoot.AddComponent<MainTownNpcAppearanceApplicator>();
                var secondApplicator = secondRoot.AddComponent<MainTownNpcAppearanceApplicator>();

                var firstRecord = new CivilianPopulationRecord
                {
                    CivilianId = "citizen.001",
                    BaseBodyId = "male.body",
                    HairId = "hair.short",
                    HairColorId = "hair.variant.a",
                    BeardId = "beard4",
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "brous1",
                    OuterwearId = "openJacket",
                    MaterialColorIds = new List<string> { "style.variant.a" }
                };
                var secondRecord = new CivilianPopulationRecord
                {
                    CivilianId = "citizen.002",
                    BaseBodyId = "male.body",
                    HairId = "hair.short",
                    HairColorId = "hair.variant.c",
                    BeardId = "beard4",
                    OutfitTopId = "tshirt1",
                    OutfitBottomId = "brous1",
                    OuterwearId = "openJacket",
                    MaterialColorIds = new List<string> { "style.variant.c" }
                };

                firstApplicator.Apply(firstRecord);
                secondApplicator.Apply(secondRecord);

                var firstSignature = GetActiveMaterialSignature(firstRoot);
                var secondSignature = GetActiveMaterialSignature(secondRoot);

                Assert.That(firstSignature, Is.Not.Empty);
                Assert.That(secondSignature, Is.Not.Empty);
                Assert.That(firstSignature, Is.Not.EqualTo(secondSignature));
            }
            finally
            {
                Object.DestroyImmediate(firstRoot);
                Object.DestroyImmediate(secondRoot);
            }
        }

        [Test]
        public void Apply_WhenRecordMatches_ReusesDeterministicGarmentMaterials()
        {
            var firstRoot = CreateTestRoot();
            var secondRoot = CreateTestRoot();
            try
            {
                var firstApplicator = firstRoot.AddComponent<MainTownNpcAppearanceApplicator>();
                var secondApplicator = secondRoot.AddComponent<MainTownNpcAppearanceApplicator>();

                var record = new CivilianPopulationRecord
                {
                    CivilianId = "citizen.shared",
                    BaseBodyId = "female.body",
                    HairId = "hair.long",
                    HairColorId = "hair.variant.b",
                    BeardId = string.Empty,
                    OutfitTopId = "tshirt2",
                    OutfitBottomId = "brous7",
                    OuterwearId = string.Empty,
                    MaterialColorIds = new List<string> { "style.variant.b" }
                };

                firstApplicator.Apply(record);
                secondApplicator.Apply(record);

                Assert.That(GetActiveMaterialSignature(firstRoot), Is.EqualTo(GetActiveMaterialSignature(secondRoot)));
            }
            finally
            {
                Object.DestroyImmediate(firstRoot);
                Object.DestroyImmediate(secondRoot);
            }
        }

        [Test]
        public void ResolveDialogueFocusAnchor_WhenHeadBoneExists_CreatesHeadAnchoredFaceTarget()
        {
            var root = new GameObject("Npc");
            var visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(root.transform, false);
            var maleRoot = new GameObject("StyleMaleRoot").transform;
            maleRoot.SetParent(visualRoot, false);
            var applicator = root.AddComponent<MainTownNpcAppearanceApplicator>();
            var headBone = new GameObject("HeadBone").transform;
            headBone.SetParent(maleRoot, false);
            headBone.position = new Vector3(1f, 1.6f, 2f);
            maleRoot.rotation = Quaternion.Euler(0f, 90f, 0f);

            try
            {
                var method = typeof(MainTownNpcAppearanceApplicator).GetMethod(
                    "ResolveHeadBoneDialogueFocusAnchor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null, "Expected dedicated face-anchor helper for head-bone dialogue framing.");

                var anchor = method!.Invoke(applicator, new object[] { headBone, maleRoot }) as Transform;

                Assert.That(anchor, Is.Not.Null, "Expected head-bone framing to produce a runtime face anchor.");
                Assert.That(anchor!.name, Is.EqualTo("DialogueFaceAnchorRuntime"));
                Assert.That(anchor.position.y, Is.EqualTo(headBone.position.y - 0.24f).Within(0.001f), "Expected face anchor to sit 0.3 units lower than the prior head-bone framing offset.");
                Assert.That(anchor.position.x, Is.EqualTo(headBone.position.x).Within(0.001f), "Expected head-anchored dialogue target to avoid shifting sideways from the head position.");
                Assert.That(anchor.position.z, Is.EqualTo(headBone.position.z).Within(0.001f), "Expected head-anchored dialogue target to stay inside the face plane rather than pushing in front of the NPC.");
            }
            finally
            {
                Object.DestroyImmediate(root);
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

            AttachMaterialCatalog(maleRoot, new[]
            {
                "Hair_Hair_Hair",
                "beard4_beard4_beard4",
                "T_shirt1__T_shirt1_T_shirt1",
                "T_shirt2_T_shirt2_T_shirt2",
                "jacket_jacket_jacket",
                "brous1_brous1_brous1",
                "pants1_pants1_pants1"
            });
            AttachMaterialCatalog(femaleRoot, new[]
            {
                "hair3",
                "T_shirt1",
                "T_shirt2",
                "brous7",
                "pants1"
            });

            return root;
        }

        private static void CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshRenderer>();
            child.SetActive(false);
        }

        private static void AttachMaterialCatalog(GameObject root, IEnumerable<string> childNames)
        {
            var catalog = root.AddComponent<StyleMaterialVariantCatalog>();
            catalog.SetEntries(childNames.Select(childName => new StyleMaterialVariantCatalog.Entry
            {
                ChildName = childName,
                Materials = new[]
                {
                    CreateMaterial($"{childName}_Variant_A"),
                    CreateMaterial($"{childName}_Variant_B"),
                    CreateMaterial($"{childName}_Variant_C"),
                    CreateMaterial($"{childName}_Variant_D"),
                    CreateMaterial($"{childName}_Variant_E"),
                    CreateMaterial($"{childName}_Variant_F"),
                    CreateMaterial($"{childName}_Variant_G")
                }
            }));
        }

        private static Material CreateMaterial(string name)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(shader)
            {
                name = name
            };
            return material;
        }

        private static void SetVendorId(ShopVendorTarget target, string vendorId)
        {
            var vendorIdField = typeof(ShopVendorTarget).GetField("_vendorId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(vendorIdField, Is.Not.Null);
            vendorIdField.SetValue(target, vendorId);
        }

        private static void SetNpcDefinition(NpcAgent agent, NpcDefinition definition, string npcId)
        {
            var npcIdField = typeof(NpcDefinition).GetField("_npcId", BindingFlags.Instance | BindingFlags.NonPublic);
            var definitionField = typeof(NpcAgent).GetField("_definition", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(npcIdField, Is.Not.Null);
            Assert.That(definitionField, Is.Not.Null);
            npcIdField.SetValue(definition, npcId);
            definitionField.SetValue(agent, definition);
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

        private static string GetActiveMaterialSignature(GameObject root)
        {
            var activeMaterials = new List<string>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (!renderer.gameObject.activeSelf || renderer.sharedMaterial == null)
                {
                    continue;
                }

                activeMaterials.Add($"{renderer.gameObject.name}:{renderer.sharedMaterial.name}");
            }

            return string.Join("|", activeMaterials.OrderBy(name => name));
        }

        private static bool HasActiveBottom(GameObject root, string bottomName)
        {
            return root.GetComponentsInChildren<Transform>(includeInactive: true)
                .Any(child => child != root.transform && child.gameObject.activeSelf && child.name == bottomName);
        }
    }
}
