using UnityEngine;
using System.IO;
using UnityEditor;

public class AtlasBuilder : MonoBehaviour {

    public Texture2D[] texturesForAtlas = default;
    [SerializeField] ComputeShader atlasCompute = default;
    [HideInInspector] public Vector2Int columnsAndRows = new Vector2Int(4, 4);
    [HideInInspector] public Vector2Int atlasResolution = new Vector2Int(1024, 1024);
    [HideInInspector] public bool isVelocityAtlas = false;
    [HideInInspector] public string destinationPath = "/FluidBook/Exports";
    [HideInInspector] public string atlasName = "AtlasName";

    public void BuildAtlas() {

        RenderTexture atlasRT = new RenderTexture(atlasResolution.x, atlasResolution.y, 0, isVelocityAtlas ? RenderTextureFormat.RG16 : RenderTextureFormat.ARGB32);
        atlasRT.enableRandomWrite = true;
        atlasRT.Create();

        RenderTexture sourceRT = new RenderTexture(texturesForAtlas[0].width, texturesForAtlas[0].height, 0, isVelocityAtlas ? RenderTextureFormat.RG16 : RenderTextureFormat.ARGB32);
        sourceRT.enableRandomWrite = true;
        sourceRT.Create();
        
        
        atlasCompute.SetTexture(0, "Atlas", atlasRT);

        int frameWidth = texturesForAtlas[0].width;
        int frameHeight = texturesForAtlas[0].height;
        for (int i = 0; i < texturesForAtlas.Length; i++) {


            Graphics.Blit(texturesForAtlas[i], sourceRT);
            atlasCompute.SetTexture(0, "Source", sourceRT);

            int coordX = Mathf.FloorToInt((float)i % (float)columnsAndRows.x);
            int coordY = Mathf.FloorToInt(i / columnsAndRows.x);
            atlasCompute.SetInt("CoordX", coordX * frameWidth);
            atlasCompute.SetInt("CoordY", coordY * frameHeight);
            
            atlasCompute.Dispatch(0, texturesForAtlas[0].width / 32, texturesForAtlas[0].height / 32, 1);
        }


        Texture2D atlasTex = new Texture2D(atlasResolution.x, atlasResolution.y, isVelocityAtlas ? TextureFormat.RG16 : TextureFormat.ARGB32, 0, true);
        atlasTex.name = atlasName;

        RenderTexture activeRT = RenderTexture.active;
        activeRT.Create();
        RenderTexture.active = atlasRT;
        atlasTex.ReadPixels(new Rect(0, 0, atlasRT.width, atlasRT.height), 0, 0, false);

        atlasTex.Apply();

        SaveTexture(atlasTex, isVelocityAtlas);

        RenderTexture.active = activeRT;
        sourceRT.Release();
        atlasRT.Release();

    }

    void SaveTexture(Texture2D t, bool isVelocity) {

        byte[] bytes = t.EncodeToPNG();

        File.WriteAllBytes(Application.dataPath + destinationPath + "/" + t.name + ".png", bytes);

        AssetDatabase.Refresh();

        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath("Assets/" + destinationPath + "/" + t.name + ".png");
        TextureImporterSettings settings = new TextureImporterSettings();

        importer.ReadTextureSettings(settings);

        if(isVelocity) {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        } else {
            importer.textureCompression = TextureImporterCompression.Compressed;
            settings.alphaIsTransparency = true;
        }
        
        //importer.alphaIsTransparency = true;
        
        settings.filterMode = FilterMode.Point;
        settings.sRGBTexture = false;
        //settings.readable = true;
        //settings.readable = true;
        

        importer.SetTextureSettings(settings);
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        AssetDatabase.Refresh();

    }

    public bool CheckAllTexturesAreSameDimensions() {

        if(texturesForAtlas == null) return false;
        if(texturesForAtlas.Length < 1) return false;

        Vector2Int dimXY = new Vector2Int (texturesForAtlas[0].width, texturesForAtlas[0].height);

        for (int i = 1; i < texturesForAtlas.Length; i++) {
            if(texturesForAtlas[i].width != dimXY.x || texturesForAtlas[i].height != dimXY.y) {
                return false;
            }
        }

        return true;
    }

    public Vector2Int GetTextureSize() {

        if(texturesForAtlas == null) return Vector2Int.zero;
        if(texturesForAtlas.Length < 1) return Vector2Int.zero;

        Vector2Int dimXY = new Vector2Int (texturesForAtlas[0].width, texturesForAtlas[0].height);
        return dimXY;
    }

    public void ClearAtlasTextures() {

        texturesForAtlas = new Texture2D[0];

    }


}
