using UnityEngine;

public class GradientToShader : MonoBehaviour {

    [SerializeField, GradientUsageAttribute(true)] Gradient gradient = default;
    [SerializeField] Material displayMaterial = default;
    Vector4[] colours;

    void OnValidate() {

        if(displayMaterial == null) return;

        colours = new Vector4[128];
        for (int i = 0; i < colours.Length; i++) {
            float progress = i / 128f;
            Vector4 gradientCol = (Vector4)(gradient.Evaluate(progress));
            colours[i] = gradientCol;

            
        }



        displayMaterial.SetVectorArray("_Colours", colours);
        

    }




}
