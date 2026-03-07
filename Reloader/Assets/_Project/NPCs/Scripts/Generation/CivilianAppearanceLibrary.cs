using System;

namespace Reloader.NPCs.Generation
{
    [Serializable]
    public sealed class CivilianAppearanceLibrary
    {
        public string[] BaseBodyIds { get; set; } = Array.Empty<string>();
        public string[] PresentationTypes { get; set; } = Array.Empty<string>();
        public string[] HairIds { get; set; } = Array.Empty<string>();
        public string[] HairColorIds { get; set; } = Array.Empty<string>();
        public string[] BeardIds { get; set; } = Array.Empty<string>();
        public string[] OutfitTopIds { get; set; } = Array.Empty<string>();
        public string[] OutfitBottomIds { get; set; } = Array.Empty<string>();
        public string[] OuterwearIds { get; set; } = Array.Empty<string>();
        public string[] MaterialColorIds { get; set; } = Array.Empty<string>();
        public string[] DescriptionTags { get; set; } = Array.Empty<string>();
    }
}
