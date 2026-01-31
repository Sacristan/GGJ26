using TMPro;
using UnityEngine;

public class UIMarquee : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform viewport;     // Viewport RectTransform (clipped area)
    [SerializeField] RectTransform textRect;     // Text (TMP) RectTransform
    [SerializeField] TMP_Text tmp;               // TMP component on Text

    [Header("Timing")]
    [SerializeField] float startDelay = 0.75f;   // blank time before scroll starts
    [SerializeField] float repeatDelay = 1.0f;   // blank time after a full pass before repeating / swapping

    [Header("Motion")]
    [SerializeField] float defaultSpeed = 15f;   // DEFAULT scroll speed (px/sec)

    [Header("Gap (auto)")]
    [Tooltip("Gap is computed from text width: clamp(textW * gapFactor, minGap, maxGap).")]
    [SerializeField] float gapFactor = 0.25f;
    [SerializeField] float minGap = 40f;
    [SerializeField] float maxGap = 240f;

    [Header("Layout (optional but recommended)")]
    [Tooltip("If true, forces Text RectTransform pivot/anchors to the left to avoid layout surprises.")]
    [SerializeField] bool forceLeftAnchorAndPivot = true;

    // Runtime layout
    float viewportW;
    float textW;
    float gap;

    float xStart;    // off-screen right (blank)
    float xEnd;      // off-screen left (blank)

    // Runtime state
    float timer;
    float currentSpeed;
    State state;

    bool hasActiveText;

    bool hasPending;
    string pendingText;
    float pendingSpeed;

    enum State
    {
        Idle,           // no text set -> no scrolling
        StartingBlank,  // startDelay before starting scroll (first entry)
        Scrolling,      // moving left
        RepeatBlank     // repeatDelay between passes (same text) OR before swapping to pending
    }

    public bool IsVisible =>
        state == State.Scrolling &&
        textRect &&
        textRect.anchoredPosition.x < viewportW &&
        textRect.anchoredPosition.x + textW > 0f;

    public bool IsActive => state != State.Idle;
    public bool IsScrolling => state == State.Scrolling;

    void Awake()
    {
        if (!viewport) viewport = transform as RectTransform;
        if (!textRect) Debug.LogError("UIMarquee: textRect not assigned.");
        if (!tmp) tmp = textRect ? textRect.GetComponent<TMP_Text>() : null;
        if (!tmp) Debug.LogError("UIMarquee: TMP_Text not assigned/found.");

        if (forceLeftAnchorAndPivot && textRect)
        {
            // Make anchoredPosition.x represent the LEFT edge consistently.
            textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
            textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);
            textRect.pivot = new Vector2(0f, textRect.pivot.y);
        }

        EnterIdle();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        switch (state)
        {
            case State.Idle:
                break;

            case State.StartingBlank:
                timer -= dt;
                if (timer <= 0f)
                    state = State.Scrolling;
                break;

            case State.Scrolling:
            {
                var p = textRect.anchoredPosition;
                p.x -= currentSpeed * dt;

                if (p.x <= xEnd)
                {
                    p.x = xEnd;
                    textRect.anchoredPosition = p;

                    state = State.RepeatBlank;
                    timer = repeatDelay;
                }
                else
                {
                    textRect.anchoredPosition = p;
                }

                break;
            }

            case State.RepeatBlank:
                timer -= dt;
                if (timer <= 0f)
                {
                    if (hasPending)
                    {
                        ApplyPendingAndStart();
                    }
                    else
                    {
                        SetX(xStart);
                        state = State.Scrolling;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Set marquee text.
    /// If overrideCurrent=true -> interrupt immediately and restart (blank -> startDelay -> scroll).
    /// If overrideCurrent=false -> queue and start after current text finishes its pass + repeatDelay.
    /// speed: px/sec (uses defaultSpeed if <= 0)
    /// </summary>
    public void SetText(string text, bool overrideCurrent = true, float speed = -1f)
    {
        if (string.IsNullOrEmpty(text))
        {
            StopAndClear();
            return;
        }

        float resolvedSpeed = speed > 0f ? speed : defaultSpeed;

        if (!hasActiveText || state == State.Idle)
        {
            StartNew(text, resolvedSpeed);
            return;
        }

        if (overrideCurrent)
        {
            hasPending = false;
            StartNew(text, resolvedSpeed);
        }
        else
        {
            pendingText = text;
            pendingSpeed = resolvedSpeed;
            hasPending = true;
        }
    }

    public void StopAndClear()
    {
        hasPending = false;
        hasActiveText = false;

        state = State.Idle;
        currentSpeed = defaultSpeed;

        if (tmp) tmp.text = "";
        SetX(999999f);
    }

    // ------------------------------------------------------------------
    // INTERNAL
    // ------------------------------------------------------------------

    void StartNew(string text, float speed)
    {
        hasActiveText = true;
        currentSpeed = speed;

        tmp.text = text;
        Recalc();

        // Start blank: keep off-screen right for startDelay
        SetX(xStart);
        state = State.StartingBlank;
        timer = startDelay;
    }

    void ApplyPendingAndStart()
    {
        string t = pendingText;
        float s = pendingSpeed;

        hasPending = false;
        StartNew(t, s);
    }

    void EnterIdle()
    {
        hasActiveText = false;
        hasPending = false;

        state = State.Idle;
        currentSpeed = defaultSpeed;

        if (tmp) tmp.text = "";
        SetX(999999f);
    }

    void Recalc()
    {
        if (!viewport || !textRect || !tmp) return;

        tmp.ForceMeshUpdate();

        viewportW = viewport.rect.width;
        textW = tmp.preferredWidth;

        // Ensure the rect is wide enough for the text
        var size = textRect.sizeDelta;
        size.x = textW;
        textRect.sizeDelta = size;

        // Auto gap from text width
        gap = Mathf.Clamp(textW * gapFactor, minGap, maxGap);

        // Pivot-safe start/end so long texts ALWAYS start fully off-screen right
        float pivotX = textRect.pivot.x;

        // Start: left edge exactly at viewport's right edge (+gap)
        // leftEdge = anchoredX - pivotX * textW  => anchoredX = leftEdge + pivotX * textW
        xStart = (viewportW + gap) + pivotX * textW;

        // End: right edge exactly past left edge of viewport (-gap)
        // rightEdge = anchoredX + (1 - pivotX) * textW  => anchoredX = rightEdge - (1 - pivotX) * textW
        xEnd = (-gap) - (1f - pivotX) * textW;
    }

    void SetX(float x)
    {
        var p = textRect.anchoredPosition;
        p.x = x;
        textRect.anchoredPosition = p;
    }
}
