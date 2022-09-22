using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sturfee.DigitalTwin.Auth;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using SturfeeVPS.Core;

namespace Sturfee.DigitalTwin.Auth.Editor
{
    public class AppKeyEditor 
    {
        public static string AbsolutePath => Path.Combine(Application.dataPath, "Resources", "Sturfee", "Auth", "AppKeys");
        public static string LocalPath => Path.Combine("Assets", "Resources", "Sturfee", "Auth", "AppKeys");
        
        [InitializeOnLoadMethod]
        public static void CreateAppKeyScriptableObjects()
        {
            string dir = AbsolutePath;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            foreach (var platform in Enum.GetValues(typeof(AppKeySupportedPlatforms)).Cast<AppKeySupportedPlatforms>())
            {
                string file = Path.Combine(dir, $"{platform}.asset");
                if (!File.Exists(file))
                {
                    AppKeyConfig config = CreateApiKeyObject(platform);
                    Debug.Log($"ScriptableObject {JsonUtility.ToJson(config)} created ");

                    // CreateAsset works only with project relative/local path
                    AssetDatabase.CreateAsset(config, Path.Combine(LocalPath, $"{platform}.asset"));
                }
                else
                {
                    // Replace/Reset
                }
            }
        }

        [MenuItem("DigitalTwin/Auth/AppKeys/Android")]
        public static void ShowAppKeyConfig_Android()
        {
            ShowAppKeyConfig(AppKeySupportedPlatforms.Android);
        }

        [MenuItem("DigitalTwin/Auth/AppKeys/IOS")]
        public static void ShowAppKeyConfig_IOS()
        {
            ShowAppKeyConfig(AppKeySupportedPlatforms.IOS);
        }

        [MenuItem("DigitalTwin/Auth/AppKeys/Desktop")]
        public static void ShowAppKeyConfig_Desktop()
        {
            ShowAppKeyConfig(AppKeySupportedPlatforms.Desktop);
        }

        private static void ShowAppKeyConfig(AppKeySupportedPlatforms platform)
        {
            var appKeyConfig = Resources.Load<AppKeyConfig>($"Sturfee/Auth/AppKeys/{platform}");
            EditorUtility.FocusProjectWindow();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path.Combine(LocalPath, $"{platform}.asset"));

            if(obj == null)
            {
                CreateApiKeyObject(platform);
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path.Combine(LocalPath, $"{platform}.asset"));
            }

            Selection.activeObject = obj;
        }
        private static AppKeyConfig CreateApiKeyObject(AppKeySupportedPlatforms platform)
        {
            AppKeyConfig config = ScriptableObject.CreateInstance<AppKeyConfig>();
            switch (platform)
            {
                case AppKeySupportedPlatforms.Android:
                    config.SourceHeader = "X-android-package";
                    break;
                case AppKeySupportedPlatforms.IOS:
                    config.SourceHeader = "X-ios-bundle-identifier";
                    break;
                case AppKeySupportedPlatforms.Desktop:
                    config.SourceHeader = "X-sturfee-desktop-bundle-identifier";
                    break;
            }

            config.ApiKey = $"*****Your {platform} Api Key here****";
            config.SourceId = Application.identifier;

            Debug.Log($"ScriptableObject {JsonUtility.ToJson(config)} created ");

            // CreateAsset works only with project relative/local path
            AssetDatabase.CreateAsset(config, Path.Combine(LocalPath, $"{platform}.asset"));

            return config;
        }
    }
}