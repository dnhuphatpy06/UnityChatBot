using System;
using System.Collections;
using UnityEngine;
using ChatApp.Models;
using ChatApp.Network;

namespace ChatApp.Managers
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private const string KEY_USER_ID   = "user_id";
        private const string KEY_USERNAME  = "username";
        private const string KEY_IS_GUEST  = "is_guest";

        public User CurrentUser { get; private set; }
        public bool IsLoggedIn  => CurrentUser != null;

        public event Action<User>   OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action<string> OnNetworkError;
        public event Action         OnLogout;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryRestoreSession();
        }

        // ── Restore PlayerPrefs session ───────────────────────────────
        private void TryRestoreSession()
        {
            string id = PlayerPrefs.GetString(KEY_USER_ID, "");
            if (string.IsNullOrEmpty(id)) return;
            CurrentUser = new User
            {
                id       = id,
                username = PlayerPrefs.GetString(KEY_USERNAME, "")
            };
        }

        public void Register(string username, string password,
            string fullname, string studentID,
            Action<User> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            var body = new RegisterRequest
            {
                username = username,
                password = password,
                full_name = fullname,
                student_id = studentID
            };
            StartCoroutine(APIClient.Instance.Post<User>(
                "/api/register", body,
                user => { SaveSession(user); onSuccess?.Invoke(user); },
                err => { onServerError?.Invoke(ParseError(err)); },
                err => { onNetworkError?.Invoke(err); OnNetworkError?.Invoke(err); }
            ));
        }

        public void Login(string username, string password,
            Action<User> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            var body = new LoginRequest { username = username, password = password };
            Debug.Log("CALL API POST /api/login");
            StartCoroutine(APIClient.Instance.Post<User>(
                "/api/login", body,
                user => { SaveSession(user); onSuccess?.Invoke(user); },
                err => { onServerError?.Invoke(ParseError(err)); },
                err => { onNetworkError?.Invoke(err); OnNetworkError?.Invoke(err); }
            ));
        }

        public void LoginAsGuest(
            Action<User> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            StartCoroutine(APIClient.Instance.Post<User>(
                "/api/guest", new object(),
                user => { SaveSession(user, isGuest: true); onSuccess?.Invoke(user); },
                err => { onServerError?.Invoke(ParseError(err)); },
                err => { onNetworkError?.Invoke(err); OnNetworkError?.Invoke(err); }
            ));
        }

        public void Logout(Action onDone = null)
        {
            if (CurrentUser == null) { onDone?.Invoke(); return; }

            bool isGuest = PlayerPrefs.GetInt(KEY_IS_GUEST, 0) == 1;
            if (isGuest)
            {
                string guestId = CurrentUser.id;
                StartCoroutine(APIClient.Instance.Delete<DeleteResponse>(
                    $"/api/guest/{guestId}",
                    _ => { },   // onSuccess  — bỏ qua
                    _ => { },   // onServerError — bỏ qua
                    _ => { }    // onNetworkError — bỏ qua
                ));
            }
            ClearSession();
            onDone?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void SaveSession(User user, bool isGuest = false)
        {
            CurrentUser = user;
            PlayerPrefs.SetString(KEY_USER_ID,  user.id);
            PlayerPrefs.SetString(KEY_USERNAME, user.username ?? "");
            PlayerPrefs.SetInt(KEY_IS_GUEST, isGuest ? 1 : 0);
            PlayerPrefs.Save();
            OnLoginSuccess?.Invoke(user);
        }

        private void ClearSession()
        {
            CurrentUser = null;
            PlayerPrefs.DeleteKey(KEY_USER_ID);
            PlayerPrefs.DeleteKey(KEY_USERNAME);
            PlayerPrefs.DeleteKey(KEY_IS_GUEST);
            PlayerPrefs.Save();
            OnLogout?.Invoke();
        }

        private string ParseError(string raw)
        {
            try
            {
                var err = JsonUtility.FromJson<ApiError>(raw);
                return err.error ?? err.message ?? raw;
            }
            catch { return raw; }
        }
    }
}