using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MapGameplayInstaller
{
    const string Art1 = "Assets/image/Map/Map1-Unified-v2/PortalPad.png";
    const string Art2 = "Assets/image/Map/Map2-Forest-v1/Bramble.png";
    const string Hp = "Assets/image/Item/Potion-HP.png";
    const string Energy = "Assets/image/Item/Potion-Energy.png";
    static readonly string[] Map1 = { "map_1", "map_1_2", "map_1_3" };
    static readonly string[] Map2 = { "Map_2", "Map_2_2" };

    [MenuItem("Tools/Quantum Rift/Maps/Install Minimap Breakables And Traps")]
    public static void InstallAndVerify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first");
        var health = Load(Hp);
        var energy = Load(Energy);
        int configured = 0;
        foreach (var group in new[] { Map1, Map2 })
        {
            var sprite = Load(group == Map1 ? Art1 : Art2);
            var theme = group == Map1 ? MapGameplayFeatures.Theme.Spaceship :
                MapGameplayFeatures.Theme.LivingForest;
            foreach (string name in group)
            {
                string path = "Assets/Prefab/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var features = root.GetComponent<MapGameplayFeatures>();
                    if (features == null) features = root.AddComponent<MapGameplayFeatures>();
                    features.theme = theme;
                    features.trapSprite = sprite;
                    features.healthPotionSprite = health;
                    features.energyPotionSprite = energy;
                    var rooms = root.GetComponentsInChildren<RoomController>(true);
                    var props = root.GetComponentsInChildren<MapAssetRandomizer>(true);
                    if (rooms.Length < 2 || props.Length == 0)
                        throw new Exception("Missing room or randomized props in " + name);
                    EditorUtility.SetDirty(features);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    configured++;
                    Debug.Log("MAP_FEATURES " + name + " rooms=" + rooms.Length + " propGroups=" + props.Length);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        AssetDatabase.SaveAssets();
        if (configured != 5) throw new Exception("Not all map prefabs configured");
        Debug.Log("MAP_FEATURES_INSTALLED prefabs=" + configured);
    }

    static Sprite Load(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new FileNotFoundException("Missing sprite " + path);
        return sprite;
    }

    public static void VerifyPreview()
    {
        string output = Path.GetFullPath("../MapFeaturesPreview");
        var args = Environment.GetCommandLineArgs();
        int argument = Array.IndexOf(args, "-qrOutput");
        if (argument >= 0 && argument + 1 < args.Length) output = args[argument + 1];
        Directory.CreateDirectory(output);
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var canvas = GameObject.Find("UI").GetComponent<Canvas>();
        var hud = UnityEngine.Object.FindFirstObjectByType<HUDManager>();
        var camera = Camera.main;
        if (canvas == null || hud == null || camera == null) throw new Exception("Gameplay preview scene incomplete");
        foreach (Transform child in canvas.transform)
            if (child.name != "GameplayHUD") child.gameObject.SetActive(false);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.enabled = false;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        canvas.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
        canvas.sortingOrder = 32760;
        canvas.overrideSorting = true;
        hud.UpdateHP(6,8); hud.UpdateEnergy(42,50); hud.UpdateCurrency(1250);
        var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Character/Hero/นักรบ.asset");
        if (character != null) hud.SetupSkillIcons(character.skillQ.skillIcon, character.skillE.skillIcon);
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapon/Rusty Pistol.asset");
        if (weapon != null) hud.UpdateWeapon(weapon);
        int verified = 0;
        foreach (string name in new[] { "map_1", "Map_2" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + name + ".prefab");
            var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var layout = map.GetComponentInChildren<MapLayoutRandomizer>(true);
            if (layout != null)
                for (int i=0;i<layout.layouts.Length;i++)
                    layout.layouts[i].SetActive(i==0);
            var rooms = map.GetComponentsInChildren<RoomController>(false);
            var feature = map.GetComponent<MapGameplayFeatures>();
            feature.Initialize(new Vector2(9999,9999));
            int breakables = map.GetComponentsInChildren<BreakableProp>(false).Length;
            int traps = map.GetComponentsInChildren<StageTrap>(false).Length;
            if (rooms.Length < 2 || breakables < 1 || traps != 2)
                throw new Exception(name + " placement failed rooms=" + rooms.Length +
                    " breakables=" + breakables + " traps=" + traps);
            var hero = new GameObject("PreviewHero");
            hero.transform.position = rooms[0].transform.position;
            MiniMapHUD.Show(hud,map,hero.transform);
            var mini = canvas.transform.Find("GameplayHUD/MiniMapHUD");
            int questions = mini.GetComponent<MiniMapHUD>().Graph.nodes.Count;
            if (questions < rooms.Length)
                throw new Exception(name + " minimap rooms=" + questions + "/" + rooms.Length);
            camera.orthographicSize = 7f;
            camera.transform.position = new Vector3(hero.transform.position.x,hero.transform.position.y,-10f);
            var flow = map.GetComponentInChildren<WorldFlowBackdrop>();
            if (flow != null) flow.RefreshForCamera(camera,.6f);
            Capture(camera,canvas,Path.Combine(output,name+"-MiniMap.png"));
            Debug.Log("MAP_GAMEPLAY_VERIFIED " + name + " rooms=" + questions +
                " breakables=" + breakables + " traps=" + traps);
            UnityEngine.Object.DestroyImmediate(hero);
            UnityEngine.Object.DestroyImmediate(map);
            verified++;
        }
        if (verified != 2) throw new Exception("Preview verification incomplete");
        int layoutsChecked = 0;
        foreach (string name in Map1.Concat(Map2))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + name + ".prefab");
            var rootLayout = prefab.GetComponentInChildren<MapLayoutRandomizer>(true);
            int variants = rootLayout != null ? rootLayout.layouts.Length : 1;
            for (int variant = 0; variant < variants; variant++)
            {
                var map = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var layout = map.GetComponentInChildren<MapLayoutRandomizer>(true);
                    if (layout != null)
                        for (int i=0;i<layout.layouts.Length;i++) layout.layouts[i].SetActive(i==variant);
                    int roomCount = map.GetComponentsInChildren<RoomController>(false).Length;
                    map.GetComponent<MapGameplayFeatures>().Initialize(new Vector2(9999,9999));
                    int breakableCount = map.GetComponentsInChildren<BreakableProp>(false).Length;
                    int trapCount = map.GetComponentsInChildren<StageTrap>(false).Length;
                    if (roomCount < 2 || breakableCount < 1 || trapCount != 2)
                        throw new Exception(name + " layout=" + variant + " rooms=" + roomCount +
                            " breakables=" + breakableCount + " traps=" + trapCount);
                    layoutsChecked++;
                }
                finally { UnityEngine.Object.DestroyImmediate(map); }
            }
        }
        Debug.Log("MAP_LAYOUT_FEATURES_VERIFIED variants=" + layoutsChecked);
    }

    public static void Capture(Camera camera,Canvas canvas,string path)
    {
        const int width=1280, height=720;
        var rt = new RenderTexture(width,height,24);
        camera.targetTexture=rt;
        camera.aspect=(float)width/height;
        canvas.scaleFactor=Mathf.Sqrt((width/1920f)*(height/1080f));
        Canvas.ForceUpdateCanvases();
        foreach (var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
        camera.Render();
        var old=RenderTexture.active;
        RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=old;
        camera.targetTexture=null;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(rt);
    }
}
