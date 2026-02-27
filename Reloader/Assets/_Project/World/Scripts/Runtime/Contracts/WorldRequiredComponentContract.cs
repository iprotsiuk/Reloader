using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Reloader.World.Contracts
{
    [Serializable]
    public sealed class WorldRequiredComponentContract
    {
        [SerializeField] private string _objectPath = string.Empty;
        [SerializeField] private UnityObject _componentScriptAsset;
        [SerializeField] private string _componentTypeName = string.Empty;
        [SerializeField] private List<string> _requiredNonNullObjectReferenceFields = new();
        [SerializeField] private List<string> _requiredNonEmptyArrayFields = new();
        [SerializeField] private List<string> _requiredNonEmptyStringFields = new();

        public string ObjectPath
        {
            get => _objectPath;
            set => _objectPath = value ?? string.Empty;
        }

        public UnityObject ComponentScriptAsset
        {
            get => _componentScriptAsset;
            set => _componentScriptAsset = value;
        }

        public string ComponentTypeName
        {
            get => _componentTypeName;
            set => _componentTypeName = value ?? string.Empty;
        }

        public List<string> RequiredNonNullObjectReferenceFields => _requiredNonNullObjectReferenceFields;
        public List<string> RequiredNonEmptyArrayFields => _requiredNonEmptyArrayFields;
        public List<string> RequiredNonEmptyStringFields => _requiredNonEmptyStringFields;
    }
}
