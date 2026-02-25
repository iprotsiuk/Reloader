using UnityEngine;

namespace Reloader.Core.UI
{
    public sealed class ItemIconCatalogProvider : MonoBehaviour
    {
        [SerializeField] private ItemIconCatalog _itemIconCatalog;

        public static ItemIconCatalogProvider Instance { get; private set; }
        public static ItemIconCatalog Catalog => Instance != null ? Instance._itemIconCatalog : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetCatalogForTests(ItemIconCatalog itemIconCatalog)
        {
            _itemIconCatalog = itemIconCatalog;
        }
    }
}
