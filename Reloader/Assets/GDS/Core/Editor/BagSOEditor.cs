using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GDS.Core {

    [CustomEditor(typeof(BagSO))]
    public class BagSOEditor : Editor {
        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();

            var valueProp = serializedObject.FindProperty("Value");
            if (valueProp.managedReferenceValue != null) { root.Add(new PropertyField(valueProp)); return root; }

            var types = TypeCacheUtil.GetConcreteSubclasses<Bag>();
            var dropdown = new DropdownField("Select a bag type");
            dropdown.choices.AddRange(types.Select(b => $"{b.Name} ({b.FullName})").ToList());
            dropdown.formatSelectedValueCallback = (i) => dropdown.index == -1 ? "<none>" : i;
            dropdown.RegisterValueChangedCallback((_) => {
                var instance = Activator.CreateInstance(types[dropdown.index]);
                valueProp.managedReferenceValue = instance;
                valueProp.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(valueProp.serializedObject.targetObject);

                var field = new PropertyField(valueProp);
                field.Bind(valueProp.serializedObject);
                root.Add(field);
                root.Remove(dropdown);

                AssetDatabase.SaveAssets();
            });

            root.Add(dropdown);
            return root;
        }
    }

}