using UnityEngine;
using System.IO;
using UnityEditor;

public class FluidBook : MonoBehaviour {

    
    
    Material fluidSimMat;
    Material fluidSimDensityDisplayMat;
    Material fluidSimVelocityDisplayMat;
    Material fluidSimGradientColoursDisplayMat;
    Texture2D noiseTexture;
    Texture2D velocityTexture;
    int simulationFrameRate;
    float densityFadingMultiplier;
    float velocityFadingMultiplier;
    float velocityMultiplier;
    Vector2 noiseScrollSpeed;
    

    [Header("Camera Setup")]
    [SerializeField] Camera captureCamera = default;
    [SerializeField] Vector3 captureBoxSize = new Vector3(10f, 10f, 10f);
    [SerializeField] Color gizmoColour = Color.grey;
    [SerializeField, GradientUsageAttribute(true)] Gradient colourGradient = default;
    

    Vector2Int textureResolution;
    string destinationFolder;
    string textureName;
    int numFramesToCapture;
    float secondsBetweenCaptures;
    
    [HideInInspector] public bool foldoutFluidBookData = true;
    
    [HideInInspector] public FluidBookData fluidBookData = default;
    

    //[SerializeField] ComputeShader fluidSimCompute = default;
    
    

    RenderTexture captureRT;
    bool isCapturing = false;
    float timeSinceLastCapture = 0f;
    int imagesCaptured = 0;
    bool captureVelocity;

    RenderTexture densityRT1;
    RenderTexture densityRT2;
    RenderTexture velocityRT1;
    RenderTexture velocityRT2;
    RenderTexture divergenceRT;
    RenderTexture pressureTempRT;
    RenderTexture pressureRT;

    RenderTexture velocityDisplayRT1;

    float timeSinceLastFluidSim;

    Vector4[] gradientColours = new Vector4[128];


    void OnValidate() {
        if(Application.isPlaying && fluidBookData != null) InitialiseSim();
    }

    public void InitialiseSim() {

        textureResolution = fluidBookData.textureResolution;
        SetFluidBookVariables();

        if(fluidSimMat != null) Destroy(fluidSimMat);
        fluidSimMat = new Material(Shader.Find("FluidBook/FluidSimShader"));
        fluidSimMat.hideFlags = HideFlags.HideAndDontSave;

        if(fluidSimDensityDisplayMat != null) Destroy(fluidSimDensityDisplayMat);
        fluidSimDensityDisplayMat = new Material(Shader.Find("FluidBook/FluidSimDensityDisplay"));
        fluidSimDensityDisplayMat.hideFlags = HideFlags.HideAndDontSave;

        if(fluidSimVelocityDisplayMat != null) Destroy(fluidSimVelocityDisplayMat);
        fluidSimVelocityDisplayMat = new Material(Shader.Find("FluidBook/FluidSimVelocityDisplay"));
        fluidSimVelocityDisplayMat.hideFlags = HideFlags.HideAndDontSave;

        if(fluidSimGradientColoursDisplayMat != null) Destroy(fluidSimGradientColoursDisplayMat);
        fluidSimGradientColoursDisplayMat = new Material(Shader.Find("FluidBook/FluidSimGradientColoursDisplay"));
        fluidSimGradientColoursDisplayMat.hideFlags = HideFlags.HideAndDontSave;

        transform.Find("DensityVisualisation").GetComponent<Renderer>().material = fluidSimDensityDisplayMat;
        transform.Find("VelocityVisualisation").GetComponent<Renderer>().material = fluidSimVelocityDisplayMat;
        transform.Find("GradientVisualisation").GetComponent<Renderer>().material = fluidSimGradientColoursDisplayMat;

        ClearRenderTextures();

        densityRT1 = CreateRenderTexture(3);
        densityRT2 = CreateRenderTexture();
        velocityRT1 = CreateRenderTexture(1);
        velocityRT2 = CreateRenderTexture(1);
        divergenceRT = CreateRenderTexture(2);
        pressureTempRT = CreateRenderTexture(2);
        pressureRT = CreateRenderTexture(2);

        velocityDisplayRT1 = CreateRenderTexture(1);

        if(captureRT != null) captureRT.Release();
        captureRT = new RenderTexture(textureResolution.x, textureResolution.y, 0, RenderTextureFormat.ARGB32);
        captureRT.enableRandomWrite = true;
        captureRT.name = "CaptureTexture";
        captureRT.Create();
        
        captureCamera.targetTexture = captureRT;

        SetColourGradient();
        


/*
        // aspect ratio
        if(textureResolution.x != textureResolution.y) {
            if(textureResolution.x > textureResolution.y) {
                float newHeightRatio = (float)textureResolution.y / (float)textureResolution.x;
                captureCamera.rect = new Rect(0f, 0f, 1f, newHeightRatio);
            } else {
                float newWidthRatio = (float)textureResolution.x / (float)textureResolution.y;
                captureCamera.rect = new Rect(0f, 0f, newWidthRatio, 1f);
            }
        } else {
            captureCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

*/

    }

    public void SetColourGradient() {
        for (int i = 0; i < gradientColours.Length; i++) {
            gradientColours[i] = (Vector4)colourGradient.Evaluate(i / (float)(gradientColours.Length - 1));
        }
    }

    public void SetFluidBookVariables() {// doing it separately so we can refresh on change without recreating textutes
        numFramesToCapture = fluidBookData.numFramesToCapture;
        secondsBetweenCaptures = fluidBookData.secondsBetweenCaptures;
        destinationFolder = fluidBookData.destinationFolder;
        textureName = fluidBookData.textureName;
        simulationFrameRate = fluidBookData.simulationFrameRate;
        noiseTexture = fluidBookData.noiseTexture;
        velocityTexture = fluidBookData.velocityTexture;
        velocityMultiplier = fluidBookData.velocityMultiplier;
        noiseScrollSpeed = fluidBookData.noiseScrollSpeed;
        densityFadingMultiplier = fluidBookData.densityFadingMultiplier;
        velocityFadingMultiplier = fluidBookData.velocityFadingMultiplier;
        captureVelocity = fluidBookData.captureVelocity;

        
    }
    

    void Awake() {

        if(fluidBookData == null) Debug.LogError("Missing FluidBookData on " + this.name);
        else InitialiseSim();

    }

    RenderTexture CreateRenderTexture(int index = 0) {
        RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
        if(index == 1) format = RenderTextureFormat.RGHalf;
        else if(index == 2) format = RenderTextureFormat.RHalf;

        RenderTexture newRT = new RenderTexture(textureResolution.x, textureResolution.y, 0, format);
        newRT.enableRandomWrite = true;
        //if(index == 3) newRT.filterMode = FilterMode.Point;
        newRT.Create();

        return newRT;
    }

    

    void FixedUpdate() {

        timeSinceLastFluidSim += Time.fixedDeltaTime;

        float fluidRate = 1f / (float)simulationFrameRate;

        while(timeSinceLastFluidSim > fluidRate) {
            PerformFluidSim();
            timeSinceLastFluidSim -= fluidRate;
        }
        

        fluidSimDensityDisplayMat.SetTexture("_FluidSimDensity", densityRT1);
        fluidSimVelocityDisplayMat.SetTexture("_FluidSimVelocity", velocityDisplayRT1);
        fluidSimGradientColoursDisplayMat.SetTexture("_FluidSimDensity", densityRT1);
        fluidSimGradientColoursDisplayMat.SetVectorArray("_Colours", gradientColours);
        Shader.SetGlobalTexture("_FluidSimDensityTest", densityRT1);
        //Shader.SetGlobalTexture("_FluidSimVelocity", velocityDisplayRT1);
        

        if(isCapturing) {

            timeSinceLastCapture += Time.fixedDeltaTime;

            if(timeSinceLastCapture >= secondsBetweenCaptures) {

                timeSinceLastCapture -= secondsBetweenCaptures;

                CaptureImage(densityRT1, imagesCaptured);
                if(captureVelocity) CaptureImage(velocityDisplayRT1, imagesCaptured, true);

                imagesCaptured++;

                if(imagesCaptured >= numFramesToCapture) {
                    isCapturing = false;
                    timeSinceLastCapture = 0f;
                    imagesCaptured = 0;
                }
            }
        }
    }


    void PerformFluidSim() {

        fluidSimMat.SetTexture("_NoiseTex", noiseTexture);
        fluidSimMat.SetTexture("_VelocityInputTex", velocityTexture);
        fluidSimMat.SetVector("_VelocityNoiseScrollSpeed", new Vector4(noiseScrollSpeed.x, noiseScrollSpeed.y, 0f, 0f));  

        fluidSimMat.SetFloat("_DensityFadingRatio", densityFadingMultiplier);
        fluidSimMat.SetFloat("_VelocityFadingRatio", velocityFadingMultiplier);

        fluidSimMat.SetTexture("_SecondTex", captureRT);
        Graphics.Blit(densityRT1, densityRT2, fluidSimMat, 0);

        fluidSimMat.SetTexture("_SecondTex", captureRT);
        fluidSimMat.SetFloat("_VelocityStrength", velocityMultiplier);
        Graphics.Blit(velocityRT1, velocityRT2, fluidSimMat, 1);

        Graphics.Blit(velocityRT2, velocityRT1, fluidSimMat, 2);

        Graphics.Blit(velocityRT1, velocityDisplayRT1, fluidSimMat, 8);

        fluidSimMat.SetTexture("_SecondTex", velocityRT1);
        Graphics.Blit(densityRT2, densityRT1, fluidSimMat, 3);

        Graphics.Blit(velocityRT1, divergenceRT, fluidSimMat, 4);

        for (int i = 0; i < 6; i++) {
            fluidSimMat.SetTexture("_SecondTex", divergenceRT);
            Graphics.Blit(pressureRT, pressureTempRT, fluidSimMat, 5);
            fluidSimMat.SetTexture("_SecondTex", divergenceRT);
            Graphics.Blit(pressureTempRT, pressureRT, fluidSimMat, 5);
        }

        Graphics.CopyTexture(velocityRT1, velocityRT2);

        fluidSimMat.SetTexture("_SecondTex", velocityRT2);
        Graphics.Blit(pressureRT, velocityRT1, fluidSimMat, 6);

    }


    public void CaptureSequence() {

        isCapturing = true;
        timeSinceLastCapture = secondsBetweenCaptures;// so that we capture one image straight away
        imagesCaptured = 0;

    }

    void CaptureImage(RenderTexture image, int imageId = 0,  bool isVelocity = false) {

        Texture2D t = new Texture2D(image.width, image.height, isVelocity ? TextureFormat.RG16 : TextureFormat.ARGB32, 0, true);

        RenderTexture.active = image;
        t.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0, false);
        //t.alphaIsTransparency = true;
        t.Apply();

        string name = isVelocity ? fluidBookData.textureName + "_" + "Velocity" + "_" + imageId : fluidBookData.textureName + "_" + imageId;

        SaveTexture(t, name, isVelocity);
    }


    void SaveTexture(Texture2D t, string name, bool isVelocity) {

        byte[] bytes = t.EncodeToPNG();

        File.WriteAllBytes(Application.dataPath + destinationFolder + "/" + name + ".png", bytes);

        AssetDatabase.Refresh();

        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath("Assets/" + destinationFolder + "/" + name + ".png");
        TextureImporterSettings settings = new TextureImporterSettings();

        
        importer.ReadTextureSettings(settings);

        if(isVelocity) importer.textureCompression = TextureImporterCompression.Uncompressed;
        //importer.textureCompression = TextureImporterCompression.Compressed;
        //importer.alphaIsTransparency = true;
        
        settings.sRGBTexture = false;
        settings.filterMode = FilterMode.Point;
        
        settings.readable = true;
        settings.alphaIsTransparency = true;

        importer.SetTextureSettings(settings);
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        AssetDatabase.Refresh();

    }

    void ClearRenderTextures() {

        if(densityRT1 != null) {
            densityRT1.Release();
            Destroy(densityRT1);
        }

        if(densityRT2 != null) {
            densityRT2.Release();
            Destroy(densityRT2);
        }

        if(velocityRT1 != null) {
            velocityRT1.Release();
            Destroy(velocityRT1);
        }

        if(velocityRT2 != null) {
            velocityRT2.Release();
            Destroy(velocityRT2);
        }

        if(divergenceRT != null) {
            divergenceRT.Release();
            Destroy(divergenceRT);
        }

        if(pressureTempRT != null) {
            pressureTempRT.Release();
            Destroy(pressureTempRT);
        }

        if(pressureRT != null) {
            pressureRT.Release();
            Destroy(pressureRT);
        }

        if(velocityDisplayRT1 != null) {
            velocityDisplayRT1.Release();
            Destroy(velocityDisplayRT1);
        }

    }


    void OnApplicationQuit() {
        if(captureRT != null) captureRT.Release();
        ClearRenderTextures();
    }

    void OnDrawGizmos() {


        if(fluidBookData == null) return;
        
        
        Gizmos.color = gizmoColour;
        

        Gizmos.DrawWireCube(transform.position, captureBoxSize);


        Gizmos.color = new Color(gizmoColour.r, gizmoColour.g, gizmoColour.b, .1f);

        Gizmos.DrawCube(transform.position, captureBoxSize);


    }

}
