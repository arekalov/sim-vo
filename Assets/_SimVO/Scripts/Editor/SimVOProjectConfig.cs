using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace SimVO.EditorTools
{
    /// <summary>Physics layers, player settings and OpenXR features for Quest and desktop.</summary>
    public static class SimVOProjectConfig
    {
        static readonly string[] QuestFeatures =
        {
            "MetaQuestFeature",
            "OculusTouchControllerProfile",
            "MetaQuestTouchPlusControllerProfile",
            "MetaQuestTouchProControllerProfile",
        };

        public static void Apply()
        {
            ConfigureLayers();
            ConfigurePlayer();
            ConfigureOpenXR();
            AssetDatabase.SaveAssets();
            Debug.Log("SimVO: project configured");
        }

        static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(6).stringValue = "Weapon";
            layers.GetArrayElementAtIndex(7).stringValue = "Target";
            tagManager.ApplyModifiedProperties();
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "SimVO Team";
            PlayerSettings.productName = "SimVO";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.simvo.tir");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.simvo.tir");
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }

        static void ConfigureOpenXR()
        {
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var android = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (android == null)
            {
                Debug.LogWarning("SimVO: no OpenXR settings for Android");
                return;
            }
            foreach (var feature in android.GetFeatures())
            {
                var name = feature.GetType().Name;
                if (QuestFeatures.Contains(name))
                {
                    feature.enabled = true;
                    Debug.Log($"SimVO: OpenXR Android feature enabled: {name}");
                }
                else if (name.StartsWith("AndroidXR"))
                {
                    feature.enabled = false;
                }
            }
            EditorUtility.SetDirty(android);

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            var standalone = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (standalone != null)
            {
                foreach (var feature in standalone.GetFeatures())
                {
                    if (feature.GetType().Name == "OculusTouchControllerProfile") feature.enabled = true;
                }
                EditorUtility.SetDirty(standalone);
            }
        }
    }
}
