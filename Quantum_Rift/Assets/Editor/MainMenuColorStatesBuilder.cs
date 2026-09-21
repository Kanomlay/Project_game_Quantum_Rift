using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Blank state sprites and an independent label prevent text disappearing on press.
public static class MainMenuColorStatesBuilder
{
    public const string Folder="Assets/image/UI_Image/QuantumRift-Button-Approved-v3";
    public const string ScenePath="Assets/Scenes/MainMenu.unity";
    static Sprite Load(string state){var s=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Button-"+state+".png");if(s==null)throw new InvalidOperationException("Missing button state: "+state);return s;}
    [MenuItem("Tools/Quantum Rift/Apply Main Menu Color States")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath){if(scene.isDirty)throw new InvalidOperationException("Save the current scene first");scene=EditorSceneManager.OpenScene(ScenePath);}
        ApplyToScene(scene);EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Main Menu button states applied. Save the scene to keep the changes.");
    }
    public static void ApplyToScene(UnityEngine.SceneManagement.Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<Button>(true))
        {
            switch(b.name)
            {
                case "Button_Start":case "Start":ApplyWideButton(b.transform,"Start");break;
                case "Button_Setting":ApplyWideButton(b.transform,"Settings");break;
                case "Button_Exit":ApplyWideButton(b.transform,"Quit");break;
                case "BACK":ApplyWideButton(b.transform,"Back");break;
                default:ApplyIconButton(b);break;
            }
        }
    }
    public static void ApplyWideButton(Transform target,string word)
    {
        var image=target.GetComponent<Image>();var b=target.GetComponent<Button>();
        if(image==null||b==null)throw new InvalidOperationException("Expected Image and Button on "+target.name);
        // Fill the existing menu rectangle: aspect-fitting the taller replacement
        // artwork shrinks a 450-wide button to roughly 262 UI units.
        image.sprite=Load("Normal");image.overrideSprite=null;image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=false;image.raycastTarget=true;
        b.targetGraphic=image;b.transition=Selectable.Transition.SpriteSwap;
        b.spriteState=new SpriteState{highlightedSprite=Load("Hover"),pressedSprite=Load("Pressed"),selectedSprite=Load("Hover"),disabledSprite=Load("Disabled")};
        var labels=target.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text label=labels.Length>0?labels[0]:null;
        if(label==null){var go=new GameObject("StateLabel",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(target,false);label=go.GetComponent<TMP_Text>();}
        foreach(var other in labels)if(other!=label)other.gameObject.SetActive(false);
        foreach(var old in target.GetComponentsInChildren<Text>(true))old.gameObject.SetActive(false);
        label.gameObject.SetActive(true);label.enabled=true;label.text=word;label.color=new Color32(245,245,255,255);label.raycastTarget=false;
        label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=42;label.fontStyle=FontStyles.Bold;
        var parent=(RectTransform)target;var rect=label.rectTransform;
        float h=parent.rect.height;float w=parent.rect.width;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(w*.61f,h*.46f);rect.localScale=Vector3.one;
        rect.SetAsLastSibling();
        var feedback=target.GetComponent<MenuButtonLabelFeedback>();
        if(feedback==null)feedback=target.gameObject.AddComponent<MenuButtonLabelFeedback>();
        feedback.button=b;feedback.label=label;feedback.restingPosition=rect.anchoredPosition;feedback.pressedOffset=2;
        feedback.Refresh();
    }
    static void ApplyIconButton(Button b)
    {
        if(b.targetGraphic==null)return;
        b.transition=Selectable.Transition.ColorTint;
        if(b.targetGraphic is Image im)im.overrideSprite=null;
        b.targetGraphic.color=Color.white;
        var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color32(70,235,245,255);c.pressedColor=new Color32(255,173,40,255);c.selectedColor=c.highlightedColor;c.disabledColor=new Color32(100,105,115,170);c.colorMultiplier=1;c.fadeDuration=.05f;b.colors=c;
    }
}
