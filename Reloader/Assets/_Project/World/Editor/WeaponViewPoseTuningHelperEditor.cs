#if UNITY_EDITOR
using System.Text;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEditor;
using UnityEngine;

namespace Reloader.World.Editor
{
    [CustomEditor(typeof(WeaponViewPoseTuningHelper))]
    public sealed class WeaponViewPoseTuningHelperEditor : UnityEditor.Editor
    {
        private const string CapturedPayloadSessionKey = "Reloader.World.Editor.WeaponViewPoseTuningHelperEditor.CapturedPayload";
        private bool _playModeBufferInitialized;
        private bool _playModeBufferIsAttachmentOverride;
        private WeaponAttachmentSlotType _playModeBufferSlotType;
        private string _playModeBufferAttachmentItemId = string.Empty;
        private WeaponViewPoseTuningValues _playModeBufferValues;

        [System.Serializable]
        private sealed class CapturedPosePayload
        {
            public string TargetKey;
            public bool IsAttachmentOverride;
            public int SlotType;
            public string AttachmentItemId;
            public WeaponViewPoseTuningValues Values;
        }

        public override void OnInspectorGUI()
        {
            var helper = (WeaponViewPoseTuningHelper)target;
            if (helper == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                DrawPlayModeRuntimeTuning(helper);
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawDefaultInspector();
                }
                return;
            }

            DrawDefaultInspector();
            EditorGUILayout.Space();
            DrawCapturedRuntimeValues(helper);
        }

        private void DrawPlayModeRuntimeTuning(WeaponViewPoseTuningHelper helper)
        {
            EditorGUILayout.HelpBox(
                "Use this play-mode section for live pose tuning. It writes directly into the runtime helper and caches the values so they can be applied back in edit mode after play stops.",
                MessageType.Info);

            if (!helper.TryGetRuntimeTuningContext(out var context))
            {
                EditorGUILayout.HelpBox("No active weapon pose tuning target is currently equipped.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Play Mode Override", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Target Weapon", string.IsNullOrWhiteSpace(helper.TargetWeaponItemId) ? "<any>" : helper.TargetWeaponItemId);
            EditorGUILayout.Slider("ADS Blend", context.AdsBlendT, 0f, 1f);
            EditorGUILayout.Toggle("Magnified Scoped Alignment", context.UsesMagnifiedScopedAlignment);

            if (context.HasActiveAttachmentOverride)
            {
                SyncPlayModeBuffer(context.SlotType, context.AttachmentItemId, context.Values);
                EditorGUILayout.LabelField("Slot", context.SlotType.ToString());
                EditorGUILayout.LabelField("Attachment", context.AttachmentItemId);
                if (context.UsesMagnifiedScopedAlignment)
                {
                    EditorGUILayout.HelpBox(
                        "Magnified scoped ADS is camera-authoritative. Root ADS position/euler remain coarse presentation inputs, while Scoped Eye Relief Back Offset is the effective full-ADS tuning control.",
                        MessageType.None);
                }

                _playModeBufferValues = DrawValuesEditor(_playModeBufferValues);

                if (GUILayout.Button("Apply Runtime Override Values"))
                {
                    helper.TrySetAttachmentPoseOverride(context.SlotType, context.AttachmentItemId, _playModeBufferValues);
                    CacheCapturedPayload(CreateAttachmentPayload(helper, context.SlotType, context.AttachmentItemId, _playModeBufferValues));
                    EditorUtility.SetDirty(helper);
                }

                if (GUILayout.Button("Capture Current Runtime Override"))
                {
                    CacheCapturedPayload(CreateAttachmentPayload(helper, context.SlotType, context.AttachmentItemId, _playModeBufferValues));
                }

                return;
            }

            SyncPlayModeBaseBuffer(context.Values);
            _playModeBufferValues = DrawValuesEditor(_playModeBufferValues);

            if (GUILayout.Button("Apply Runtime Base Pose Values"))
            {
                helper.SetBasePoseValues(_playModeBufferValues);
                CacheCapturedPayload(CreateBasePayload(helper, _playModeBufferValues));
                EditorUtility.SetDirty(helper);
            }

            if (GUILayout.Button("Capture Current Runtime Base Pose"))
            {
                CacheCapturedPayload(CreateBasePayload(helper, _playModeBufferValues));
            }
        }

        private void DrawCapturedRuntimeValues(WeaponViewPoseTuningHelper helper)
        {
            if (!TryLoadCapturedPayload(out var payload) || payload == null)
            {
                return;
            }

            if (!string.Equals(payload.TargetKey, BuildTargetKey(helper), System.StringComparison.Ordinal))
            {
                return;
            }

            EditorGUILayout.LabelField("Captured Play Mode Values", EditorStyles.boldLabel);
            if (payload.IsAttachmentOverride)
            {
                EditorGUILayout.LabelField("Slot", ((WeaponAttachmentSlotType)payload.SlotType).ToString());
                EditorGUILayout.LabelField("Attachment", payload.AttachmentItemId);
            }
            else
            {
                EditorGUILayout.LabelField("Pose", "Base");
            }

            DrawValuesReadOnly(payload.Values);

            if (GUILayout.Button("Apply Captured Values To Serialized Helper"))
            {
                ApplyCapturedPayload(helper, payload);
            }

            if (GUILayout.Button("Clear Captured Values"))
            {
                SessionState.EraseString(CapturedPayloadSessionKey);
            }
        }

        private static WeaponViewPoseTuningValues DrawValuesEditor(WeaponViewPoseTuningValues values)
        {
            values.HipLocalPosition = DrawDelayedVector3Field("Hip Local Position", values.HipLocalPosition);
            values.HipLocalEuler = DrawDelayedVector3Field("Hip Local Euler", values.HipLocalEuler);
            values.AdsLocalPosition = DrawDelayedVector3Field("Ads Local Position", values.AdsLocalPosition);
            values.AdsLocalEuler = DrawDelayedVector3Field("Ads Local Euler", values.AdsLocalEuler);
            values.BlendSpeed = EditorGUILayout.DelayedFloatField("Blend Speed", values.BlendSpeed);
            values.RifleLocalEulerOffset = DrawDelayedVector3Field("Rifle Local Euler Offset", values.RifleLocalEulerOffset);
            values.ScopedAdsEyeReliefBackOffset = EditorGUILayout.DelayedFloatField("Scoped Eye Relief Back Offset", values.ScopedAdsEyeReliefBackOffset);
            return values;
        }

        private static Vector3 DrawDelayedVector3Field(string label, Vector3 value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            value.x = EditorGUILayout.DelayedFloatField("X", value.x);
            value.y = EditorGUILayout.DelayedFloatField("Y", value.y);
            value.z = EditorGUILayout.DelayedFloatField("Z", value.z);
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private static void DrawValuesReadOnly(WeaponViewPoseTuningValues values)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("Hip Local Position", values.HipLocalPosition);
                EditorGUILayout.Vector3Field("Hip Local Euler", values.HipLocalEuler);
                EditorGUILayout.Vector3Field("Ads Local Position", values.AdsLocalPosition);
                EditorGUILayout.Vector3Field("Ads Local Euler", values.AdsLocalEuler);
                EditorGUILayout.FloatField("Blend Speed", values.BlendSpeed);
                EditorGUILayout.Vector3Field("Rifle Local Euler Offset", values.RifleLocalEulerOffset);
                EditorGUILayout.FloatField("Scoped Eye Relief Back Offset", values.ScopedAdsEyeReliefBackOffset);
            }
        }

        private static CapturedPosePayload CreateAttachmentPayload(
            WeaponViewPoseTuningHelper helper,
            WeaponAttachmentSlotType slotType,
            string attachmentItemId,
            WeaponViewPoseTuningValues values)
        {
            return new CapturedPosePayload
            {
                TargetKey = BuildTargetKey(helper),
                IsAttachmentOverride = true,
                SlotType = (int)slotType,
                AttachmentItemId = attachmentItemId,
                Values = values
            };
        }

        private static CapturedPosePayload CreateBasePayload(
            WeaponViewPoseTuningHelper helper,
            WeaponViewPoseTuningValues values)
        {
            return new CapturedPosePayload
            {
                TargetKey = BuildTargetKey(helper),
                IsAttachmentOverride = false,
                SlotType = -1,
                AttachmentItemId = string.Empty,
                Values = values
            };
        }

        private static void CacheCapturedPayload(CapturedPosePayload payload)
        {
            SessionState.SetString(CapturedPayloadSessionKey, EditorJsonUtility.ToJson(payload));
        }

        private static bool TryLoadCapturedPayload(out CapturedPosePayload payload)
        {
            var json = SessionState.GetString(CapturedPayloadSessionKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                payload = null;
                return false;
            }

            payload = new CapturedPosePayload();
            EditorJsonUtility.FromJsonOverwrite(json, payload);
            return true;
        }

        private static void ApplyCapturedPayload(WeaponViewPoseTuningHelper helper, CapturedPosePayload payload)
        {
            var serializedObject = new SerializedObject(helper);
            if (payload.IsAttachmentOverride)
            {
                ApplyAttachmentOverride(serializedObject, payload);
            }
            else
            {
                ApplyBaseValues(serializedObject, payload.Values);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(helper);
        }

        private static void ApplyBaseValues(SerializedObject serializedObject, WeaponViewPoseTuningValues values)
        {
            serializedObject.FindProperty("_hipLocalPosition").vector3Value = values.HipLocalPosition;
            serializedObject.FindProperty("_hipLocalEuler").vector3Value = values.HipLocalEuler;
            serializedObject.FindProperty("_adsLocalPosition").vector3Value = values.AdsLocalPosition;
            serializedObject.FindProperty("_adsLocalEuler").vector3Value = values.AdsLocalEuler;
            serializedObject.FindProperty("_blendSpeed").floatValue = Mathf.Max(1f, values.BlendSpeed);
            serializedObject.FindProperty("_rifleLocalEulerOffset").vector3Value = values.RifleLocalEulerOffset;
        }

        private static void ApplyAttachmentOverride(SerializedObject serializedObject, CapturedPosePayload payload)
        {
            var overridesProperty = serializedObject.FindProperty("_attachmentPoseOverrides");
            if (overridesProperty == null || !overridesProperty.isArray)
            {
                return;
            }

            SerializedProperty overrideProperty = null;
            for (var i = 0; i < overridesProperty.arraySize; i++)
            {
                var candidate = overridesProperty.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("_slotType").enumValueIndex != payload.SlotType)
                {
                    continue;
                }

                if (!string.Equals(candidate.FindPropertyRelative("_attachmentItemId").stringValue, payload.AttachmentItemId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                overrideProperty = candidate;
                break;
            }

            if (overrideProperty == null)
            {
                overridesProperty.arraySize += 1;
                overrideProperty = overridesProperty.GetArrayElementAtIndex(overridesProperty.arraySize - 1);
                overrideProperty.FindPropertyRelative("_slotType").enumValueIndex = payload.SlotType;
                overrideProperty.FindPropertyRelative("_attachmentItemId").stringValue = payload.AttachmentItemId;
            }

            overrideProperty.FindPropertyRelative("_hipLocalPosition").vector3Value = payload.Values.HipLocalPosition;
            overrideProperty.FindPropertyRelative("_hipLocalEuler").vector3Value = payload.Values.HipLocalEuler;
            overrideProperty.FindPropertyRelative("_adsLocalPosition").vector3Value = payload.Values.AdsLocalPosition;
            overrideProperty.FindPropertyRelative("_adsLocalEuler").vector3Value = payload.Values.AdsLocalEuler;
            overrideProperty.FindPropertyRelative("_blendSpeed").floatValue = Mathf.Max(1f, payload.Values.BlendSpeed);
            overrideProperty.FindPropertyRelative("_rifleLocalEulerOffset").vector3Value = payload.Values.RifleLocalEulerOffset;
            overrideProperty.FindPropertyRelative("_scopedAdsEyeReliefBackOffset").floatValue = payload.Values.ScopedAdsEyeReliefBackOffset;
        }

        private static string BuildTargetKey(WeaponViewPoseTuningHelper helper)
        {
            var builder = new StringBuilder();
            builder.Append(helper.gameObject.scene.path);
            builder.Append('|');
            builder.Append(helper.TargetWeaponItemId);
            builder.Append('|');
            builder.Append(BuildHierarchyPath(helper.transform));
            return builder.ToString();
        }

        private static string BuildHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(target.name);
            var current = target.parent;
            while (current != null)
            {
                builder.Insert(0, '/');
                builder.Insert(0, current.name);
                current = current.parent;
            }

            return builder.ToString();
        }

        private void SyncPlayModeBuffer(
            WeaponAttachmentSlotType slotType,
            string attachmentItemId,
            WeaponViewPoseTuningValues values)
        {
            if (_playModeBufferInitialized
                && _playModeBufferIsAttachmentOverride
                && _playModeBufferSlotType == slotType
                && string.Equals(_playModeBufferAttachmentItemId, attachmentItemId, System.StringComparison.Ordinal))
            {
                return;
            }

            _playModeBufferInitialized = true;
            _playModeBufferIsAttachmentOverride = true;
            _playModeBufferSlotType = slotType;
            _playModeBufferAttachmentItemId = attachmentItemId;
            _playModeBufferValues = values;
        }

        private void SyncPlayModeBaseBuffer(WeaponViewPoseTuningValues values)
        {
            if (_playModeBufferInitialized && !_playModeBufferIsAttachmentOverride)
            {
                return;
            }

            _playModeBufferInitialized = true;
            _playModeBufferIsAttachmentOverride = false;
            _playModeBufferSlotType = default;
            _playModeBufferAttachmentItemId = string.Empty;
            _playModeBufferValues = values;
        }
    }
}
#endif
