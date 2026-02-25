using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    [CustomPropertyDrawer(typeof(Bag))]
    public class BagPropertyDrawer : PropertyDrawer {

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {

            var root = new VisualElement();
            if (property.managedReferenceValue != null) { root.Add(new PropertyField(property)); return root; }


            var types = TypeCacheUtil.GetConcreteSubclasses<Bag>();
            var dropdown = new DropdownField("Select a bag type");
            dropdown.choices.AddRange(types.Select(b => b.FullName).ToList());
            dropdown.formatSelectedValueCallback = (i) => dropdown.index == -1 ? "<none>" : i;
            dropdown.RegisterValueChangedCallback((_) => {
                Debug.Log($"should create a bag of type {types[dropdown.index]}");
                root.Add(new Label { text = "Should show property below" + property.name });
            });

            root.Add(dropdown);
            return root;
        }
    }
}