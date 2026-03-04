#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reloader.Tools.Editor
{
    public static class LowPolyShooterAnimationExporter
    {
        private const string SourceRoot = "Assets/Infima Games/Low Poly Shooter Pack - Free Sample/Art/Animations";
        private const string DestinationRoot = "Assets/_Project/Player/Resources/Imported/LPSP_AnimationLibrary";

        [MenuItem("Reloader/Tools/Animations/Export Low Poly Shooter Pack Clips")]
        public static void Export()
        {
            if (!AssetDatabase.IsValidFolder(SourceRoot))
            {
                Debug.LogError($"Source folder not found: {SourceRoot}");
                return;
            }

            EnsureFolderPath(DestinationRoot);

            var assetPaths = AssetDatabase.FindAssets(string.Empty, new[] { SourceRoot });
            var copiedCount = 0;
            var extractedCount = 0;
            var failed = new List<string>();

            foreach (var guid in assetPaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".anim")
                {
                    if (CopyAnim(path))
                    {
                        copiedCount++;
                    }
                    else
                    {
                        failed.Add(path);
                    }

                    continue;
                }

                if (extension == ".fbx")
                {
                    extractedCount += ExtractFbxClips(path, failed);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"LPSP animation export complete. Copied .anim: {copiedCount}, extracted FBX clips: {extractedCount}, failed: {failed.Count}. Destination: {DestinationRoot}");

            if (failed.Count > 0)
            {
                foreach (var path in failed)
                {
                    Debug.LogWarning($"Failed to export clip from: {path}");
                }
            }
        }

        private static bool CopyAnim(string sourcePath)
        {
            var relative = sourcePath.Substring(SourceRoot.Length).TrimStart('/');
            var targetPath = Path.Combine(DestinationRoot, relative).Replace('\\', '/');
            EnsureFolderPath(Path.GetDirectoryName(targetPath)?.Replace('\\', '/') ?? DestinationRoot);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath) != null)
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            return AssetDatabase.CopyAsset(sourcePath, targetPath);
        }

        private static int ExtractFbxClips(string sourcePath, List<string> failed)
        {
            var clipCount = 0;
            var relative = sourcePath.Substring(SourceRoot.Length).TrimStart('/');
            var relativeDir = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
            var fbxName = Path.GetFileNameWithoutExtension(sourcePath);
            var targetDir = string.IsNullOrEmpty(relativeDir)
                ? $"{DestinationRoot}/{fbxName}"
                : $"{DestinationRoot}/{relativeDir}/{fbxName}";

            EnsureFolderPath(targetDir);

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
            foreach (var asset in subAssets)
            {
                if (asset is not AnimationClip sourceClip)
                {
                    continue;
                }

                if (sourceClip.name == "__preview__Take 001")
                {
                    continue;
                }

                var clipAssetName = SanitizeFileName(sourceClip.name);
                var targetPath = $"{targetDir}/{clipAssetName}.anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath) != null)
                {
                    AssetDatabase.DeleteAsset(targetPath);
                }

                var extractError = AssetDatabase.ExtractAsset(sourceClip, targetPath);
                if (string.IsNullOrEmpty(extractError) &&
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath) != null)
                {
                    clipCount++;
                }
                else
                {
                    if (!string.IsNullOrEmpty(extractError))
                    {
                        Debug.LogWarning($"Failed to extract '{sourceClip.name}' from '{sourcePath}': {extractError}");
                    }

                    failed.Add(sourcePath);
                }
            }

            return clipCount;
        }

        private static void EnsureFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            var normalized = folderPath.Replace('\\', '/').Trim('/');
            var parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                return;
            }

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in invalid)
            {
                value = value.Replace(ch, '_');
            }

            return value.Trim();
        }
    }
}
#endif
