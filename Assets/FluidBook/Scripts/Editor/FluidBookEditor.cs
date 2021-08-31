using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FluidBook))]
public class FluidBookEditor : Editor {

    FluidBook fluidBook;
    const string captureButtonName = "Capture";
    const string cantCaptureButtonName = "Capture - Playmode Only";

    Editor fluidBookDataEditor;



    public override void OnInspectorGUI() {

        base.OnInspectorGUI();

        //EditorGUI.BeginChangeCheck();
        //SerializedProperty gradient = serializedObject.FindProperty("colourGradient");
        //EditorGUILayout.PropertyField(gradient, true, null);
        //bool gradientWasChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space(20);

        SerializedProperty data = serializedObject.FindProperty("fluidBookData");

        data.objectReferenceValue = EditorGUILayout.ObjectField("FluidBookData", data.objectReferenceValue, typeof(FluidBookData), true);
        if(data.objectReferenceValue != null) {


            SerializedProperty foldout = serializedObject.FindProperty("foldoutFluidBookData");

            foldout.boolValue = EditorGUILayout.InspectorTitlebar(foldout.boolValue, data.objectReferenceValue);

            using(var check = new EditorGUI.ChangeCheckScope()) {

                if(foldout.boolValue) {
                    CreateCachedEditor(data.objectReferenceValue, null, ref fluidBookDataEditor);
                    fluidBookDataEditor.OnInspectorGUI();
                }

                if(check.changed) {
                    fluidBook.SetFluidBookVariables();
                }

            }

        }



        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(30);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        
        if(GUILayout.Button(Application.isPlaying ? captureButtonName : cantCaptureButtonName)) {
            fluidBook.CaptureSequence();
        }

        EditorGUI.EndDisabledGroup();
        
        //if(gradientWasChanged) {
        //    fluidBook.SetColourGradient();
        //}


    }

    void OnEnable() {
        fluidBook = (FluidBook)target;
    }



}
