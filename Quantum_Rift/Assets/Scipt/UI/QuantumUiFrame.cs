using UnityEngine;
using UnityEngine.UI;

// กรอบจาก geometry ของ UI จึงไม่มีลายภาพที่แตกเมื่อย่อ และไม่แย่งคลิกจากปุ่ม
[AddComponentMenu("UI/Quantum Clean Frame")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class QuantumUiFrame : MaskableGraphic
{
    [Min(1)] public float thickness=5;
    public void Configure(float width)
    {
        thickness=width;raycastTarget=false;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=rectTransform.rect;
        if(r.width<4||r.height<4)return;
        float t=Mathf.Min(thickness,Mathf.Min(r.width,r.height)*.15f);
        float cut=t*1.6f;
        // ขอบนอกเกลี่ย alpha หนึ่งพิกเซล ตามด้วยรางโลหะกว้างที่ไม่มี texture รบกวน
        Ring(mesh,r,0,1,cut,new Color32(16,20,34,0),new Color32(16,20,34,255),false);
        Ring(mesh,r,1,t,cut,new Color32(64,70,98,255),new Color32(37,41,64,255),true);
        Ring(mesh,r,t,t+1,cut,new Color32(88,119,149,255),new Color32(88,119,149,255),false);
        // ใช้ไฟเป็นเส้นสั้นเฉพาะกรอบใหญ่ แทนรายละเอียดจุกจิกตลอดขอบ
        if(r.width>180 && t>=3)
        {
            float length=Mathf.Clamp(r.width*.1f,18,66);
            Bar(mesh,new Rect(r.center.x-length*.5f,r.yMax-t*.6f,length,2),new Color32(111,224,239,255));
            if(r.height>120)
                Bar(mesh,new Rect(r.center.x-length*.25f,r.yMin+t*.6f-2,length*.5f,2),new Color32(161,114,219,255));
        }
    }
    static Vector2[] Outline(Rect r,float inset,float cut)
    {
        float l=r.xMin+inset,b=r.yMin+inset,h=r.yMax-inset,right=r.xMax-inset;
        float c=Mathf.Clamp(cut-inset,0,Mathf.Min(right-l,h-b)*.25f);
        return new[]{new Vector2(l+c,b),new Vector2(right-c,b),new Vector2(right,b+c),new Vector2(right,h-c),new Vector2(right-c,h),new Vector2(l+c,h),new Vector2(l,h-c),new Vector2(l,b+c)};
    }
    void Ring(VertexHelper mesh,Rect r,float outer,float inner,float cut,Color a,Color b,bool bevel)
    {
        var outside=Outline(r,outer,cut);var inside=Outline(r,inner,cut);
        for(int i=0;i<8;i++)
        {
            int next=(i+1)%8;
            Color light=bevel?(i>=3&&i<=5?new Color32(95,101,133,255):a):a;
            Quad(mesh,outside[i],outside[next],inside[next],inside[i],light,light,b,b);
        }
    }
    void Bar(VertexHelper mesh,Rect r,Color c)=>Quad(mesh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax),c,c,c,c);
    void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ca,Color cb,Color cc,Color cd)
    {
        int start=mesh.currentVertCount;
        mesh.AddVert(a,ca*color,Vector2.zero);mesh.AddVert(b,cb*color,Vector2.zero);
        mesh.AddVert(c,cc*color,Vector2.zero);mesh.AddVert(d,cd*color,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
}
