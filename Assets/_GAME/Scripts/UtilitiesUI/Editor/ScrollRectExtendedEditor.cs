using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ScrollRectExtended))]
public class ScrollRectExtendedEditor : ScrollRectEditor
{
    SerializedProperty scrollByDrag;
    SerializedProperty scrollToTopOnEnable;
    SerializedProperty scrollToLeftOnEnable;


    protected override void OnEnable()
    {
        scrollByDrag = serializedObject.FindProperty("scrollByDrag");
        scrollToTopOnEnable = serializedObject.FindProperty("scrollToTopOnEnable");
        scrollToLeftOnEnable = serializedObject.FindProperty("scrollToLeftOnEnable");
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        ScrollRectExtended scrollRect = (ScrollRectExtended)target;

        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(scrollByDrag);
        EditorGUILayout.PropertyField(scrollToTopOnEnable);
        EditorGUILayout.PropertyField(scrollToLeftOnEnable);
        serializedObject.ApplyModifiedProperties();
    }
}