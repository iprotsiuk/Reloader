using Reloader.UI.Toolkit.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Editor
{
    [InitializeOnLoad]
    public static class UiToolkitPanelSettingsSetup
    {
        private const string SetupKey = "Reloader.UI.PanelSettingsSetup.v1";
        private const string PanelSettingsPath = "Assets/_Project/UI/Data/RuntimePanelSettings.asset";
        private const string BeltHudPrefabPath = "Assets/_Project/UI/Prefabs/BeltHud.prefab";

        static UiToolkitPanelSettingsSetup()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        [MenuItem("Reloader/UI/Setup Runtime Panel Settings")]
        public static void ForceSetup()
        {
            SessionState.EraseBool(SetupKey);
            EnsureSetup();
        }

        private static void EnsureSetup()
        {
            if (SessionState.GetBool(SetupKey, false))
            {
                return;
            }

            SessionState.SetBool(SetupKey, true);

            var panelSettings = EnsurePanelSettingsAsset();
            if (panelSettings == null)
            {
                Debug.LogWarning("UI Toolkit setup skipped: could not create/find RuntimePanelSettings asset.");
                return;
            }

            if (!AssignPanelSettingsToBeltHudPrefab(panelSettings))
            {
                Debug.LogWarning("UI Toolkit setup skipped: BeltHud prefab not found or has no UiToolkitRuntimeInstaller.");
                return;
            }

            Debug.Log("UI Toolkit setup: RuntimePanelSettings asset created/updated and assigned to BeltHud prefab.");
        }

        private static PanelSettings EnsurePanelSettingsAsset()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "RuntimePanelSettings";
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            if (panelSettings.themeStyleSheet == null)
            {
                var themeGuids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
                for (var i = 0; i < themeGuids.Length; i++)
                {
                    var candidatePath = AssetDatabase.GUIDToAssetPath(themeGuids[i]);
                    if (string.IsNullOrWhiteSpace(candidatePath))
                    {
                        continue;
                    }

                    if (candidatePath.IndexOf("Default", System.StringComparison.OrdinalIgnoreCase) < 0
                        && candidatePath.IndexOf("Runtime", System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(candidatePath);
                    if (theme == null)
                    {
                        continue;
                    }

                    panelSettings.themeStyleSheet = theme;
                    EditorUtility.SetDirty(panelSettings);
                    break;
                }
            }

            AssetDatabase.SaveAssets();
            return panelSettings;
        }

        private static bool AssignPanelSettingsToBeltHudPrefab(PanelSettings panelSettings)
        {
            var root = PrefabUtility.LoadPrefabContents(BeltHudPrefabPath);
            if (root == null)
            {
                return false;
            }

            var installer = root.GetComponent<UiToolkitRuntimeInstaller>();
            if (installer == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return false;
            }

            var serializedInstaller = new SerializedObject(installer);
            serializedInstaller.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            serializedInstaller.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BeltHudPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
