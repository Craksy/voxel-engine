using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(Block))]
public class BlockPropertyDrawer : PropertyDrawer
{
    float w, sep;
    private string rename;
    private string select;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, 30, 16), property.isExpanded, GUIContent.none);
        if (rename != property.propertyPath) {
            if (GUI.Button(new Rect(position.x, position.y, position.width, 16), label, EditorStyles.label)) {
                rename = property.propertyPath;
                select = "";
                EditorGUI.FocusTextInControl(property.propertyPath);
            }
        } 
        if(rename == property.propertyPath) {
            GUI.SetNextControlName(property.propertyPath);
            var value = EditorGUI.DelayedTextField(new Rect(position.x, position.y, position.width, 16), GUIContent.none, label.text);
            Debug.Log(value);
            property.FindPropertyRelative("Name").stringValue = value;
            if(GUI.GetNameOfFocusedControl() == property.propertyPath) {
                select = property.propertyPath;
            }
        }

        if(select == property.propertyPath && (!EditorGUIUtility.editingTextField || GUI.GetNameOfFocusedControl() != property.propertyPath)){
            rename = "";
            select = "";
        }

        position = new Rect(position.x, position.y + 16, position.width, position.height-16);
        sep = (position.width - 6 * 64)/5;

        string[] dirs = { "South", "North", "East", "West", "Up", "Down" };
        if (property.isExpanded) {
            for(int i = 0; i<dirs.Length; i++) {
                SerializedProperty prop = property.FindPropertyRelative(dirs[i]);
                EditorGUI.PropertyField(
                    new Rect(position.x + (64+sep)*i, position.y+16, 64, 16),
                    prop, GUIContent.none);
                Texture2D t2d;
                t2d = prop.intValue == 0 ? Texture2D.blackTexture : VoxelBlocksManager.TextureCollection[prop.intValue];
                EditorGUI.DrawPreviewTexture(
                    new Rect(position.x + (64+sep) * i, position.y+32, 64, 64), t2d);
            }
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return property.isExpanded ? 48 + 64: 16;
    }
}

