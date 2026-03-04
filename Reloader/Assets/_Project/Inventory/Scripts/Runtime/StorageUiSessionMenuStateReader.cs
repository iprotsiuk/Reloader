using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Inventory
{
    public sealed class StorageUiSessionMenuStateReader : MonoBehaviour, IExternalMenuStateReader
    {
        public bool IsStorageUiOpen => StorageUiSession.IsOpen;
    }
}
