using System;
using System.Collections.Generic;
using Reloader.Core.Save.Modules;

namespace Reloader.NPCs.Generation
{
    public sealed class CivilianAppearanceGenerator
    {
        public CivilianPopulationRecord GenerateRecord(
            CivilianAppearanceLibrary library,
            string civilianId,
            int createdAtDay,
            string spawnAnchorId,
            int seed,
            bool isContractEligible)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            if (string.IsNullOrWhiteSpace(civilianId))
            {
                throw new ArgumentException("Civilian id is required.", nameof(civilianId));
            }

            if (string.IsNullOrWhiteSpace(spawnAnchorId))
            {
                throw new ArgumentException("Spawn anchor id is required.", nameof(spawnAnchorId));
            }

            var random = new Random(seed);

            return new CivilianPopulationRecord
            {
                CivilianId = civilianId.Trim(),
                IsAlive = true,
                IsContractEligible = isContractEligible,
                BaseBodyId = PickRequired(library.BaseBodyIds, nameof(library.BaseBodyIds), random),
                PresentationType = PickRequired(library.PresentationTypes, nameof(library.PresentationTypes), random),
                HairId = PickRequired(library.HairIds, nameof(library.HairIds), random),
                HairColorId = PickRequired(library.HairColorIds, nameof(library.HairColorIds), random),
                BeardId = PickRequired(library.BeardIds, nameof(library.BeardIds), random),
                OutfitTopId = PickRequired(library.OutfitTopIds, nameof(library.OutfitTopIds), random),
                OutfitBottomId = PickRequired(library.OutfitBottomIds, nameof(library.OutfitBottomIds), random),
                OuterwearId = PickRequired(library.OuterwearIds, nameof(library.OuterwearIds), random),
                MaterialColorIds = PickOptionalList(library.MaterialColorIds, random),
                GeneratedDescriptionTags = PickOptionalList(library.DescriptionTags, random),
                SpawnAnchorId = spawnAnchorId.Trim(),
                CreatedAtDay = Math.Max(0, createdAtDay),
                RetiredAtDay = -1
            };
        }

        private static string PickRequired(IReadOnlyList<string> values, string fieldName, Random random)
        {
            if (values == null || values.Count == 0)
            {
                throw new InvalidOperationException($"Civilian appearance library requires at least one value for {fieldName}.");
            }

            var candidates = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    candidates.Add(values[i].Trim());
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"Civilian appearance library requires non-empty values for {fieldName}.");
            }

            return candidates[random.Next(candidates.Count)];
        }

        private static List<string> PickOptionalList(IReadOnlyList<string> values, Random random)
        {
            var selected = new List<string>();
            if (values == null || values.Count == 0)
            {
                return selected;
            }

            var candidates = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    candidates.Add(values[i].Trim());
                }
            }

            if (candidates.Count == 0)
            {
                return selected;
            }

            selected.Add(candidates[random.Next(candidates.Count)]);
            return selected;
        }
    }
}
