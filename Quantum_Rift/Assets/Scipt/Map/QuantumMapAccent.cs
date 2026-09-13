using UnityEngine;

/// <summary>Subtle ambient machinery pulse. Visual only; no damage or gameplay state.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class QuantumMapAccent : MonoBehaviour
{
    [Range(0f,.2f)] public float intensity=.08f;
    [Min(2f)] public float period=4f;
    SpriteRenderer display;
    Color original;
    void Awake(){display=GetComponent<SpriteRenderer>();original=display.color;}
    void Update()
    {
        float phase=transform.position.x*.17f+transform.position.y*.13f;
        float value=1f-intensity*(.5f+.5f*Mathf.Sin(Time.time*Mathf.PI*2f/Mathf.Max(2f,period)+phase));
        display.color=new Color(original.r*value,original.g*value,original.b*value,original.a);
    }
    void OnDisable(){if(display!=null)display.color=original;}
}
