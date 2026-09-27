using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ThemedMapObjectsArtInstaller
{
    const string Folder="Assets/image/Map/Wall-Blocks-Flush-Traps-v5";
    static readonly Dictionary<Sprite,Vector2> visibleSizes=new Dictionary<Sprite,Vector2>();
    [MenuItem("Tools/Quantum Rift/Maps/Fit Breakable Blocks To Wall Tile")]
    public static void ResizeWallBlocks()
    {
        int count=0;
        foreach(string name in new[]{"map_1","map_1_2","map_1_3","Map_2","Map_2_2"})
        {
            string path="Assets/Prefab/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var box in root.GetComponentsInChildren<BreakableProp>(true))
                { FitBlockToWall(root,box); count++; }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("WALL_BLOCKS_RESIZED count="+count+" fit=one-wall-tile colliders=scaled-with-block");
        FixedMapObjectsInstaller.Verify();
    }
    static void FitBlockToWall(GameObject root,BreakableProp box)
    {
        var grid=root.GetComponentInChildren<Grid>(true);
        if(grid==null)throw new Exception("Map has no wall grid");
        var sprite=box.GetComponent<SpriteRenderer>().sprite;
        if(!visibleSizes.TryGetValue(sprite,out var size))
        {
            // วัดเฉพาะพิกเซลที่มองเห็น ไม่รวมขอบโปร่งใสของไฟล์ภาพ
            var texture=new Texture2D(2,2);
            try
            {
                if(!texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))))throw new Exception("Cannot measure wall block");
                var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                    if(pixels[y*texture.width+x].a>0){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                if(maxX<0)throw new Exception("Empty wall block image");
                size=new Vector2(maxX-minX+1,maxY-minY+1)/sprite.pixelsPerUnit;
                visibleSizes[sprite]=size;
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        var tile=Vector2.Scale(grid.cellSize,grid.transform.lossyScale);
        float fit=Mathf.Min(Mathf.Abs(tile.x)/size.x,Mathf.Abs(tile.y)/size.y);
        var parent=box.transform.parent.lossyScale;
        // ย่อทั้งภาพและ collider ไปพร้อมกัน โดยรักษาสัดส่วนพิกเซลเดิม
        box.transform.localScale=new Vector3(fit/Mathf.Abs(parent.x),fit/Mathf.Abs(parent.y),1f);
    }
    public static void VerifyFinal()
    {
        FixedMapObjectsInstaller.Verify();
        FixedObjectsPlayCheck.Run();
    }
    [MenuItem("Tools/Quantum Rift/Maps/Apply Themed Crates And Spike Art")]
    public static void Install()
    {
        InstallInternal(true);
    }
    public static void ApplyArtworkOnly()
    {
        InstallInternal(false);
    }
    static void InstallInternal(bool verify)
    {
        AssetDatabase.Refresh();
        foreach(string theme in new[]{"Spaceship","Forest"})
        foreach(string name in new[]{"Crate"}.Concat(Enumerable.Range(1,7).Select(i=>"Spike-"+i.ToString("00"))))
        {
            string path=Folder+"/"+theme+"/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Missing image "+path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=48;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.1f);
            importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.maxTextureSize=256;
            importer.SaveAndReimport();
        }
        int crates=0,traps=0;
        foreach(string name in new[]{"map_1","map_1_2","map_1_3","Map_2","Map_2_2"})
        {
            string path="Assets/Prefab/"+name+".prefab";
            string theme=name.StartsWith("Map_2")?"Forest":"Spaceship";
            var crate=Load(theme,"Crate");
            var frames=Enumerable.Range(1,7).Select(i=>Load(theme,"Spike-"+i.ToString("00"))).ToArray();
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var box in root.GetComponentsInChildren<BreakableProp>(true))
                {
                    var sprite=box.GetComponent<SpriteRenderer>();sprite.sprite=crate;sprite.color=Color.white;
                    FitBlockToWall(root,box);
                    var mark=box.transform.Find("SupplyMark");if(mark!=null)UnityEngine.Object.DestroyImmediate(mark.gameObject);
                    crates++;
                }
                foreach(var spike in root.GetComponentsInChildren<FixedSpikeTrap>(true))
                {
                    spike.spikes=null;spike.warningDisplay=null;
                    foreach(string oldName in new[]{"RecessedPlate","WarningLight","Spikes"})
                    {var old=spike.transform.Find(oldName);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
                    var art=spike.transform.Find("AnimatedArtwork");
                    if(art==null){art=new GameObject("AnimatedArtwork",typeof(SpriteRenderer)).transform;art.SetParent(spike.transform,false);}
                    art.localPosition=new Vector3(0,-.5f,0);art.localScale=Vector3.one;
                    var display=art.GetComponent<SpriteRenderer>();display.sprite=frames[0];
                    display.sortingLayerName="object";display.sortingOrder=-2;display.color=Color.white;
                    spike.animatedDisplay=display;spike.animationFrames=frames;
                    // Footprint and authored positions stay aligned with the 10-second gameplay cycle.
                    traps++;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        if(crates!=90 || traps!=90)throw new Exception("Incomplete map artwork "+crates+"/"+traps);
        Debug.Log("THEMED_ART_INSTALLED crates="+crates+" traps="+traps+" framesPerTrap=7");
        if (verify) FixedMapObjectsInstaller.Verify();
    }
    static Sprite Load(string theme,string name)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+theme+"/"+name+".png");
        if(sprite==null)throw new Exception("Missing sprite "+theme+"/"+name);return sprite;
    }
}
