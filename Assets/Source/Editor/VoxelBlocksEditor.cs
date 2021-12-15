using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(VoxelBlocksManager))]
public class VoxelBlocksEditor : Editor
{
    private SerializedProperty textures;
    private SerializedProperty TextureSheet;
    private SerializedProperty colProp;
    private SerializedProperty sizeProp;
    private SerializedProperty blocksProp;

    private int maxColumns = 16;
    private int textureSize = 128;
    private Texture2D Sheet;
    private VoxelBlocksManager manager;

    private string nameField;
    private int south, north, east, west, up, down;

    void OnEnable() {
        manager = (VoxelBlocksManager)target;
        textures = serializedObject.FindProperty("textures");
        TextureSheet = serializedObject.FindProperty("TextureSheet");
        Sheet = (Texture2D)TextureSheet.objectReferenceValue;
        colProp = serializedObject.FindProperty("maxColumns");
        sizeProp = serializedObject.FindProperty("textureSize");
        blocksProp = serializedObject.FindProperty("blockTypes");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUILayout.PropertyField(colProp);
        EditorGUILayout.PropertyField(sizeProp);
        EditorGUILayout.PropertyField(textures);
        if (GUILayout.Button("Update Preview")) {
            manager.GenerateTexture();
            Debug.Log(manager.TextureSheet);
        }
        Rect blocksRect = EditorGUILayout.GetControlRect();

        EditorGUILayout.PropertyField(blocksProp);

        Texture2D tobj = TextureSheet.objectReferenceValue as Texture2D;
        if (tobj != null) {
            Rect r =GUILayoutUtility.GetAspectRect(tobj.width / tobj.height);
            EditorGUI.DrawPreviewTexture(r, tobj);
        }
        EditorUtility.SetDirty(target);
        serializedObject.ApplyModifiedProperties();
    }
    private void SaveTexture() {
        //string savepath = EditorUtility.SaveFilePanel("Save Texture Pack", "Assets/", "NewTexturePack", "png");
        string path = "";
        path = AssetDatabase.GetAssetPath(target.GetInstanceID());
        path = Path.Combine(Path.GetDirectoryName(path), target.name + "Texture.asset");

        try {
            ProjectWindowUtil.CreateAsset(Sheet, path);
        } catch(System.Exception ex){
            EditorUtility.DisplayDialog("Unable to save texture", ex.Message, "Okay");
        }
    }

    private void ShowBlockList(Rect position, SerializedProperty list) {
        EditorGUI.BeginProperty(position, null, list);
        list.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, 16), list.isExpanded, list.displayName);
        if (list.isExpanded) {
            foreach(SerializedProperty block in list) {
                EditorGUILayout.PropertyField(block, false);
                if(block.isExpanded)
                    ShowBlockPreview(new Rect(position.x, position.y, position.width, position.height), block, new GUIContent(block.displayName));
            }
        }
        EditorGUI.EndProperty();
    }

    private void ShowBlockPreview(Rect position, SerializedProperty sblock, GUIContent label) {
        string[] directions = { "South", "North", "East", "West", "Up", "Down" };
        EditorGUILayout.PropertyField(sblock.FindPropertyRelative("Name"));
        GUILayout.BeginHorizontal();
        foreach(var s in directions) {
            GUILayout.BeginVertical();
            var prop = sblock.FindPropertyRelative(s);
            var value = prop.intValue;
            //var rect = GUILayoutUtility.GetAspectRect(1);
            var tex = (Texture2D)textures.GetArrayElementAtIndex(value).objectReferenceValue;
            GUILayout.Button(tex, GUILayout.ExpandWidth(false), GUILayout.MaxWidth(50), GUILayout.MaxHeight(50));
            //EditorGUI.DrawPreviewTexture(rect, tex);
            EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(50));
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    private Texture2D UpdateSheet() {
        int count = textures.arraySize;
        Debug.Log($"Array size: {count}");
        int rows = 1 + (count-1) / maxColumns;
        int columns = count >= maxColumns ? maxColumns : count;
        int width = columns * textureSize;
        int height = rows * textureSize;

        Texture2D sheet = new Texture2D(width, height);
        int c = 0;
        int r = 0;
        foreach(SerializedProperty serialized in textures) {
            Texture2D tex = (Texture2D)serialized.objectReferenceValue;
            sheet.SetPixels(
                c * tex.width,
                r * tex.height,
                tex.width,
                tex.height,
                tex.GetPixels());
            c++;
            if(c >= maxColumns) {
                c = 0;
                r++;
            }
        }
        sheet.Apply();
        return sheet;
    }
}
