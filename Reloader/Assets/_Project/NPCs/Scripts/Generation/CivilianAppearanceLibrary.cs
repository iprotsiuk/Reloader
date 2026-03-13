using System;
using UnityEngine;

namespace Reloader.NPCs.Generation
{
    [Serializable]
    public sealed class CivilianAppearanceLibrary
    {
        [SerializeField] private string[] _baseBodyIds = Array.Empty<string>();
        [SerializeField] private string[] _presentationTypes = Array.Empty<string>();
        [SerializeField] private string[] _hairIds = Array.Empty<string>();
        [SerializeField] private string[] _hairColorIds = Array.Empty<string>();
        [SerializeField] private string[] _eyebrowIds = { "brous1" };
        [SerializeField] private string[] _beardIds = Array.Empty<string>();
        [SerializeField] private string[] _outfitTopIds = Array.Empty<string>();
        [SerializeField] private string[] _outfitBottomIds = Array.Empty<string>();
        [SerializeField] private string[] _outerwearIds = Array.Empty<string>();
        [SerializeField] private string[] _materialColorIds = Array.Empty<string>();
        [SerializeField] private string[] _descriptionTags = Array.Empty<string>();

        public string[] BaseBodyIds { get => _baseBodyIds; set => _baseBodyIds = value ?? Array.Empty<string>(); }
        public string[] PresentationTypes { get => _presentationTypes; set => _presentationTypes = value ?? Array.Empty<string>(); }
        public string[] HairIds { get => _hairIds; set => _hairIds = value ?? Array.Empty<string>(); }
        public string[] HairColorIds { get => _hairColorIds; set => _hairColorIds = value ?? Array.Empty<string>(); }
        public string[] EyebrowIds { get => _eyebrowIds; set => _eyebrowIds = value ?? Array.Empty<string>(); }
        public string[] BeardIds { get => _beardIds; set => _beardIds = value ?? Array.Empty<string>(); }
        public string[] OutfitTopIds { get => _outfitTopIds; set => _outfitTopIds = value ?? Array.Empty<string>(); }
        public string[] OutfitBottomIds { get => _outfitBottomIds; set => _outfitBottomIds = value ?? Array.Empty<string>(); }
        public string[] OuterwearIds { get => _outerwearIds; set => _outerwearIds = value ?? Array.Empty<string>(); }
        public string[] MaterialColorIds { get => _materialColorIds; set => _materialColorIds = value ?? Array.Empty<string>(); }
        public string[] DescriptionTags { get => _descriptionTags; set => _descriptionTags = value ?? Array.Empty<string>(); }
    }
}
