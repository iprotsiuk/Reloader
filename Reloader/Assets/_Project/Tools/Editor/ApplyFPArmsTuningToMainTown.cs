#if UNITY_EDITOR
using Reloader.Tools.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Tools.Editor
{
    public static class ApplyFPArmsTuningToMainTown
    {
        private const string TuningScenePath = "Assets/_Project/World/Scenes/FPArmsTuning/FPArmsTuning.unity";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";

        [MenuItem("Reloader/Tools/FP Arms/Apply Tuning To MainTown")]
        public static void Apply()
        {
            var sourceWasOpen = IsSceneOpen(TuningScenePath, out var sourceScene);
            var targetWasOpen = IsSceneOpen(MainTownScenePath, out var targetScene);

            if (!sourceWasOpen)
            {
                sourceScene = EditorSceneManager.OpenScene(TuningScenePath, OpenSceneMode.Additive);
            }

            if (!targetWasOpen)
            {
                targetScene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);
            }

            try
            {
                if (!TryFindPlayerRoot(sourceScene, out var sourceRoot) || !TryFindPlayerRoot(targetScene, out var targetRoot))
                {
                    Debug.LogError("Apply FP Arms Tuning failed: PlayerRoot not found in one of the scenes.");
                    return;
                }

                var sourceArms = sourceRoot.transform.Find("CameraPivot/PlayerArms");
                var sourceVisual = sourceRoot.transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");
                var targetArms = targetRoot.transform.Find("CameraPivot/PlayerArms");
                var targetVisual = targetRoot.transform.Find("CameraPivot/PlayerArms/PlayerArmsVisual");

                if (sourceArms == null || targetArms == null)
                {
                    Debug.LogError("Apply FP Arms Tuning failed: PlayerArms transform not found.");
                    return;
                }

                CopyLocalTransform(sourceArms, targetArms);
                if (sourceVisual != null && targetVisual != null)
                {
                    CopyLocalTransform(sourceVisual, targetVisual);
                }

                var sourceTuner = sourceRoot.GetComponent<FPArmsTuningController>();
                var targetTuner = targetRoot.GetComponent<FPArmsTuningController>();
                if (targetTuner == null)
                {
                    targetTuner = Undo.AddComponent<FPArmsTuningController>(targetRoot);
                }

                var targetSo = new SerializedObject(targetTuner);
                targetSo.FindProperty("_editorPreviewEnabled")?.SetBoolValueSafe(true);
                targetSo.FindProperty("_startEquipped")?.SetBoolValueSafe(true);
                targetSo.FindProperty("_forceEquippedPose")?.SetBoolValueSafe(true);
                targetSo.FindProperty("_forceRifleIdleClipPose")?.SetBoolValueSafe(true);

                if (sourceTuner != null)
                {
                    var sourceSo = new SerializedObject(sourceTuner);
                    CopyProperty(sourceSo, targetSo, "_gripLocalPosition");
                    CopyProperty(sourceSo, targetSo, "_gripLocalEuler");
                    CopyProperty(sourceSo, targetSo, "_preferredGripBoneName");
                    CopyProperty(sourceSo, targetSo, "_editorRiflePrefabPath");
                    CopyProperty(sourceSo, targetSo, "_rifleIdleStateName");
                }

                targetSo.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(targetRoot);
                EditorUtility.SetDirty(targetArms);
                if (targetVisual != null)
                {
                    EditorUtility.SetDirty(targetVisual);
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
                AssetDatabase.SaveAssets();

                Debug.Log("Applied FPArms tuning to MainTown PlayerRoot.");
            }
            finally
            {
                if (!sourceWasOpen && sourceScene.IsValid())
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }

                if (!targetWasOpen && targetScene.IsValid())
                {
                    EditorSceneManager.CloseScene(targetScene, false);
                }
            }
        }

        private static bool IsSceneOpen(string path, out Scene scene)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var current = SceneManager.GetSceneAt(i);
                if (current.path == path)
                {
                    scene = current;
                    return true;
                }
            }

            scene = default;
            return false;
        }

        private static bool TryFindPlayerRoot(Scene scene, out GameObject root)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == "PlayerRoot")
                {
                    root = roots[i];
                    return true;
                }
            }

            root = null;
            return false;
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            Undo.RecordObject(target, "Apply FP arms tuning transform");
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void CopyProperty(SerializedObject source, SerializedObject target, string propertyName)
        {
            var src = source.FindProperty(propertyName);
            var dst = target.FindProperty(propertyName);
            if (src == null || dst == null)
            {
                return;
            }

            switch (src.propertyType)
            {
                case SerializedPropertyType.Vector3:
                    dst.vector3Value = src.vector3Value;
                    break;
                case SerializedPropertyType.String:
                    dst.stringValue = src.stringValue;
                    break;
                case SerializedPropertyType.Boolean:
                    dst.boolValue = src.boolValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = src.objectReferenceValue;
                    break;
            }
        }

        private static void SetBoolValueSafe(this SerializedProperty property, bool value)
        {
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }
    }
}
#endif
