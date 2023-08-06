using Sturfee.DigitalTwin.Auth.Editor;
using SturfeeVPS.SDK.Editor;
using Sturfee.Auth;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;


namespace Sturfee.Auth.Editor
{
    public class DtSturfeeConfigureWindow : SturfeeConfigurationWindow
    {

        [MenuItem("Sturfee/Configure", false, 0)]
        public static new void ShowWindow()
        {
            DtSturfeeConfigureWindow window = GetWindow<DtSturfeeConfigureWindow>();
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>(_editorPath + "/Images/sturfee_official_icon-black.png");
            GUIContent customTitleContent = new GUIContent("Sturfee", icon);
            window.titleContent = customTitleContent;
            window.Show();
        }

        protected override void OnAuthTab()
        {
            base.OnAuthTab();
            GUILayout.Label("App key settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            {
                if (GUILayout.Button("Android", GUILayout.Width(200), GUILayout.Height(25)))
                {
                    AppKeyEditor.ShowAppKeyConfig_Android();
                }

                if (GUILayout.Button("IOS", GUILayout.Width(200), GUILayout.Height(25)))
                {
                    AppKeyEditor.ShowAppKeyConfig_IOS();
                }

                if (GUILayout.Button("Desktop", GUILayout.Width(200), GUILayout.Height(25)))
                {
                    AppKeyEditor.ShowAppKeyConfig_Desktop();
                }

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            GUILayout.Label("Authentication Provider", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            index = EditorGUILayout.Popup(index, options);
            if (GUILayout.Button("Save Authentication Settings"))
                InstantiatePrimitive();
        }


        public static string AbsolutePath => Path.Combine(Application.dataPath, "Resources", "Sturfee", "Auth");
        public static string LocalPath => Path.Combine("Assets", "Resources", "Sturfee", "Auth");
        public string[] options = new string[] { "Sturfee", "AWS Cognito" };
        public int index = 0;
        void InstantiatePrimitive()
        {
            switch (index)
            {
                case 0:
                    // delete any cognito settings
                    ShowAuthProviderConfig("Sturfee");
                    break;
                case 1:
                    // create cognito settings
                    ShowAuthProviderConfig("AWS Cognito");
                    break;
                default:
                    Debug.LogError("Unrecognized Option");
                    break;
            }
        }

        private static void ShowAuthProviderConfig(string provider)
        {
            EditorUtility.FocusProjectWindow();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path.Combine(LocalPath, $"AuthProvider.asset"));

            if (obj == null)
            {
                var config = CreateAuthProviderObject(provider);
                SaveAuthProvider(config);

                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path.Combine(LocalPath, $"AuthProvider.asset"));
            }

            Selection.activeObject = obj;
        }
        private static AuthenticationProviderConfig CreateAuthProviderObject(string provider)
        {
            AuthenticationProviderConfig config = ScriptableObject.CreateInstance<AuthenticationProviderConfig>();

            if (provider == "Sturfee")
            {
                config.Provider = AuthenticationProvider.Sturfee;
            }
            if (provider == "AWS Cognito")
            {
                config.Provider = AuthenticationProvider.AwsCognito;
            }

            Debug.Log($"AuthenticationProviderConfig {JsonUtility.ToJson(config)} created ");

            return config;
        }

        private static void SaveAuthProvider(AuthenticationProviderConfig config)
        {

            string dir = AbsolutePath;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // CreateAsset works only with project relative/local path
            AssetDatabase.CreateAsset(config, Path.Combine(LocalPath, $"AuthProvider.asset"));

            Debug.Log($"AuthenticationProviderConfig {JsonUtility.ToJson(config)} saved ");

        }
    }

}
