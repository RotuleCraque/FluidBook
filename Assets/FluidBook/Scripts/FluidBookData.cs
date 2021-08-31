using UnityEngine;

[CreateAssetMenu(fileName = "FluidBookData", menuName = "FluidBook/FluidBookData", order = 1)]
public class FluidBookData : ScriptableObject {

    [Header("Texture Output")]
    [Min(32)] public Vector2Int textureResolution = new Vector2Int(512, 512);
    [Min(1)] public int numFramesToCapture = 4; 
    [Min(.02f)] public float secondsBetweenCaptures = .2f;
    public string destinationFolder = "/FluidBook/Exports";
    public string textureName = "NewTexture";
    public bool captureVelocity = true;



    [Header("Simulation")]
    public int simulationFrameRate = 60; 
    public Texture2D noiseTexture = default;
    public Texture2D velocityTexture = default;
    public float velocityMultiplier = 1f;
    public Vector2 noiseScrollSpeed = Vector2.up;
    public float densityFadingMultiplier = .96f;
    public float velocityFadingMultiplier = .99f;


    //[Header("Previsualisation")]
    //public Gradient colourGradient = default;
    


}
