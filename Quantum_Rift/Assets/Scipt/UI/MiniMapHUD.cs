using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniMapHUD : MonoBehaviour
{
    readonly List<Image> markers=new List<Image>();
    readonly List<TMP_Text> questions=new List<TMP_Text>();
    Transform player;
    GameObject mapRoot;
    MapRoomGraph graph;
    RectTransform panel,playerDot;
    float[] xs,ys;
    float stepX,stepY,nextRefresh;
    public MapRoomGraph Graph => graph;
    public static void Show(HUDManager hud,GameObject map,Transform hero)
    {
        if(hud==null || map==null) return;
        var canvas=hud.GetComponentInParent<Canvas>();
        if(canvas==null) {var ui=GameObject.Find("UI"); if(ui!=null) canvas=ui.GetComponent<Canvas>();}
        if(canvas==null) return;
        var overlay=canvas.transform.Find("GameplayHUD"); if(overlay==null)return;
        var old=overlay.Find("MiniMapHUD");
        var go=old!=null?old.gameObject:new GameObject("MiniMapHUD",typeof(RectTransform));
        if(old==null)go.transform.SetParent(overlay,false);
        go.layer=5;
        var mini=go.GetComponent<MiniMapHUD>()??go.AddComponent<MiniMapHUD>();
        mini.Build(hud,map,hero);
    }
    void Build(HUDManager hud,GameObject map,Transform hero)
    {
        player=hero;
        Physics2D.SyncTransforms();
        if(mapRoot!=map || graph==null)graph=new MapRoomGraph(map);
        mapRoot=map;
        panel=(RectTransform)transform;
        panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;
        panel.anchoredPosition=new Vector2(-32,-132); panel.sizeDelta=new Vector2(320,232);
        var border=GetComponent<Image>()??gameObject.AddComponent<Image>();
        border.color=new Color32(105,81,151,255); border.raycastTarget=false;
        for(int i=transform.childCount-1;i>=0;i--)
        {
            var child=transform.GetChild(i).gameObject; child.SetActive(false);
            if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
        }
        markers.Clear();questions.Clear();
        Box("Inset",Vector2.zero,new Vector2(312,224),new Color32(12,17,31,245));
        Box("Accent",new Vector2(0,114),new Vector2(42,3),new Color32(98,212,228,255));
        gameObject.SetActive(graph.nodes.Count>0);if(graph.nodes.Count==0)return;
        xs=graph.nodes.Select(n=>n.center.x).Distinct().OrderBy(x=>x).ToArray();
        ys=graph.nodes.Select(n=>n.center.y).Distinct().OrderBy(y=>y).ToArray();
        stepX=Mathf.Min(78,252f/Mathf.Max(1,xs.Length-1));
        stepY=Mathf.Min(54,156f/Mathf.Max(1,ys.Length-1));
        foreach(var edge in graph.edges)
        {
            Vector2 a=Project(graph.nodes[edge.x].center),b=Project(graph.nodes[edge.y].center);
            var bridge=Box("Connection",(a+b)*.5f,new Vector2(Vector2.Distance(a,b),8),new Color32(85,76,121,255));
            bridge.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
        }
        Vector2 size=new Vector2(Mathf.Min(60,stepX-10),Mathf.Min(40,stepY-8));
        foreach(var node in graph.nodes)
        {
            var marker=Box("Room_"+node.id,Project(node.center),size,new Color32(69,57,104,255));
            markers.Add(marker);
            var label=new GameObject("Question",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(marker.transform,false);label.gameObject.layer=5;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            label.font=hud.currencyText.font;label.fontSize=24;label.text="?";
            label.alignment=TextAlignmentOptions.Center;label.overflowMode=TextOverflowModes.Overflow;
            label.raycastTarget=false;questions.Add(label);
        }
        playerDot=Box("Player",Vector2.zero,new Vector2(8,8),Color.white).rectTransform;
        Refresh();
    }
    Image Box(string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(panel,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.anchoredPosition=position;rect.sizeDelta=size;
        var image=go.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static float Rank(float value,float[] values)
    {
        if(values.Length<=1)return 0;
        for(int i=0;i<values.Length-1;i++)
            if(value<=values[i+1])return i+Mathf.InverseLerp(values[i],values[i+1],value)-(values.Length-1)*.5f;
        return (values.Length-1)*.5f;
    }
    Vector2 Project(Vector2 world)=>new Vector2(Rank(world.x,xs)*stepX,Rank(world.y,ys)*stepY);
    void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;Refresh();}
    public void Refresh()
    {
        if(player==null || graph==null || graph.nodes.Count==0)return;
        int current=graph.Observe(player.position);
        for(int i=0;i<markers.Count;i++)
        {
            var node=graph.nodes[i];
            if(node.room!=null && node.room.HasBeenVisited)node.visited=true;
            markers[i].color=i==current?new Color32(58,183,195,255):node.visited?
                new Color32(91,119,159,255):new Color32(69,57,104,255);
            questions[i].gameObject.SetActive(!node.visited);
        }
        playerDot.anchoredPosition=Project(player.position);
    }
}
