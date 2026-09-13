using System.Collections;
using UnityEngine;

/// <summary>Seven poses: index 0 open, index 6 closed. GameObject stays enabled.</summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class AnimatedRoomGate : MonoBehaviour
{
    public Sprite[] frames = new Sprite[7];
    [Min(1f)] public float framesPerSecond = 10f;
    public bool initiallyClosed;
    public bool IsClosed { get; private set; }
    public int CurrentFrame { get; private set; }
    private SpriteRenderer display;
    private BoxCollider2D blocker;
    private Coroutine transition;
    private void Awake() { Cache(); SetClosed(initiallyClosed, true); }
    private void Cache()
    {
        if (display == null) display = GetComponent<SpriteRenderer>();
        if (blocker == null) blocker = GetComponent<BoxCollider2D>();
    }
    public void SetClosed(bool closed, bool immediate = false)
    {
        Cache(); IsClosed = closed;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        int target = closed ? Mathf.Max(0, (frames == null ? 0 : frames.Length) - 1) : 0;
        // Inset triggers keep the player past the threshold before closing.
        if (closed) blocker.enabled = true;
        if (immediate || !Application.isPlaying || !isActiveAndEnabled)
        {
            Apply(target); blocker.enabled = closed; return;
        }
        transition = StartCoroutine(Animate(target, closed));
    }
    private IEnumerator Animate(int target, bool closed)
    {
        while (CurrentFrame != target)
        {
            Apply(CurrentFrame + (target > CurrentFrame ? 1 : -1));
            yield return new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
        }
        blocker.enabled = closed; transition = null;
    }
    private void Apply(int index)
    {
        CurrentFrame = index;
        if (frames != null && frames.Length > index && frames[index] != null) display.sprite = frames[index];
    }
    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
    }
}
