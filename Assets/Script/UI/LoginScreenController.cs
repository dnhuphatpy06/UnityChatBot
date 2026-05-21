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
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private Button retryButton;

        //private void Start()
        //{
        //    ShowLoginPanel();
        //    loginButton.onClick.AddListener(OnLoginClicked);
        //    guestButton.onClick.AddListener(OnGuestClicked);
        //    registerButton.onClick.AddListener(OnRegisterClicked);

        //    // Kiểm tra session đã có
        //    if (AuthManager.Instance.IsLoggedIn)
        //        UIManager.Instance.GoToConversations();
        //}
        private IEnumerator Start()
        {
            ShowLoginPanel();
            loginButton.onClick.AddListener(OnLoginClicked);
            guestButton.onClick.AddListener(OnGuestClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);
            if (retryButton != null) { retryButton.onClick.AddListener(OnRetryClicked); retryButton.gameObject.SetActive(false); }

            // ── THÊM: chờ Firebase load xong ────────────────────
            SetLoading(true);
            yield return new WaitUntil(() =>
                APIClient.Instance != null &&
                (APIClient.Instance.IsReady || APIClient.Instance.LoadError != null)
            );
            SetLoading(false);

            if (APIClient.Instance.LoadError != null)
            {
                ShowError(APIClient.Instance.LoadError);
                SetInteractable(false);
                if (retryButton != null) retryButton.gameObject.SetActive(true);
                yield break;
            }
            // ────────────────────────────────────────────────────

            if (AuthManager.Instance.IsLoggedIn)
                UIManager.Instance.GoToConversations();
        }
        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            ClearError();
        }

        public void ShowRegisterPanel()
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            ClearError();
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
            SetLoading(true);
            AuthManager.Instance.Login(u, p,
                user => { Debug.Log("LOGIN SUCCESS"); SetLoading(false); UIManager.Instance.GoToConversations(); },
                err  => { Debug.LogError("LOGIN FAILED: " + err); SetLoading(false); ShowError(err); }
            );
        }

        private void OnRegisterClicked()
        {
            string u  = regUsernameField.text.Trim();
            string p  = regPasswordField.text;
            string fn = regFullnameField.text.Trim();
            string sid = regStudentIDField.text.Trim();
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                ShowError("Username và password không được để trống.");
                return;
            }
            SetLoading(true);
            AuthManager.Instance.Register(u, p, fn, sid,
                user => { SetLoading(false); UIManager.Instance.GoToConversations(); },
                err  => { SetLoading(false); ShowError(err); }
            );
        }

        private void OnGuestClicked()
        {
            SetLoading(true);
            AuthManager.Instance.LoginAsGuest(
                user => { SetLoading(false); UIManager.Instance.GoToConversations(); },
                err  => { SetLoading(false); ShowError(err); }
            );
        }

        private void ShowError(string msg)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }

        private void ClearError() => errorText.gameObject.SetActive(false);

        private void SetLoading(bool on)
        {
            loadingOverlay.SetActive(on);
            loginButton.interactable    = !on;
            registerButton.interactable = !on;
            guestButton.interactable    = !on;
        }
        private void OnRetryClicked() => StartCoroutine(RetryConnection());

        private IEnumerator RetryConnection()
        {
            ClearError();
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            SetLoading(true);

            // Cho APIClient cơ hội reset state trước
            yield return APIClient.Instance.Retry();

            // Timeout 10 giây thay vì WaitUntil vô hạn
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

            // Hết 10s mà vẫn chưa ready → coi như lỗi
            bool timedOut = !APIClient.Instance.IsReady && APIClient.Instance.LoadError == null;

            if (timedOut || APIClient.Instance.LoadError != null)
            {
                string msg = timedOut
                    ? "Vui lòng thử lại."
                    : APIClient.Instance.LoadError;

                ShowError(msg);
                SetInteractable(false);
                if (retryButton != null) retryButton.gameObject.SetActive(true);
            }
            else
            {
                SetInteractable(true);
                if (AuthManager.Instance.IsLoggedIn)
                    UIManager.Instance.GoToConversations();
            }
        }

        private void SetInteractable(bool on)
        {
            loginButton.interactable = on;
            registerButton.interactable = on;
            guestButton.interactable = on;
        }
    }
}