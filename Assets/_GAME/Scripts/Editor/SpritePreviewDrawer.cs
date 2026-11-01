using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
public class SpritePreviewDrawer : PropertyDrawer
{
    private static Dictionary<string, int> pickerControlIDs = new Dictionary<string, int>();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SpritePreviewAttribute spritePreview = attribute as SpritePreviewAttribute;
        return EditorGUI.GetPropertyHeight(property, label, true) + spritePreview.PreviewSize + 10;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.LabelField(position, label.text, "SpritePreview used on an invalid type");
            return;
        }

        SpritePreviewAttribute spritePreview = attribute as SpritePreviewAttribute;

        Rect propertyRect = new Rect(position.x, position.y, position.width - spritePreview.PreviewSize - 5, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propertyRect, property, label, true);

        Rect previewRect = new Rect(
            propertyRect.xMax + 5,
            propertyRect.y,
            spritePreview.PreviewSize,
            spritePreview.PreviewSize);

        int controlID;
        if (!pickerControlIDs.TryGetValue(property.propertyPath, out controlID))
        {
            controlID = GUIUtility.GetControlID(FocusType.Passive);
            pickerControlIDs[property.propertyPath] = controlID;
        }

        if (GUI.Button(previewRect, GUIContent.none))
        {
            EditorGUIUtility.ShowObjectPicker<Sprite>(property.objectReferenceValue, false, "", controlID);
        }

        if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == controlID)
        {
            property.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
            property.serializedObject.ApplyModifiedProperties();
        }

        if (property.objectReferenceValue != null && property.objectReferenceValue is Sprite sprite)
        {
            GUI.DrawTexture(previewRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, true, 0,
                new Color(0.3f, 0.3f, 0.3f, 1f), 0, 0);

            GUI.DrawTextureWithTexCoords(
                previewRect,
                sprite.texture,
                new Rect(
                    sprite.textureRect.x / sprite.texture.width,
                    sprite.textureRect.y / sprite.texture.height,
                    sprite.textureRect.width / sprite.texture.width,
                    sprite.textureRect.height / sprite.texture.height
                ),
                true
            );
        }
    }
}
