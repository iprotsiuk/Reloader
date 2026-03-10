using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.Core.Save.Modules;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class MainTownPopulationSpawnedCivilian : MonoBehaviour
    {
        [SerializeField] private string _civilianId = string.Empty;
        [SerializeField] private string _firstName = string.Empty;
        [SerializeField] private string _lastName = string.Empty;
        [SerializeField] private string _nickname = string.Empty;
        [SerializeField] private string _publicDisplayName = string.Empty;
        [SerializeField] private string _populationSlotId = string.Empty;
        [SerializeField] private string _poolId = string.Empty;
        [SerializeField] private string _spawnAnchorId = string.Empty;
        [SerializeField] private string _areaTag = string.Empty;

        private DialogueDefinition _runtimeDialogueDefinition;

        public string CivilianId => _civilianId;
        public string FirstName => _firstName;
        public string LastName => _lastName;
        public string Nickname => _nickname;
        public string PublicDisplayName => _publicDisplayName;
        public string PopulationSlotId => _populationSlotId;
        public string PoolId => _poolId;
        public string SpawnAnchorId => _spawnAnchorId;
        public string AreaTag => _areaTag;

        public void Initialize(CivilianPopulationRecord record)
        {
            _civilianId = record?.CivilianId ?? string.Empty;
            _firstName = record?.FirstName ?? string.Empty;
            _lastName = record?.LastName ?? string.Empty;
            _nickname = record?.Nickname ?? string.Empty;
            _publicDisplayName = BuildPublicDisplayName(record);
            _populationSlotId = record?.PopulationSlotId ?? string.Empty;
            _poolId = record?.PoolId ?? string.Empty;
            _spawnAnchorId = record?.SpawnAnchorId ?? string.Empty;
            _areaTag = record?.AreaTag ?? string.Empty;

            var appearanceApplicator = GetComponent<MainTownNpcAppearanceApplicator>();
            appearanceApplicator?.Apply(record);

            ConfigureRuntimeDialogue(record);
        }

        private void OnDestroy()
        {
            if (_runtimeDialogueDefinition != null)
            {
                DestroyImmediate(_runtimeDialogueDefinition);
                _runtimeDialogueDefinition = null;
            }
        }

        private void ConfigureRuntimeDialogue(CivilianPopulationRecord record)
        {
            var capability = GetComponent<DialogueCapability>();
            if (capability == null)
            {
                return;
            }

            if (_runtimeDialogueDefinition != null)
            {
                DestroyImmediate(_runtimeDialogueDefinition);
                _runtimeDialogueDefinition = null;
            }

            _runtimeDialogueDefinition = BuildRuntimeDialogueDefinition(record, _publicDisplayName);
            capability.ConfigureRuntimeDefinition(_runtimeDialogueDefinition, "Talk");
        }

        private static DialogueDefinition BuildRuntimeDialogueDefinition(CivilianPopulationRecord record, string publicDisplayName)
        {
            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            var safeId = string.IsNullOrWhiteSpace(record?.CivilianId) ? "unknown" : record.CivilianId.Trim();
            var safeDisplayName = string.IsNullOrWhiteSpace(publicDisplayName) ? safeId : publicDisplayName.Trim();
            var entryNode = new DialogueNodeDefinition(
                "entry",
                BuildIdentityLine(safeDisplayName),
                new[]
                {
                    new DialogueReplyDefinition("reply.leave", "Alright.", string.Empty, string.Empty, string.Empty)
                });

            SetPrivateField(definition, "_dialogueId", $"dialogue.procedural.{safeId}");
            SetPrivateField(definition, "_entryNodeId", "entry");
            SetPrivateField(definition, "_nodes", new[] { entryNode });
            return definition;
        }

        private static string BuildIdentityLine(string publicDisplayName)
        {
            return string.IsNullOrWhiteSpace(publicDisplayName)
                ? "Yeah?"
                : $"I'm {publicDisplayName}.";
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static string BuildPublicDisplayName(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            var firstName = record.FirstName?.Trim() ?? string.Empty;
            var lastName = record.LastName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return record.CivilianId ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                return lastName;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                return firstName;
            }

            return string.Concat(firstName, " ", lastName);
        }
    }
}
