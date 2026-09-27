using UnityEngine;

/// <summary>หนามประจำจุด เตือนก่อนพุ่งขึ้นทุก 10 วินาที และทำความเสียหายหนึ่งครั้งต่อรอบ</summary>
public sealed class FixedSpikeTrap : MonoBehaviour
{
    public enum Phase { Retracted, Warning, Raised }
    public float cycleSeconds = 10f;
    public float warningSeconds = 1.2f;
    public float raisedSeconds = 1.4f;
    public float damage = 1f;
    public Transform spikes;
    public Renderer warningDisplay;
    public BoxCollider2D damageArea;
    public RoomController room;
    public SpriteRenderer animatedDisplay;
    public Sprite[] animationFrames;
    public Phase CurrentPhase { get; private set; }
    public int CompletedCycles { get; private set; }
    float elapsed;
    int hitCycle = -1;
    PlayerStats player;

    void OnEnable() { elapsed = 0f; hitCycle = -1; Advance(0f); }
    void Update()
    {
        if (PauseManager.isGamePaused || ShopWindow.IsOpen || BlessingManager.IsChoosing || Time.timeScale <= 0f) return;
        if (player == null)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero != null) player = hero.GetComponent<PlayerStats>();
        }
        if (player != null && player.isDead) return;
        Advance(Time.deltaTime);
        if (CurrentPhase == Phase.Raised && player != null && damageArea != null &&
            damageArea.OverlapPoint(player.transform.position)) TryHit(player);
    }

    public void Advance(float seconds)
    {
        if (room != null && room.IsSafeRoom)
        {
            CurrentPhase = Phase.Retracted;
            if (spikes != null) spikes.gameObject.SetActive(false);
            if (warningDisplay != null) warningDisplay.enabled = false;
            SetFrame(0);
            return;
        }
        elapsed += Mathf.Max(0f, seconds);
        float period = Mathf.Max(3f, cycleSeconds);
        float time = elapsed % period;
        CompletedCycles = Mathf.FloorToInt(elapsed / period);
        // ครั้งแรกพุ่งที่วินาที 10 จากนั้น 20, 30... ช่วงก่อนนั้นเห็นฐานกับไฟเตือน
        bool raised = CompletedCycles > 0 && time < raisedSeconds;
        bool warning = !raised && time >= period - warningSeconds;
        CurrentPhase = raised ? Phase.Raised : warning ? Phase.Warning : Phase.Retracted;
        int frame = 0;
        if (warning) frame = Mathf.FloorToInt(elapsed * 6f) % 2 == 0 ? 1 : 0;
        else if (raised)
        {
            float progress = time / Mathf.Max(.1f, raisedSeconds);
            frame = progress < .12f ? 2 : progress < .24f ? 3 : progress < .76f ? 4 : progress < .89f ? 5 : 6;
        }
        SetFrame(frame);
        if (warningDisplay != null) warningDisplay.enabled = warning && Mathf.FloorToInt(elapsed * 8f) % 2 == 0;
        if (spikes != null)
        {
            spikes.gameObject.SetActive(raised);
            float height = Mathf.Min(Mathf.Clamp01(time / .12f), Mathf.Clamp01((raisedSeconds-time) / .18f));
            spikes.localScale = new Vector3(1f, Mathf.Max(.05f,height), 1f);
        }
    }

    void SetFrame(int index)
    {
        if (animatedDisplay == null || animationFrames == null || animationFrames.Length == 0) return;
        animatedDisplay.sprite = animationFrames[Mathf.Clamp(index, 0, animationFrames.Length - 1)];
    }

    public bool TryHit(PlayerStats target)
    {
        if (target == null || target.isDead || CurrentPhase != Phase.Raised || hitCycle == CompletedCycles) return false;
        hitCycle = CompletedCycles;
        target.TakeDamage(damage);
        return true;
    }
}
