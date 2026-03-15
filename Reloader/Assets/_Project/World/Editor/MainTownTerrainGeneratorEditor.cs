using UnityEditor;
using UnityEngine;

namespace Reloader.World.Editor
{
    [CustomEditor(typeof(MainTownTerrainGenerator))]
    public sealed class MainTownTerrainGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var generator = (MainTownTerrainGenerator)target;
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Regenerate Terrain"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Regenerate MainTown Terrain");
                    generator.RegenerateInEditor();
                }

                if (GUILayout.Button("Repaint Terrain Layers"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Repaint MainTown Terrain Layers");
                    generator.RepaintTerrainLayersInEditor();
                }

                EditorGUILayout.Space();

                var presetProperty = serializedObject.FindProperty("presetAsset");
                var hasPreset = presetProperty != null && presetProperty.objectReferenceValue != null;

                using (new EditorGUI.DisabledScope(!hasPreset))
                {
                    if (GUILayout.Button("Load Preset"))
                    {
                        Undo.RegisterCompleteObjectUndo(generator, "Load MainTown Terrain Preset");
                        generator.ApplyPresetInEditor((MainTownTerrainGeneratorPreset)presetProperty.objectReferenceValue);
                        serializedObject.Update();
                    }

                    if (GUILayout.Button("Save Current To Preset"))
                    {
                        generator.CapturePresetInEditor((MainTownTerrainGeneratorPreset)presetProperty.objectReferenceValue);
                    }
                }

                if (!hasPreset)
                {
                    EditorGUILayout.HelpBox("Assign a MainTownTerrainGeneratorPreset to save or restore a terrain setup.", MessageType.Info);
                }
            }
        }
    }
}
