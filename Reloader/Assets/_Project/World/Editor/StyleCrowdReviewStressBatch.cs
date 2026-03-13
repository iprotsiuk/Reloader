using System;
using System.Collections.Generic;

namespace Reloader.World.Editor
{
    public static class StyleCrowdReviewStressBatch
    {
        public static IReadOnlyList<StyleCrowdReviewSpec> Build()
        {
            var specs = new List<StyleCrowdReviewSpec>(50);

            AddRole(specs, "Police", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard9", "jacket", "brous10"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt2", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard6", "jacket", "brous6")
            });

            AddRole(specs, "EMS", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard5", "jacket", "brous10"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "tshirt1", "brous5"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard2", "tshirt1", "brous2")
            });

            AddRole(specs, "BlueCollar", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard10", "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard8", "jacket", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "tshirt1", "brous5")
            });

            AddRole(specs, "Jogger", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt2", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard3", "tshirt1", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt2", "brous1")
            });

            AddRole(specs, "Hunter", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard7", "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard4", "tshirt2", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard9", "jacket", "brous6")
            });

            AddRole(specs, "ParkRanger", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "brous5"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard1", "jacket", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "brous9")
            });

            AddRole(specs, "Hiker", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard6", "jacket", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt2", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt1", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard3", "tshirt1", "brous4")
            });

            AddRole(specs, "WhiteCollar", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard2", "jacket", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "tshirt2", "brous1")
            });

            AddRole(specs, "Student", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt1", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt2", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard5", "jacket", "brous4"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt1", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt2", "brous6")
            });

            AddRole(specs, "RoughLiving", new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard10", "tshirt1", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard8", "jacket", "brous10"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard7", "tshirt2", "brous6")
            });

            return specs;
        }

        private static void AddRole(List<StyleCrowdReviewSpec> specs, string archetype, RoleTemplate[] templates)
        {
            if (templates == null || templates.Length == 0)
            {
                throw new ArgumentException("Templates are required.", nameof(templates));
            }

            for (var i = 0; i < templates.Length; i++)
            {
                var template = templates[i];
                specs.Add(new StyleCrowdReviewSpec(
                    $"{archetype}_Stress_{i + 1:00}",
                    archetype,
                    StyleCrowdReviewBatchKind.Stress,
                    template.Gender,
                    template.HairId,
                    template.BeardId,
                    template.TopId,
                    template.EyebrowId,
                    template.BottomId));
            }
        }

        private static RoleTemplate CreateTemplate(
            StyleCrowdReviewGender gender,
            string hairId,
            string beardId,
            string topId,
            string eyebrowId)
        {
            return new RoleTemplate(gender, hairId, beardId, topId, eyebrowId, StyleCrowdReviewCatalog.RequiredBottomId);
        }

        private readonly struct RoleTemplate
        {
            public RoleTemplate(StyleCrowdReviewGender gender, string hairId, string beardId, string topId, string eyebrowId, string bottomId)
            {
                Gender = gender;
                HairId = hairId;
                BeardId = beardId;
                TopId = topId;
                EyebrowId = eyebrowId;
                BottomId = bottomId;
            }

            public StyleCrowdReviewGender Gender { get; }
            public string HairId { get; }
            public string BeardId { get; }
            public string TopId { get; }
            public string EyebrowId { get; }
            public string BottomId { get; }
        }
    }
}
