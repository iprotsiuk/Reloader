using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEditor;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class PlayerRootDeathHookEditModeTests
    {
        private const string PlayerRootPrefabPath = "Assets/_Project/Player/Prefabs/PlayerRoot.prefab";

        [Test]
        public void PlayerRootPrefab_AuthorsDamageReceiverAndDeathContractBridge()
        {
            var bridgeType = ResolveBridgeType();
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected PlayerRoot prefab to load.");
                Assert.That(prefabRoot.GetComponent<HumanoidDamageReceiver>(), Is.Not.Null,
                    "Expected PlayerRoot to expose a live damage receiver so lethal impacts have a concrete producer.");
                Assert.That(prefabRoot.GetComponent(bridgeType), Is.Not.Null,
                    "Expected PlayerRoot to author the death-contract bridge on the canonical runtime root.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void PlayerRootPrefab_HumanoidDamageReceiverSerializesOneHundredMaxHealth()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerRootPrefabPath);

            try
            {
                Assert.That(prefabRoot, Is.Not.Null, "Expected PlayerRoot prefab to load.");
                var receiver = prefabRoot.GetComponent<HumanoidDamageReceiver>();
                Assert.That(receiver, Is.Not.Null, "Expected PlayerRoot to expose the shared damage receiver.");

                var serializedReceiver = new SerializedObject(receiver);
                var maxHealth = serializedReceiver.FindProperty("_maxHealth");
                Assert.That(maxHealth, Is.Not.Null, "Expected HumanoidDamageReceiver to serialize _maxHealth.");
                Assert.That(maxHealth.floatValue, Is.EqualTo(100f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static System.Type ResolveBridgeType()
        {
            var type = System.Type.GetType("Reloader.NPCs.Combat.PlayerDeathContractBridge, Reloader.NPCs", throwOnError: false);
            Assert.That(type, Is.Not.Null, "Expected PlayerDeathContractBridge type.");
            return type;
        }
    }
}
