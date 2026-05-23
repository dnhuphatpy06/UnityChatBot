using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;
using ChatApp.Network;
using System.Collections;

namespace ChatApp.UI
{
    public class LoginScreenController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;

        [SerializeField] private Button tabLoginButton;     // thêm
        [SerializeField] private Button tabRegisterButton;  // thêm

        [SerializeField] private TMP_Text loginText;
        [SerializeField] private TMP_Text registerText;


        [SerializeField] private Color tabActiveColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color tabInactiveColor = new Color(0.8f, 0.8f, 0.8f);

        [SerializeField] private Color textActiveColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color textInactiveColor = new Color(0.8f, 0.8f, 0.8f);

        [Header("Login - Show Password")]
        [SerializeField] private Button loginShowPasswordButton;
        [SerializeField] private Image loginIconEye;       // icon mắt mở
        [SerializeField] private Image loginIconEyeOff;    // icon mắt đóng (gạch chéo)

        [Header("Register - Show Password")]
        [SerializeField] private Button regShowPasswordButton;
        [SerializeField] private Image regIconEye;
        [SerializeField] private Image regIconEyeOff;

        [Header("Login Fields")]
        [SerializeField] private TMP_InputField loginUsernameField;
        [SerializeField] private TMP_InputField loginPasswordField;
        [SerializeField] private Button         loginButton;
        [SerializeField] private Button         guestButton;

        [Header("Register Fields")]
        [SerializeField] private TMP_InputField regUsernameField;
        [SerializeField] private TMP_InputField regPasswordField;
        [SerializeField] private TMP_InputField regFullnameField;
        [SerializeField] private TMP_InputField regStudentIDField;
        [SerializeField] private Button         registerButton;

        [Header("Shared")]
        //[SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text loginErrorText;     // THÊM
        [SerializeField] private TMP_Text registerErrorText;  // THÊM
        [SerializeField] private GameObject loadingOverlay;

        [Header("Network Error Popup")]
        [SerializeField] private GameObject errorOverlay;   // Kéo ErrorOverlay vào đây
        [SerializeField] private Button btnReload;      // Kéo Btn_Reload vào đây
        [SerializeField] private Button btnExit;        // Kéo Btn_Exit vào đây

        private Action _pendingAction;
        private bool _loginPasswordVisible = false;
        private bool _regPasswordVisible = false;

        private IEnumerator Start()
        {
            ShowLoginPanel();

            // Gắn sự kiện cho các nút đăng nhập/đăng ký
            loginButton.onClick.AddListener(OnLoginClicked);
            guestButton.onClick.AddListener(OnGuestClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);

            // Gắn sự kiện cho Popup Lỗi mạng
            btnReload.onClick.AddListener(OnRetryClicked);
            btnExit.onClick.AddListener(OnExitClicked);

            // Eyes
            loginShowPasswordButton.onClick.AddListener(() => {
                _loginPasswordVisible = !_loginPasswordVisible;
                loginPasswordField.contentType = _loginPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                loginPasswordField.ForceLabelUpdate();
                loginIconEye.gameObject.SetActive(_loginPasswordVisible);
                loginIconEyeOff.gameObject.SetActive(!_loginPasswordVisible);
            });

            regShowPasswordButton.onClick.AddListener(() => {
                _regPasswordVisible = !_regPasswordVisible;
                regPasswordField.contentType = _regPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                regPasswordField.ForceLabelUpdate();
                regIconEye.gameObject.SetActive(_regPasswordVisible);
                regIconEyeOff.gameObject.SetActive(!_regPasswordVisible);
            });

            // Trạng thái ban đầu
            loginIconEye.gameObject.SetActive(false);
            loginIconEyeOff.gameObject.SetActive(true);
            regIconEye.gameObject.SetActive(false);
            regIconEyeOff.gameObject.SetActive(true);

            // Ẩn popup lỗi mạng khi mới mở app
            HideNetworkErrorPopup();

            // ── Bắt đầu kiểm tra kết nối Server ──
            SetLoading(true);
            yield return new WaitUntil(() =>
                APIClient.Instance != null &&
                (APIClient.Instance.IsReady || APIClient.Instance.LoadError != null)
            );
            SetLoading(false);

            // Nếu thất bại kết nối ngay từ đầu -> Bật Popup Lỗi
            if (APIClient.Instance.LoadError != null)
            {
                ShowNetworkErrorPopup();
                SetInteractable(false);
                yield break; // Dừng tại đây, không xử lý tiếp
            }

            // Nếu thành công và đã đăng nhập -> Vào thẳng app
            if (AuthManager.Instance.IsLoggedIn)
                UIManager.Instance.GoToConversations();
        }
        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            ClearError();
            tabLoginButton.image.color = tabActiveColor;
            tabRegisterButton.image.color = tabInactiveColor;
            loginText.color = textActiveColor;
            registerText.color = textInactiveColor;
        }

        public void ShowRegisterPanel()
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            ClearError();
            tabLoginButton.image.color = tabInactiveColor;
            tabRegisterButton.image.color = tabActiveColor;
            loginText.color = textInactiveColor;
            registerText.color = textActiveColor;
        }

        private void ShowNetworkErrorPopup() => errorOverlay.SetActive(true);
        private void HideNetworkErrorPopup() => errorOverlay.SetActive(false);

        private void ShowError(string msg)
        {
            // Hiển thị lỗi đúng panel đang active
            if (loginPanel.activeSelf)
            {
                loginErrorText.text = msg;
                loginErrorText.gameObject.SetActive(true);
            }
            else
            {
                registerErrorText.text = msg;
                registerErrorText.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            loginErrorText.gameObject.SetActive(false);
            registerErrorText.gameObject.SetActive(false);
        }

        private void SetLoading(bool on)
        {
            loadingOverlay.SetActive(on);
            loginButton.interactable    = !on;
            registerButton.interactable = !on;
            guestButton.interactable    = !on;
        }
        private void OnLoginClicked()
        {
            string u = loginUsernameField.text.Trim();
            string p = loginPasswordField.text;
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                ShowError("Vui lòng điền đầy đủ thông tin.");
                return;
            }
            _pendingAction = OnLoginClicked; // Lưu lại
            SetLoading(true);
            AuthManager.Instance.Login(u, p,
                onSuccess: user =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    UIManager.Instance.GoToConversations();
                },
                onServerError: err =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    ShowError(err);
                },
                onNetworkError: err =>
                {
                    SetLoading(false);
                    ShowNetworkErrorPopup();
                    SetInteractable(false);
                }
            );
        }

        private void OnRegisterClicked()
        {
            string u = regUsernameField.text.Trim();
            string p = regPasswordField.text;
            string fn = regFullnameField.text.Trim();
            string sid = regStudentIDField.text.Trim();
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                ShowError("Vui lòng điền đầy đủ thông tin.");
                return;
            }
            _pendingAction = OnRegisterClicked; // Lưu lại
            SetLoading(true);
            AuthManager.Instance.Register(u, p, fn, sid,
                onSuccess: user =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    UIManager.Instance.GoToConversations();
                },
                onServerError: err =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    ShowError(err);
                },
                onNetworkError: err =>
                {
                    SetLoading(false);
                    ShowNetworkErrorPopup();
                    SetInteractable(false);
                }
            );
        }

        private void OnGuestClicked()
        {
            _pendingAction = OnGuestClicked; // Lưu lại
            SetLoading(true);
            AuthManager.Instance.LoginAsGuest(
                onSuccess: user =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    UIManager.Instance.GoToConversations();
                },
                onServerError: err =>
                {
                    _pendingAction = null;
                    SetLoading(false);
                    ShowError(err);
                },
                onNetworkError: err =>
                {
                    SetLoading(false);
                    ShowNetworkErrorPopup();
                    SetInteractable(false);
                }
            );
        }

        private void OnRetryClicked() => StartCoroutine(RetryConnection());

        private IEnumerator RetryConnection()
        {
            HideNetworkErrorPopup();
            SetLoading(true);

            yield return APIClient.Instance.Retry();

            float elapsed = 0f;
            const float timeout = 10f;
            while (elapsed < timeout)
            {
                if (APIClient.Instance.IsReady || APIClient.Instance.LoadError != null)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetLoading(false);
            bool timedOut = !APIClient.Instance.IsReady && APIClient.Instance.LoadError == null;

            if (timedOut || APIClient.Instance.LoadError != null)
            {
                ShowNetworkErrorPopup();
                SetInteractable(false);
            }
            else
            {
                SetInteractable(true);
                _pendingAction?.Invoke();
            }
        }

        private void SetInteractable(bool on)
        {
            loginButton.interactable = on;
            registerButton.interactable = on;
            guestButton.interactable = on;
        }
        private void OnExitClicked()
        {
            Debug.Log("Thoát ứng dụng");
            HideNetworkErrorPopup();
            SetInteractable(true);
        }
    }
}