using System.Linq;
using NUnit.Framework;
using Reloader.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.World.Tests.EditMode
{
    public class StyleCrowdReviewBuilderEditModeTests
    {
        [Test]
        public void GetLocalPosition_SeparatesPlausibleAndStressBlocks()
        {
            var plausible = StyleCrowdReviewBuilder.GetLocalPosition(0, StyleCrowdReviewBatchKind.Plausible);
            var stress = StyleCrowdReviewBuilder.GetLocalPosition(0, StyleCrowdReviewBatchKind.Stress);
            var recovered = StyleCrowdReviewBuilder.GetLocalPosition(0, StyleCrowdReviewBatchKind.Recovered);

            Assert.That(plausible.x, Is.LessThan(stress.x));
            Assert.That(Mathf.Abs(stress.x - plausible.x), Is.GreaterThan(10f));
            Assert.That(stress.x, Is.LessThan(recovered.x));
        }

        [Test]
        public void StyleCrowdReviewSpec_ExposesSeparateRequiredEyebrowLane()
        {
            var eyebrowProperty = typeof(StyleCrowdReviewSpec).GetProperty("EyebrowId");

            Assert.That(eyebrowProperty, Is.Not.Null, "STYLE review specs need an explicit eyebrow lane separate from BottomId.");
        }

        [Test]
        public void BuildAllSpecs_RequireExplicitEyebrowsAndPants()
        {
            var eyebrowProperty = typeof(StyleCrowdReviewSpec).GetProperty("EyebrowId");
            Assert.That(eyebrowProperty, Is.Not.Null, "STYLE review specs need an explicit eyebrow lane separate from BottomId.");

            var specs = StyleCrowdReviewSpecLibrary.BuildAll();

            foreach (var spec in specs)
            {
                var eyebrowId = eyebrowProperty.GetValue(spec) as string;

                Assert.That(eyebrowId, Is.Not.Null.And.Not.Empty, $"{spec.Name} is missing an explicit eyebrow id.");
                Assert.That(spec.BottomId, Is.EqualTo("pants1"), $"{spec.Name} should always use pants1 as the bottom lane.");
            }
        }

        [Test]
        public void StyleCrowdReviewSpec_WhenEyebrowAndBottomAreMissing_NormalizesToRequiredDefaults()
        {
            var spec = new StyleCrowdReviewSpec(
                "Recovered_Missing_01",
                "Recovered",
                StyleCrowdReviewBatchKind.Recovered,
                StyleCrowdReviewGender.Female,
                "hair.bob",
                string.Empty,
                "tshirt1",
                string.Empty,
                string.Empty);

            Assert.That(spec.EyebrowId, Is.EqualTo("brous1"));
            Assert.That(spec.BottomId, Is.EqualTo("pants1"));
        }

        [Test]
        public void GetDirectChildNamesToActivate_WhenSpecCarriesInvalidEyebrowAndBottom_HealsToSupportedDefaults()
        {
            var spec = new StyleCrowdReviewSpec(
                "Recovered_Invalid_01",
                "Recovered",
                StyleCrowdReviewBatchKind.Recovered,
                StyleCrowdReviewGender.Male,
                "hair.short",
                string.Empty,
                "tshirt1",
                "legacy.brows.invalid",
                "legacy.bottom.invalid");

            var activeNames = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();

            CollectionAssert.Contains(activeNames, "brous1_brous1_brous1");
            CollectionAssert.Contains(activeNames, "pants1_pants1_pants1");
        }

        [Test]
        public void GetDirectChildNamesToActivate_ForMaleSpec_EnablesExpectedGroups()
        {
            var spec = new StyleCrowdReviewSpec(
                "Police_Plausible_01",
                "Police",
                StyleCrowdReviewBatchKind.Plausible,
                StyleCrowdReviewGender.Male,
                "hair.parted",
                "beard3",
                "openJacket",
                "brous6",
                "pants1");

            var activeNames = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();

            CollectionAssert.Contains(activeNames, "root");
            CollectionAssert.Contains(activeNames, "Man_Man_Man");
            CollectionAssert.Contains(activeNames, "Eyes_Eyes_Eyes");
            CollectionAssert.Contains(activeNames, "boots1_boots1_boots1");
            CollectionAssert.Contains(activeNames, "hair4_hair4_hair4");
            CollectionAssert.Contains(activeNames, "beard3_beard3_beard3");
            CollectionAssert.Contains(activeNames, "jacket_jacket_jacket");
            CollectionAssert.Contains(activeNames, "brous6_brous6_brous6");
            CollectionAssert.Contains(activeNames, "pants1_pants1_pants1");
        }

        [Test]
        public void GetDirectChildNamesToActivate_ForOuterwearTop_AddsBaseLayer()
        {
            var spec = new StyleCrowdReviewSpec(
                "Police_Plausible_03",
                "Police",
                StyleCrowdReviewBatchKind.Plausible,
                StyleCrowdReviewGender.Female,
                "hair.bob",
                string.Empty,
                "openJacket",
                "brous3",
                "pants1");

            var activeNames = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();

            CollectionAssert.Contains(activeNames, "jacket");
            CollectionAssert.DoesNotContain(activeNames, "shirt3");
            Assert.That(
                activeNames.Any(name => name is "shirt" or "shirt2" or "T_shirt1" or "T_shirt2" or "turtleneck"),
                Is.True,
                "Female outerwear should auto-add a shirt-like underlayer instead of the legacy shirt3 jacket mesh.");
        }

        [Test]
        public void GetDirectChildNamesToActivate_ForLayeredJacket_AddsTeeLikeBaseLayer()
        {
            var spec = new StyleCrowdReviewSpec(
                "Recovered_Jacket_01",
                "Recovered",
                StyleCrowdReviewBatchKind.Recovered,
                StyleCrowdReviewGender.Male,
                "hair.short",
                string.Empty,
                "jacket",
                "brous1",
                "pants1");

            var activeNames = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();

            CollectionAssert.Contains(activeNames, "shirt3_shirt3_shirt3");
            Assert.That(
                activeNames.Any(name => name is "T_shirt1__T_shirt1_T_shirt1" or "T_shirt2_T_shirt2_T_shirt2"),
                Is.True,
                "Layered jackets should include a T-shirt underlayer.");
            Assert.That(
                activeNames.Any(name => name is "shirt_shirt_shirt" or "shirt2_shirt2_shirt2"),
                Is.False,
                "Layered jackets should not stack over collared shirt meshes.");
        }

        [Test]
        public void GetDirectChildNamesToActivate_ForFemaleSpec_OmitsBeards()
        {
            var spec = new StyleCrowdReviewSpec(
                "Student_Stress_02",
                "Student",
                StyleCrowdReviewBatchKind.Stress,
                StyleCrowdReviewGender.Female,
                "hair.bob",
                string.Empty,
                "hoody",
                "brous7",
                "pants1");

            var activeNames = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();

            CollectionAssert.Contains(activeNames, "root");
            CollectionAssert.Contains(activeNames, "woman");
            CollectionAssert.Contains(activeNames, "Eyes");
            CollectionAssert.Contains(activeNames, "boots1");
            CollectionAssert.Contains(activeNames, "hair1");
            CollectionAssert.Contains(activeNames, "hoody");
            CollectionAssert.Contains(activeNames, "brous7");
            CollectionAssert.Contains(activeNames, "pants1");
            Assert.That(activeNames.Any(name => name.StartsWith("beard")), Is.False);
        }

        [Test]
        public void ResolveSourceTemplate_WhenSceneHasNoVendorRoots_LoadsMaleModelAsset()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var template = StyleCrowdReviewBuilder.ResolveSourceTemplate(scene, StyleCrowdReviewGender.Male);

            Assert.That(template, Is.Not.Null);
            Assert.That(template.name, Does.StartWith("Man_Rig_Correct"));
        }

        [Test]
        public void InstantiateClone_WhenSourceIsSceneObject_CreatesParentedCopy()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var parent = new GameObject("Parent").transform;
            var source = new GameObject("Source");
            source.transform.SetParent(scene.GetRootGameObjects().First().transform, false);

            var clone = StyleCrowdReviewBuilder.InstantiateClone(source, parent);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.transform.parent, Is.EqualTo(parent));
            Assert.That(clone.name, Does.StartWith("Source"));
        }

        [Test]
        public void BuildAllSpecs_ResolveExternalMaterials_ForEveryActiveRendererChild()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll();

            foreach (var spec in specs)
            {
                var activeChildren = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec)
                    .Where(name => name != "root");

                foreach (var childName in activeChildren)
                {
                    var path = StyleCrowdReviewBuilder.GetExternalMaterialPath(spec.Gender, childName);
                    Assert.That(path, Is.Not.Null.And.Not.Empty, $"{spec.Name} has no external material mapping for child '{childName}'.");
                    Assert.That(AssetDatabase.LoadAssetAtPath<Material>(path), Is.Not.Null, $"{spec.Name} maps child '{childName}' to missing material '{path}'.");
                }
            }
        }

        [Test]
        public void GetExternalMaterialPaths_ForKnownGarment_ReturnsSiblingVariants()
        {
            var paths = StyleCrowdReviewBuilder.GetExternalMaterialPaths(
                StyleCrowdReviewGender.Male,
                "shirt_shirt_shirt");

            Assert.That(paths, Is.Not.Null);
            Assert.That(paths.Count, Is.GreaterThan(1));
            Assert.That(paths.Distinct().Count(), Is.EqualTo(paths.Count));
            Assert.That(paths.All(path => AssetDatabase.LoadAssetAtPath<Material>(path) != null), Is.True);
        }

        [Test]
        public void GetExternalMaterialPaths_ForRecoveredGarments_ResolveToMatchingFamilies()
        {
            var hoodyPaths = StyleCrowdReviewBuilder.GetExternalMaterialPaths(
                StyleCrowdReviewGender.Male,
                "hoody_hoody_hoody");
            var openJacketPaths = StyleCrowdReviewBuilder.GetExternalMaterialPaths(
                StyleCrowdReviewGender.Male,
                "jacket_jacket_jacket");

            Assert.That(hoodyPaths, Is.Not.Empty);
            Assert.That(openJacketPaths, Is.Not.Empty);
            Assert.That(hoodyPaths.All(path => path.Contains("/Textures/Man/Hoody/", System.StringComparison.Ordinal)), Is.True);
            Assert.That(openJacketPaths.All(path => path.Contains("/Textures/Man/Jacket/", System.StringComparison.Ordinal)), Is.True);
            Assert.That(hoodyPaths.Count, Is.GreaterThan(1), "Expected recovered hoody materials to include generated sibling variants.");
            Assert.That(openJacketPaths.Count, Is.GreaterThan(1), "Expected recovered jacket materials to include generated sibling variants.");
            Assert.That(hoodyPaths.Any(path => path.EndsWith("/hoody_2_baseColor.mat", System.StringComparison.Ordinal)), Is.True);
            Assert.That(openJacketPaths.Any(path => path.EndsWith("/jacket2_baseColor.mat", System.StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void GetExternalMaterialPath_ForFemaleLongHair_Uses2048Material()
        {
            var path = StyleCrowdReviewBuilder.GetExternalMaterialPath(
                StyleCrowdReviewGender.Female,
                "hair3");

            Assert.That(path, Is.Not.Null.And.Not.Empty);
            Assert.That(path, Does.Contain("/Textures/Woman/Hair/3/2048/"));
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(path), Is.Not.Null);
        }

        [Test]
        public void BuildAllSpecs_ResolveEyebrowAndBottomMaterials_ToTheirOwnFamilies()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll();

            foreach (var spec in specs)
            {
                var activeChildren = StyleCrowdReviewBuilder.GetDirectChildNamesToActivate(spec).ToArray();
                var eyebrowChild = activeChildren.Single(name => name.StartsWith("brous", System.StringComparison.OrdinalIgnoreCase));
                var bottomChild = activeChildren.Single(name => name.Contains("pants1", System.StringComparison.OrdinalIgnoreCase));

                var eyebrowPath = StyleCrowdReviewBuilder.GetExternalMaterialPath(spec.Gender, eyebrowChild);
                var bottomPath = StyleCrowdReviewBuilder.GetExternalMaterialPath(spec.Gender, bottomChild);

                Assert.That(eyebrowPath, Does.Contain("/Brous/"), $"{spec.Name} mapped eyebrow child '{eyebrowChild}' to non-eyebrow material '{eyebrowPath}'.");
                Assert.That(bottomPath, Does.Contain("/Pants1/"), $"{spec.Name} mapped bottom child '{bottomChild}' to non-pants material '{bottomPath}'.");
            }
        }

        [Test]
        public void GetExternalMaterialPath_WithVariationKey_UsesMoreThanOneSiblingVariant()
        {
            var selectedPaths = Enumerable.Range(0, 16)
                .Select(index => StyleCrowdReviewBuilder.GetExternalMaterialPath(
                    StyleCrowdReviewGender.Male,
                    "shirt_shirt_shirt",
                    $"Police_Plausible_{index:00}|shirt"))
                .Distinct()
                .ToArray();

            Assert.That(selectedPaths.Length, Is.GreaterThan(1));
        }

        [TestCase(StyleCrowdReviewGender.Male, "brous1_brous1_brous1", "brous1_brous1_brous1")]
        [TestCase(StyleCrowdReviewGender.Male, "brous6_brous6_brous6", "brous6_brous6_brous6")]
        [TestCase(StyleCrowdReviewGender.Male, "brous10_brous10_brous10", "brous6_brous6_brous6")]
        [TestCase(StyleCrowdReviewGender.Female, "brous3", "brous3")]
        [TestCase(StyleCrowdReviewGender.Female, "brous7", "brous7")]
        [TestCase(StyleCrowdReviewGender.Female, "brous10", "brous7")]
        public void NormalizeChildName_ReplacesUnsupportedLiveVariants(
            StyleCrowdReviewGender gender,
            string childName,
            string expected)
        {
            Assert.That(StyleCrowdReviewBuilder.NormalizeChildName(gender, childName), Is.EqualTo(expected));
        }
    }
}
