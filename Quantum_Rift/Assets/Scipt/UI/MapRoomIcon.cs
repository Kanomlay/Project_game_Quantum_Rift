using UnityEngine;
using UnityEngine.UI;

// ไอคอนวาดด้วย geometry ของ UI: ย่อแล้วคมชัด ไม่ใช่ช่องสี่เหลี่ยมหรืออักขระที่ฟอนต์อาจไม่มี
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapRoomIcon : MaskableGraphic
{
    public MapRoomGraph.RoomKind kind;
    public bool visited,current,cleared,exitBadge;
    public void Configure(MapRoomGraph.RoomKind value,bool seen,bool selected,bool done,bool exit)
    {
        if(kind==value&&visited==seen&&current==selected&&cleared==done&&exitBadge==exit)return;
        kind=value;visited=seen;current=selected;cleared=done;exitBadge=exit;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var ink=new Color32(14,17,32,255);
        Circle(mesh,Vector2.zero,15.5f,ink);
        Ring(mesh,Vector2.zero,15.5f,visited?1.6f:1f,current?new Color32(106,243,247,255):visited?new Color32(131,156,191,255):new Color32(72,65,105,255));
        Color light=kind==MapRoomGraph.RoomKind.Shop?new Color32(255,212,116,255):kind==MapRoomGraph.RoomKind.Boss?new Color32(255,137,159,255):new Color32(224,233,246,255);
        switch(kind)
        {
            case MapRoomGraph.RoomKind.Shop:
                Rect(mesh,-10,-9,20,13,light);Rect(mesh,-7,-8,14,10,ink);Rect(mesh,1,-8,4,8,light);Rect(mesh,-6,-3,5,4,new Color32(120,221,230,255));
                Poly(mesh,new[]{new Vector2(-12,3),new Vector2(12,3),new Vector2(9,10),new Vector2(-9,10)},light);
                for(int i=0;i<3;i++)Rect(mesh,-8+i*7,3,3,7,new Color32(122,65,145,255));break;
            case MapRoomGraph.RoomKind.Exit:
                Ring(mesh,Vector2.zero,11,3,new Color32(160,113,246,255),.65f);
                Rect(mesh,-5,-1,11,3,new Color32(118,240,247,255));Poly(mesh,new[]{new Vector2(2,-5),new Vector2(8,.5f),new Vector2(2,6)},new Color32(118,240,247,255));break;
            case MapRoomGraph.RoomKind.Start:
                Poly(mesh,new[]{new Vector2(-9,-2),new Vector2(0,9),new Vector2(9,-2),new Vector2(4,-2),new Vector2(4,-10),new Vector2(-4,-10),new Vector2(-4,-2)},new Color32(113,230,248,255));break;
            case MapRoomGraph.RoomKind.Empty:
                Poly(mesh,new[]{new Vector2(0,9),new Vector2(8,0),new Vector2(0,-9),new Vector2(-8,0)},new Color32(158,172,199,255));break;
            default:
                Poly(mesh,new[]{new Vector2(-10,-2),new Vector2(-10,6),new Vector2(-6,10),new Vector2(6,10),new Vector2(10,6),new Vector2(10,-2),new Vector2(6,-6),new Vector2(-6,-6)},light);
                Circle(mesh,new Vector2(-4,2),3,ink);Circle(mesh,new Vector2(4,2),3,ink);
                Poly(mesh,new[]{new Vector2(0,0),new Vector2(-2,-4),new Vector2(2,-4)},ink);
                for(int i=0;i<3;i++)Rect(mesh,-5+i*4,-10,3,5,light);
                if(kind==MapRoomGraph.RoomKind.Boss){Poly(mesh,new[]{new Vector2(-9,5),new Vector2(-13,13),new Vector2(-3,9)},light);Poly(mesh,new[]{new Vector2(9,5),new Vector2(13,13),new Vector2(3,9)},light);}break;
        }
        // วาร์ปยังแสดงได้แม้ห้องเป็นบอส; ห้องที่ผ่านแล้วมีเครื่องหมายถูก ไม่ใช้แค่สี
        if(exitBadge&&kind!=MapRoomGraph.RoomKind.Exit){Circle(mesh,new Vector2(11,10),5,ink);Ring(mesh,new Vector2(11,10),4,1.5f,new Color32(182,129,255,255),.65f);}
        if(cleared&&visited){Circle(mesh,new Vector2(10,-10),5.5f,ink);Line(mesh,new Vector2(7,-10),new Vector2(9,-12),1.8f,new Color32(129,245,191,255));Line(mesh,new Vector2(9,-12),new Vector2(13,-7),1.8f,new Color32(129,245,191,255));}
    }
    Vector2 Point(Vector2 p)=>rectTransform.rect.center+p*(Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/34f);
    void Poly(VertexHelper m,Vector2[] points,Color c)
    {
        int start=m.currentVertCount;foreach(var p in points)m.AddVert(Point(p),c,Vector2.zero);for(int i=1;i<points.Length-1;i++)m.AddTriangle(start,start+i,start+i+1);
    }
    void Rect(VertexHelper m,float x,float y,float w,float h,Color c)=>Poly(m,new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)},c);
    void Circle(VertexHelper m,Vector2 center,float radius,Color c)
    {
        var p=new Vector2[32];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}Poly(m,p,c);
    }
    void Ring(VertexHelper m,Vector2 center,float r,float width,Color c,float xScale=1)
    {
        for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;var u=new Vector2(Mathf.Cos(a)*xScale,Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b)*xScale,Mathf.Sin(b));Poly(m,new[]{center+u*r,center+v*r,center+v*(r-width),center+u*(r-width)},c);}
    }
    void Line(VertexHelper m,Vector2 a,Vector2 b,float width,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;Poly(m,new[]{a+n,b+n,b-n,a-n},c);}
}
