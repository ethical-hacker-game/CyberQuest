using UnityEditor;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using UnityEngine;

[CustomEditor(typeof(CoreElementsSpawner))]
public class CoreElementsSpawnerEditor : OdinEditor
{
    private CoreElementsSpawner elementsSpawner;

    public override void OnInspectorGUI()
    {
        elementsSpawner = (CoreElementsSpawner)target;

        base.OnInspectorGUI();
        DrawElements(elementsSpawner.CoreElements, "Core");
        EditorGUILayout.Space();
        DrawElements(elementsSpawner.DevElements, "Dev");
    }

    private void DrawElements(CoreElementsPreset elementsPreset, string groupName)
    {
        if (elementsPreset == null)
        {
            EditorGUILayout.HelpBox($"Add {groupName} Elements!", MessageType.Error);
            return;
        }

        List<GameObject> sublist = elementsPreset.coreElements;
        string header = $"{groupName} Elements: {sublist.Count}";

        EditorGUILayout.HelpBox(header, MessageType.None);
        EditorGUI.BeginDisabledGroup(true); 
        EditorGUILayout.BeginVertical(GUI.skin.box);

        for (int i = 0; i < sublist.Count; i++)
        {
            EditorGUILayout.LabelField(sublist[i].name);
        }

        EditorGUILayout.EndVertical();
        EditorGUI.EndDisabledGroup();
    }
}
