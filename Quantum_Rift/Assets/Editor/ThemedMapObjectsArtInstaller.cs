using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ThemedMapObjectsArtInstaller
{
    const string Folder="Assets/image/Map/Themed-Crates-Traps-v4";
    public static void VerifyFinal()
    {
        FixedMapObjectsInstaller.Verify();
        FixedObjectsPlayCheck.Run();
    }
    [MenuItem("Tools/Quantum Rift/Maps/Apply Themed Crates And Spike Art")]
    public static void Install()
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
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.125f);
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
                    display.sortingLayerName="object";display.sortingOrder=4;display.color=Color.white;
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
        FixedMapObjectsInstaller.Verify();
    }
    static Sprite Load(string theme,string name)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+theme+"/"+name+".png");
        if(sprite==null)throw new Exception("Missing sprite "+theme+"/"+name);return sprite;
    }
}
