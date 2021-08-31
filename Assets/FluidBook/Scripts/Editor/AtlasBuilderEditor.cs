using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AtlasBuilder))]
public class AtlasBuilderEditor : Editor {
    
    AtlasBuilder builder;


    public override void OnInspectorGUI() {

        base.OnInspectorGUI();

        builder = (AtlasBuilder)target;

        

        

        EditorGUI.BeginChangeCheck();

        SerializedProperty columnsAndRows = serializedObject.FindProperty("columnsAndRows");

        columnsAndRows.vector2IntValue = EditorGUILayout.Vector2IntField("Rows & Columns", columnsAndRows.vector2IntValue);

        if(EditorGUI.EndChangeCheck()) {
            if(!builder.CheckAllTexturesAreSameDimensions()) {
                Debug.LogError("All atlas textures should have the same dimensions.");
            }

            
        }

        SerializedProperty atlasResolution = serializedObject.FindProperty("atlasResolution");
        Vector2Int dim = builder.GetTextureSize();
        atlasResolution.vector2IntValue = new Vector2Int(dim.x * columnsAndRows.vector2IntValue.x, dim.y * columnsAndRows.vector2IntValue.y);


        EditorGUI.BeginDisabledGroup(true);
        atlasResolution.vector2IntValue = EditorGUILayout.Vector2IntField("Atlas Texture Resolution", atlasResolution.vector2IntValue);
        EditorGUI.EndDisabledGroup();


        

        SerializedProperty atlasDestination = serializedObject.FindProperty("destinationPath");
        atlasDestination.stringValue = EditorGUILayout.TextField("Destination Folder", atlasDestination.stringValue);

        SerializedProperty atlasName = serializedObject.FindProperty("atlasName");
        atlasName.stringValue = EditorGUILayout.TextField("Atlas Name", atlasName.stringValue);

        SerializedProperty isVelocityAtlas = serializedObject.FindProperty("isVelocityAtlas");
        isVelocityAtlas.boolValue = EditorGUILayout.Toggle("Is Velocity Atlas", isVelocityAtlas.boolValue);


        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Build Atlas")) {

            builder.BuildAtlas();

        }

        if(GUILayout.Button("Clear Textures")) {

            builder.ClearAtlasTextures();

        }



    }


}
