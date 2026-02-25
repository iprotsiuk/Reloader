using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Economy
{
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Reloader/Economy/Shop Catalog")]
    public sealed class ShopCatalogDefinition : ScriptableObject
    {
        [SerializeField] private List<ShopCatalogItemDefinition> _items = new List<ShopCatalogItemDefinition>();

        public IReadOnlyList<ShopCatalogItemDefinition> Items => _items;
    }
}
