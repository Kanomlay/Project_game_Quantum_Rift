using UnityEngine;
using UnityEngine.UI;

/// <summary>แถบสีแบบขอบพิกเซลคม ใช้ geometry ของ UI ไม่ยืดภาพจนขอบเบลอ</summary>
public sealed class PixelHealthFill : Image
{
    public override Texture mainTexture => Texture2D.whiteTexture;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = GetPixelAdjustedRect();
        float right = r.xMin + r.width * Mathf.Clamp01(fillAmount);
        if (right <= r.xMin) return;
        Band(mesh,r.xMin,right,r.yMin,r.yMin+r.height*.2f,new Color(color.r*.65f,color.g*.65f,color.b*.65f,color.a));
        Band(mesh,r.xMin,right,r.yMin+r.height*.2f,r.yMin+r.height*.8f,color);
        Color highlight=Color.Lerp(color,Color.white,.4f);highlight.a=color.a;
        Band(mesh,r.xMin,right,r.yMin+r.height*.8f,r.yMax,highlight);
    }
    static void Band(VertexHelper mesh,float left,float right,float bottom,float top,Color color)
    {
        int start=mesh.currentVertCount;
        mesh.AddVert(new Vector3(left,bottom),color,Vector2.zero);
        mesh.AddVert(new Vector3(left,top),color,Vector2.zero);
        mesh.AddVert(new Vector3(right,top),color,Vector2.zero);
        mesh.AddVert(new Vector3(right,bottom),color,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
}
