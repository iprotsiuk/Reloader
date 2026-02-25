using UnityEngine;

namespace GDS.Core {

    [CreateAssetMenu(menuName = "SO/Core/Bag")]
    public class BagSO : ScriptableObject {

        [SerializeReference]
        public Bag Value;
    }

}