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
            }
        }
    }
}
