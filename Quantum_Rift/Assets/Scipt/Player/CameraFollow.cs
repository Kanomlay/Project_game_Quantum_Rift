using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    // กล้องสั่น (ตีโดน ทุบพื้น ระเบิด): สุ่มเลื่อนรอบตำแหน่งที่ตามอยู่ แรงค่อย ๆ ลดลงจนหมดเวลา
    // นับด้วยเวลาจริง ระหว่างหยุดภาพตอนตีโดนกล้องก็ยังสั่นอยู่
    public static CameraFollow Instance { get; private set; }
    private Vector3 followPosition;
    private Vector3 lastWritten;
    private bool initialized;
    private float shakeStrength, shakeDuration, shakeUntil;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Shake(float strength, float duration = 0.15f)
    {
        if (Instance == null || strength <= 0f) return;
        Instance.AddShake(strength, duration);
    }

    private void AddShake(float strength, float duration)
    {
        float remaining = CurrentShake();
        if (strength < remaining) return; // สั่นแรงกว่าอยู่แล้ว
        shakeStrength = strength;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeUntil = Time.unscaledTime + shakeDuration;
    }

    private float CurrentShake()
    {
        if (Time.unscaledTime >= shakeUntil) return 0f;
        return shakeStrength * (shakeUntil - Time.unscaledTime) / shakeDuration;
    }

    void LateUpdate()
    {
        // มีสคริปต์อื่นย้ายกล้องเอง (เช่นตอนโหลดแมพ) ให้ตามจากตำแหน่งใหม่นั้น
        if (!initialized || (transform.position - lastWritten).sqrMagnitude > 0.000001f)
        {
            followPosition = transform.position;
            initialized = true;
        }

        if (target != null)
        {
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, -10f);
            followPosition = Vector3.Lerp(followPosition, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        Vector3 offset = Vector3.zero;
        float shake = PauseManager.isGamePaused ? 0f : CurrentShake();
        if (shake > 0f) offset = (Vector3)(Random.insideUnitCircle * shake);

        transform.position = followPosition + offset;
        lastWritten = transform.position;
    }
}
