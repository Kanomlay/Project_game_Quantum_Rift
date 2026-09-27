using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MapObjectPrefabExporter
{
    const string Folder="Assets/Prefab/MapObjects";
    [MenuItem("Tools/Quantum Rift/Maps/Create Drag And Drop Object Prefabs")]
    public static void Export()
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefab","MapObjects");
        int count=0;
        foreach(string theme in new[]{"Spaceship","Forest"})
        {
            if(!AssetDatabase.IsValidFolder(Folder+"/"+theme))AssetDatabase.CreateFolder(Folder,theme);
            string sourcePath="Assets/Prefab/"+(theme=="Spaceship"?"map_1":"Map_2")+".prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var box=source.GetComponentInChildren<BreakableProp>(true);
            var trap=source.GetComponentInChildren<FixedSpikeTrap>(true);
            SaveCopy(box.gameObject,theme,"BreakableWall");
            SaveCopy(trap.gameObject,theme,"FloorSpikes");
            count+=2;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("DRAG_DROP_PREFABS_READY count="+count+" standaloneReferences=true colliders=true spikeFrames=7 cycle=10");
    }
    static void SaveCopy(GameObject source,string theme,string name)
    {
        var copy=UnityEngine.Object.Instantiate(source);
        try
        {
            copy.name=name;copy.transform.position=Vector3.zero;copy.transform.rotation=Quaternion.identity;
            copy.SetActive(true);
            var box=copy.GetComponent<BreakableProp>();
            if(box!=null)
            {
                var serialized=new SerializedObject(box);
                serialized.FindProperty("dropParent").objectReferenceValue=null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if(copy.GetComponent<BoxCollider2D>()==null || copy.GetComponent<CircleCollider2D>()==null)
                    throw new Exception("Missing block collider");
            }
            var trap=copy.GetComponent<FixedSpikeTrap>();
            if(trap!=null)
            {
                trap.room=null;
                if(trap.animationFrames.Length!=7 || trap.cycleSeconds!=10 || trap.damageArea==null)
                    throw new Exception("Incomplete spike prefab");
            }
            string path=Folder+"/"+theme+"/"+name+".prefab";
            PrefabUtility.SaveAsPrefabAsset(copy,path);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var component in asset.GetComponentsInChildren<Component>(true))
            {
                if(component==null)throw new Exception("Missing component in "+path);
                var data=new SerializedObject(component);var property=data.GetIterator();
                while(property.Next(true))
                {
                    if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    var reference=property.objectReferenceValue;
                    if(reference is GameObject || reference is Component)
                    {
                        string referencePath=AssetDatabase.GetAssetPath(reference);
                        if(!string.IsNullOrEmpty(referencePath) && referencePath!=path)
                            throw new Exception("External scene/map dependency: "+path+" -> "+referencePath);
                    }
                }
            }
            Debug.Log("DRAG_DROP_PREFAB_OK "+path+" scale="+asset.transform.localScale);
        }
        finally{UnityEngine.Object.DestroyImmediate(copy);}
    }
}
