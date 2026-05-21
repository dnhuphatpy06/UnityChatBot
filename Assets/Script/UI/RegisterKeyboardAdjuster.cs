using UnityEngine;
using TMPro;

public class RegisterKeyboardAdjuster : MonoBehaviour
{
    [Header("References")]
    public RectTransform loginPanel;
    public TMP_InputField inputRegFullName;
    public TMP_InputField inputRegStudentID;

    private float _defaultPanelY;
    private Canvas _canvas;
    private bool _isAdjusted = false;  // Đang ở trạng thái đã đẩy lên chưa

    void Start()
    {
        _canvas = loginPanel.GetComponentInParent<Canvas>();
        _defaultPanelY = loginPanel.anchoredPosition.y;

        inputRegFullName.onSelect.AddListener(OnLowerInputSelected);
        inputRegStudentID.onSelect.AddListener(OnLowerInputSelected);

        inputRegFullName.onDeselect.AddListener(OnInputDeselected);
        inputRegStudentID.onDeselect.AddListener(OnInputDeselected);
    }

    void OnLowerInputSelected(string text)
    {
        // Hủy coroutine reset nếu đang chờ
        StopCoroutine("DelayedReset");

        // Nếu chưa đẩy lên thì mới đẩy, tránh nhảy khi chuyển giữa 2 field
        if (!_isAdjusted)
        {
            StartCoroutine(AdjustForKeyboard());
        }
    }

    void OnInputDeselected(string text)
    {
        // Không reset ngay, chờ xem có field mới được chọn không
        StartCoroutine(DelayedReset());
    }

    System.Collections.IEnumerator AdjustForKeyboard()
    {
        yield return new WaitForSeconds(0.3f);

        float keyboardHeight = GetKeyboardHeight();
        if (keyboardHeight > 0)
        {
            ShiftPanelUp(keyboardHeight);
            _isAdjusted = true;
        }
    }

    System.Collections.IEnumerator DelayedReset()
    {
        // Chờ 1 frame để onSelect của field mới kịp bắn ra
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Nếu sau 2 frame mà không có field nào được chọn thì mới reset
        if (!inputRegFullName.isFocused && !inputRegStudentID.isFocused)
        {
            ResetPanel();
            _isAdjusted = false;
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
        //return 100f; // Giả định bàn phím cao 100px trên editor để test
        return TouchScreenKeyboard.visible ? TouchScreenKeyboard.area.height : 0f;
#endif
    }

    void ShiftPanelUp(float keyboardHeight)
    {
        float scaleFactor = _canvas.scaleFactor;
        float offset = keyboardHeight / scaleFactor;

        Vector2 pos = loginPanel.anchoredPosition;
        pos.y = _defaultPanelY + offset;
        loginPanel.anchoredPosition = pos;
    }

    void ResetPanel()
    {
        Vector2 pos = loginPanel.anchoredPosition;
        pos.y = _defaultPanelY;
        loginPanel.anchoredPosition = pos;
    }

    void OnDestroy()
    {
        if (inputRegFullName != null)
        {
            inputRegFullName.onSelect.RemoveListener(OnLowerInputSelected);
            inputRegFullName.onDeselect.RemoveListener(OnInputDeselected);
        }
        if (inputRegStudentID != null)
        {
            inputRegStudentID.onSelect.RemoveListener(OnLowerInputSelected);
            inputRegStudentID.onDeselect.RemoveListener(OnInputDeselected);
        }
    }
}