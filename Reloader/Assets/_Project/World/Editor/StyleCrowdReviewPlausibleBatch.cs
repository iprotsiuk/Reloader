using System;
using System.Collections.Generic;

namespace Reloader.World.Editor
{
    public static class StyleCrowdReviewPlausibleBatch
    {
        public static IReadOnlyList<StyleCrowdReviewSpec> Build()
        {
            var specs = new List<StyleCrowdReviewSpec>(50);

            AddRole(specs, "Police", 6, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard4", "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "jacket", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "pants1")
            });

            AddRole(specs, "EMS", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "pants1")
            });

            AddRole(specs, "BlueCollar", 8, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", "beard2", "tshirt1", "brous5"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard6", "jacket", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "pants1")
            });

            AddRole(specs, "Jogger", 6, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "tshirt1", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "tshirt2", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "pants1")
            });

            AddRole(specs, "Hunter", 6, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard5", "jacket", "brous9"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard7", "jacket", "brous10"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "pants1")
            });

            AddRole(specs, "ParkRanger", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard1", "jacket", "brous5"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "brous3"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt2", "pants1")
            });

            AddRole(specs, "Hiker", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "jacket", "brous6"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard3", "jacket", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt1", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous7")
            });

            AddRole(specs, "WhiteCollar", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "jacket", "brous1"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "brous2"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt2", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "pants1")
            });

            AddRole(specs, "Student", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "tshirt2", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "tshirt1", "brous4"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "tshirt1", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt2", "brous6")
            });

            AddRole(specs, "RoughLiving", 4, new[]
            {
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.wavy", "beard8", "jacket", "brous7"),
                CreateTemplate(StyleCrowdReviewGender.Male, "hair.parted", "beard10", "jacket", "brous8"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "pants1"),
                CreateTemplate(StyleCrowdReviewGender.Female, "hair.long", string.Empty, "tshirt1", "pants1")
            });

            return specs;
        }

        private static void AddRole(List<StyleCrowdReviewSpec> specs, string archetype, int count, RoleTemplate[] templates)
        {
            if (templates == null || templates.Length == 0)
            {
                throw new ArgumentException("Templates are required.", nameof(templates));
            }

            for (var i = 0; i < count; i++)
            {
                var template = templates[i % templates.Length];
                specs.Add(new StyleCrowdReviewSpec(
                    $"{archetype}_Plausible_{i + 1:00}",
                    archetype,
                    StyleCrowdReviewBatchKind.Plausible,
                    template.Gender,
                    template.HairId,
                    template.BeardId,
                    template.TopId,
                    template.BottomId));
            }
        }

        private static RoleTemplate CreateTemplate(
            StyleCrowdReviewGender gender,
            string hairId,
            string beardId,
            string topId,
            string bottomId)
        {
            return new RoleTemplate(gender, hairId, beardId, topId, bottomId);
        }

        private readonly struct RoleTemplate
        {
            public RoleTemplate(StyleCrowdReviewGender gender, string hairId, string beardId, string topId, string bottomId)
            {
                Gender = gender;
                HairId = hairId;
                BeardId = beardId;
                TopId = topId;
                BottomId = bottomId;
            }

            public StyleCrowdReviewGender Gender { get; }
            public string HairId { get; }
            public string BeardId { get; }
            public string TopId { get; }
            public string BottomId { get; }
        }
    }
}
