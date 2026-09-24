using UnityEngine;
using UnityEngine.Rendering;

/// <summary>ปูภาพเคลื่อนไหวต่อเนื่องตามพิกัดโลก ครอบคลุมกล้องทั้งในห้อง ทางเดิน และขอบด่าน</summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class WorldFlowBackdrop : MonoBehaviour
{
    public Sprite[] frames = new Sprite[7];
    [Min(1)] public float framesPerSecond = 5f;
    private SpriteRenderer display;
    private Camera mainCamera;

    void OnEnable()
    {
        display = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        Camera.onPreCull += BeforeLegacyCamera;
        if (mainCamera != null) RefreshForCamera(mainCamera, Time.time);
    }

    void LateUpdate()
    {
        if (mainCamera == null || !mainCamera.isActiveAndEnabled) mainCamera = Camera.main;
        if (mainCamera != null) RefreshForCamera(mainCamera, Time.time);
    }

    void BeforeCamera(ScriptableRenderContext context, Camera camera) => BeforeLegacyCamera(camera);
    void BeforeLegacyCamera(Camera camera)
    {
        if (camera == mainCamera && isActiveAndEnabled) RefreshForCamera(camera, Time.time);
    }

    // กล้องย้ายหลัง LateUpdate หรือเปลี่ยนขนาดหน้าต่างก็ยังมีภาพเต็ม viewport ก่อนวาดเฟรมนั้น
    public void RefreshForCamera(Camera camera, float time)
    {
        if (camera == null || frames == null || frames.Length == 0) return;
        if (display == null) display = GetComponent<SpriteRenderer>();
        var sprite = frames[Mathf.FloorToInt(Mathf.Max(0,time) * framesPerSecond) % frames.Length];
        if (sprite == null) return;
        display.sprite = sprite;
        display.drawMode = SpriteDrawMode.Tiled;
        display.tileMode = SpriteTileMode.Continuous;

        float sx = Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.x));
        float sy = Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.y));
        float tileWidth = sprite.bounds.size.x * sx;
        float tileHeight = sprite.bounds.size.y * sy;
        float depth = Mathf.Abs(transform.position.z - camera.transform.position.z);
        float halfHeight = camera.orthographic ? camera.orthographicSize
            : Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) * depth;
        float halfWidth = halfHeight * camera.aspect;
        // ใช้จำนวนไทล์คู่และเลื่อนเป็นจำนวนเต็มไทล์ จึงคงลายไว้กับโลก ไม่เลื่อนตามตัวละคร
        int columns = 2 * (Mathf.CeilToInt(halfWidth / tileWidth) + 2);
        int rows = 2 * (Mathf.CeilToInt(halfHeight / tileHeight) + 2);
        display.size = new Vector2(columns * sprite.bounds.size.x, rows * sprite.bounds.size.y);
        transform.position = new Vector3(
            Mathf.Floor(camera.transform.position.x / tileWidth) * tileWidth,
            Mathf.Floor(camera.transform.position.y / tileHeight) * tileHeight,
            transform.position.z);
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        Camera.onPreCull -= BeforeLegacyCamera;
    }
}
