using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
// Note; this uses the older UIElements system, not the newer UIToolkit. The newer one is more powerful but also more complicated to set up, so this is simpler for a basic status bar.
// this script also assumes the player has a PlayerCombat and playercon component, and that the status bar is in a UIDocument on the same GameObject as this script.
// You will need to make your own UI Document with the correct names for the bars and containers, or change the names in this script to match your UI Document.
// a simple guide to set this up is within the .md file in the same folder as this script.
public class StatusBarHandler : MonoBehaviour
{
    [Header("Player refs (auto-found by tag if left empty)")]
    public PlayerCombat combat;
    public playercon controller;

    [Header("Smoothing")]
    public bool smooth = true;
    public float hpLerpSpeed = 8f;
    public float dashLerpSpeed = 15f;

    CropMeter hp;
    CropMeter dash;
    float hpShown = 1f;
    float dashShown = 1f;

    // Crops a bar by resizing its container instead of squashing the bar.
    class CropMeter
    {
        readonly VisualElement container;
        readonly VisualElement bar;
        float fullWidth;
        bool ready;

        public CropMeter(VisualElement root, string containerName, string barName)
        {
            container = root.Q<VisualElement>(containerName);
            bar = root.Q<VisualElement>(barName);

            if (container == null) { Debug.LogError("StatusBarHandler: couldn't find #" + containerName); return; }
            if (bar == null) { Debug.LogError("StatusBarHandler: couldn't find #" + barName); return; }

            container.style.overflow = Overflow.Hidden; // this is what does the cropping
            container.RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        void OnGeometry(GeometryChangedEvent e)
        {
            // runs once layout is calculated, so we can read the full size
            if (ready || e.newRect.width <= 0f) return;

            fullWidth = e.newRect.width;
            bar.style.width = fullWidth;          // lock the art at its full pixel size
            bar.style.flexShrink = 0;             // stop flexbox from squeezing it
            container.style.width = fullWidth;
            ready = true;
            container.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        public void Set(float t)
        {
            if (!ready) return;
            container.style.width = fullWidth * Mathf.Clamp01(t);
        }
    }

    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        hp = new CropMeter(root, "HPBarContainer", "HPBar");
        dash = new CropMeter(root, "DashBarContainer", "DashBar");
    }

    void Start()
    {
        if (combat == null || controller == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                if (combat == null) combat = p.GetComponent<PlayerCombat>();
                if (controller == null) controller = p.GetComponent<playercon>();
            }
        }
        if (combat == null) Debug.LogError("StatusBarHandler: no PlayerCombat found");
        if (controller == null) Debug.LogError("StatusBarHandler: no playercon found");
    }

    void Update()
    {
        if (combat != null && hp != null)
        {
            float target = combat.maxHealth > 0 ? (float)combat.Health / combat.maxHealth : 0f;
            hpShown = Step(hpShown, target, hpLerpSpeed);
            hp.Set(hpShown);
        }

        if (controller != null && dash != null)
        {
            dashShown = Step(dashShown, controller.DashCharge, dashLerpSpeed);
            dash.Set(dashShown);
        }
    }

    float Step(float current, float target, float speed)
    {
        if (!smooth) return target;
        return Mathf.MoveTowards(current, target, speed * Time.unscaledDeltaTime);
    }
    public void SetVisible(bool visible)
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
// ancient incantaions lie above, but hey it works