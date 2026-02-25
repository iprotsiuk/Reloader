using System;
using UnityEngine;

namespace Reloader.Economy
{
    [Serializable]
    public sealed class ShopCatalogItemDefinition
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _category;
        [SerializeField] private int _unitPrice;
        [SerializeField] private int _startingStock = 10000;
        [SerializeField] private GameObject _iconSourcePrefab;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public string Category => _category;
        public int UnitPrice => Mathf.Max(0, _unitPrice);
        public int StartingStock => Mathf.Max(0, _startingStock);
        public GameObject IconSourcePrefab => _iconSourcePrefab;
    }
}
