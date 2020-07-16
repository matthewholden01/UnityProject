using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SetRelativity))]
[CanEditMultipleObjects]
public class RelativityEditor : Editor
{
    SetRelativity setRelativity;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        using (var check = new EditorGUI.ChangeCheckScope()) {
            
            if (check.changed)
            {
                setRelativity.UpdateSettings();
            }
            if(GUILayout.Button("Update Settings"))
            {
                setRelativity.UpdateSettings();
            }
        }
        DrawSettingsEditor(setRelativity.relativitySettings, setRelativity.OnRelativitySettingsUpdated);
        DrawSettingsEditor(setRelativity.planetSettings, setRelativity.OnPlanetSettingsUpdated);
    }

    public void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated)
    {
        Editor editor = CreateEditor(settings);
        editor.OnInspectorGUI();
    }

    public void OnEnable()
    {
        setRelativity = (SetRelativity)target;
    }



}
