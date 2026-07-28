using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Smoothly pans the background RectTransform left-to-right using a sine wave.
// Attach to the BackgroundImage child of MainMenuScreen.
// Resizes itself to the sprite's real aspect ratio so it covers the screen +
// pan travel without ever stretching the artwork.
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class MainMenuBgPanner : MonoBehaviour
{
    [Tooltip("Total horizontal travel in pixels.")]
    public float panRange  = 100f;
    [Tooltip("Seconds to complete one full left-right-left cycle.")]
    public float cycleSecs = 22f;
    [Tooltip("Cream space kept visible above the illustration at the top of the screen.")]
    public float topMargin = 130f;

    private RectTransform _rt;
    private Image _image;
    private float _baseX;
    private bool _fitted;

    private void Awake()
    {
        _rt    = GetComponent<RectTransform>();
        _image = GetComponent<Image>();

        // Anchor/pivot from the top so anchoredPosition.y directly controls the
        // margin kept above the artwork, independent of the sine pan (X only).
        _rt.anchorMin = new Vector2(0.5f, 1f);
        _rt.anchorMax = new Vector2(0.5f, 1f);
        _rt.pivot     = new Vector2(0.5f, 1f);
    }

    private void Start()
    {
        // CanvasScaler hasn't necessarily finished adjusting the canvas for the real
        // screen by Awake — fitting here reads a stale parent rect and undersizes the
        // image. Wait one frame so layout has settled before measuring it.
        StartCoroutine(FitAfterLayout());
    }

    private IEnumerator FitAfterLayout()
    {
        yield return null;
        FitToCover();
        _baseX = _rt.anchoredPosition.x;
        _fitted = true;
    }

    // Scales the RectTransform to the sprite's native aspect ratio, sized so it covers
    // (screen width + panRange) x (screen height - topMargin) — same idea as CSS
    // "background-size: cover", but starting topMargin px below the screen's top edge
    // instead of filling all the way to it, so a cream gap stays visible above the art.
    private void FitToCover()
    {
        if (_image == null || _image.sprite == null) return;

        var parentRect = _rt.parent as RectTransform;
        float screenW = parentRect != null ? parentRect.rect.width  : 390f;
        float screenH = parentRect != null ? parentRect.rect.height : 844f;

        float requiredW = screenW + panRange;
        float requiredH = screenH - topMargin;

        var spriteRect    = _image.sprite.rect;
        float spriteAspect = spriteRect.width / spriteRect.height;

        float widthIfHeightMatched = requiredH * spriteAspect;

        float finalW, finalH;
        if (widthIfHeightMatched >= requiredW)
        {
            finalH = requiredH;
            finalW = widthIfHeightMatched;
        }
        else
        {
            finalW = requiredW;
            finalH = requiredW / spriteAspect;
        }

        _rt.sizeDelta = new Vector2(finalW, finalH);
        _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, -topMargin);
    }

    private void Update()
    {
        if (!_fitted) return;

        // Sin gives natural ease-in/ease-out at both ends — feels cinematic, not mechanical.
        float phase = (Time.time / cycleSecs) * Mathf.PI * 2f;
        var pos = _rt.anchoredPosition;
        pos.x = _baseX + Mathf.Sin(phase) * (panRange * 0.5f);
        _rt.anchoredPosition = pos;
    }
}
