using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecommendButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    private RectTransform _rect;
    private const float HEIGHT = 40f;
    private const float ICON_SIZE = 20f;
    private const float SPACING = 10f;
    private const float PAD_LEFT = 0f;
    private const float PAD_RIGHT = 0f;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Setup(string text, Sprite iconSprite,
                      UnityEngine.Events.UnityAction onClick)
    {
        if (label) label.text = text;
        if (icon && iconSprite) icon.sprite = iconSprite;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        // Tự set height sau khi layout ổn định
        Canvas.ForceUpdateCanvases();
        ApplyHeight();
    }

    void ApplyHeight()
    {
        if (_rect == null) return;
        // Chỉ override height, giữ width do parent drive
        _rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, HEIGHT);
    }

    // Gọi lại mỗi frame phòng layout reset
    void LateUpdate()
    {
        if (_rect == null) return;
        if (!Mathf.Approximately(_rect.rect.height, HEIGHT))
            ApplyHeight();
    }
}