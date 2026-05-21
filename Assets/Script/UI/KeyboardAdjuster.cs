using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyboardAdjuster : MonoBehaviour
{
    [Header("References")]
    public RectTransform chatPanel;        // Kéo ChatPanel vào đây
    public TMP_InputField inputMessage;    // Kéo Input_Message vào đây

    private float _defaultPanelY;
    private bool _keyboardVisible = false;
    private Canvas _canvas;

    void Start()
    {
        _canvas = chatPanel.GetComponentInParent<Canvas>();
        _defaultPanelY = chatPanel.anchoredPosition.y;

        // Lắng nghe sự kiện focus vào input
        inputMessage.onSelect.AddListener(OnInputSelected);
        inputMessage.onDeselect.AddListener(OnInputDeselected);
    }

    void OnInputSelected(string text)
    {
        // Chờ bàn phím hiện ra rồi mới điều chỉnh
        StartCoroutine(AdjustForKeyboard());
    }

    void OnInputDeselected(string text)
    {
        ResetPanel();
    }

    System.Collections.IEnumerator AdjustForKeyboard()
    {
        // Chờ bàn phím hiện ra hoàn toàn
        yield return new WaitForSeconds(0.3f);

        float keyboardHeight = GetKeyboardHeight();
        if (keyboardHeight > 0)
        {
            ShiftPanelUp(keyboardHeight);
        }
    }

    float GetKeyboardHeight()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");
                AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect");
                decorView.Call("getWindowVisibleDisplayFrame", rect);

                int screenHeight = Screen.height;
                int visibleHeight = rect.Get<int>("bottom");
                int keyboardHeight = screenHeight - visibleHeight;

                return keyboardHeight > 100 ? keyboardHeight : 0f;
            }
        }
        catch
        {
            return TouchScreenKeyboard.area.height;
        }
#else
        // Trong Editor dùng để test
        //return 100f; // Giả định bàn phím cao 100px
        return TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
#endif
    }

    void ShiftPanelUp(float keyboardHeight)
    {
        float scaleFactor = _canvas.scaleFactor;
        float offset = keyboardHeight / scaleFactor;

        Vector2 pos = chatPanel.anchoredPosition;
        pos.y = _defaultPanelY + offset;
        chatPanel.anchoredPosition = pos;
    }

    void ResetPanel()
    {
        _keyboardVisible = false;
        Vector2 pos = chatPanel.anchoredPosition;
        pos.y = _defaultPanelY;
        chatPanel.anchoredPosition = pos;
    }

    void OnDestroy()
    {
        if (inputMessage != null)
        {
            inputMessage.onSelect.RemoveListener(OnInputSelected);
            inputMessage.onDeselect.RemoveListener(OnInputDeselected);
        }
    }
}