using Sturfee.DigitalTwin.Auth.Editor;
using Sturfee.Auth.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Sturfee.Auth.Editor
{
    [CustomEditor(typeof(AuthenticationProviderConfig))]
    public class AuthProviderEditor : UnityEditor.Editor
    {
        public string[] options = new string[] { "Sturfee", "AWS Cognito" };
        public int index = 0;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            index = serializedObject.FindProperty("Provider").enumValueIndex;

            GUILayout.Label("Choose Authentication Provider", EditorStyles.boldLabel);
            index = EditorGUILayout.Popup(index, options);

            EditorGUILayout.Space();
            if (index > 0)
            {
                GUILayout.Label("Authentication Provider Settings", EditorStyles.boldLabel);
                DrawDefaultInspector();
                serializedObject.FindProperty("Provider").enumValueIndex = index;
            }
            else
            {
                GUILayout.Label("Set up your token in the Sturfee configuration window: Sturfee > Configure");
                //DrawDefaultInspector();
                serializedObject.FindProperty("Provider").enumValueIndex = index;

                if (GUILayout.Button("Open Sturfee Configuration"))
                {
                    DtSturfeeConfigureWindow window = (DtSturfeeConfigureWindow)EditorWindow.GetWindow(typeof(DtSturfeeConfigureWindow));//, false, "Gib Halp Plis");
                    window.Show();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}