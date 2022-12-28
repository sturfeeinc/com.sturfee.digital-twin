using Sturfee.DigitalTwin.Auth.Editor;
using SturfeeVPS.SDK.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    }
}
