using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChatApp.Network
{
    public class APIClient : MonoBehaviour
    {
        public static APIClient Instance { get; private set; }
        public bool IsReady { get; private set; } = false;
        public string LoadError { get; private set; } = null;

        [Header("Config")]
        [SerializeField] private string baseUrl = "http://localhost:3000";

        [Header("Retry Settings")]
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float retryDelay = 1f;

        private FirebaseConfigLoader _loader;
        private readonly Queue<Func<IEnumerator>> _pendingQueue = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Lắng nghe sự kiện chuyển Scene ─────────────────────────────
        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene current, Scene next)
        {
            ClearPendingRequest(current.name, next.name);
        }

        private void ClearPendingRequest(string currentName = "", string nextName = "")
        {
            if (_pendingQueue.Count > 0)
            {
                Debug.Log($"[APIClient] Scene change {currentName}→{nextName}: Cleared {_pendingQueue.Count} pending requests.");
                _pendingQueue.Clear();
            }
        }
        // ──────────────────────────────────────────────────────────────

        private IEnumerator Start()
        {
            _loader = GetComponent<FirebaseConfigLoader>();
            if (_loader == null)
            {
                LoadError = "FirebaseConfigLoader không tìm thấy.";
                yield break;
            }

            yield return LoadConfig();
        }

        private bool _isRetrying = false;

        public IEnumerator Retry()
        {
            if (_isRetrying) yield break;
            _isRetrying = true;

            IsReady = false;
            LoadError = null;
            baseUrl = "";
            yield return LoadConfig();

            _isRetrying = false;
        }

        private IEnumerator LoadConfig()
        {
            if (_loader == null) yield break;

            _loader.OnConfigLoaded -= OnConfigLoaded;
            _loader.OnLoadFailed -= OnLoadFailed;

            _loader.OnConfigLoaded += OnConfigLoaded;
            _loader.OnLoadFailed += OnLoadFailed;

            yield return _loader.LoadConfig();
        }

        private void OnConfigLoaded(string url)
        {
            baseUrl = url;
            IsReady = true;
            LoadError = null;

            _loader.OnConfigLoaded -= OnConfigLoaded;
            _loader.OnLoadFailed -= OnLoadFailed;
        }

        private void OnLoadFailed(string msg)
        {
            IsReady = false;
            LoadError = msg;

            _loader.OnConfigLoaded -= OnConfigLoaded;
            _loader.OnLoadFailed -= OnLoadFailed;
        }

        private IEnumerator FlushPendingRequest()
        {
            Debug.Log($"[APIClient] Flushing {_pendingQueue.Count} pending requests...");
            while (_pendingQueue.Count > 0)
            {
                var factory = _pendingQueue.Dequeue();
                yield return StartCoroutine(factory());
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        /// true = lỗi mạng (cần retry), false = server phản hồi (4xx/5xx)
        private static bool IsNetworkError(UnityWebRequest req)
            => req.result == UnityWebRequest.Result.ConnectionError
            || req.result == UnityWebRequest.Result.DataProcessingError
            || (req.result == UnityWebRequest.Result.ProtocolError
                && (req.responseCode == 0 || req.responseCode >= 500));

        /// Gửi request, tự retry khi mất mạng.
        /// Nếu hết retry → lưu pendingFactory (ghi đè cái cũ) rồi gọi onNetworkError.
        private IEnumerator SendWithRetry(
            Func<UnityWebRequest> buildRequest,
            Action<UnityWebRequest> onDone,
            Action<string> onNetworkError,
            float timeoutSeconds = 60f)
        {
            using var req = buildRequest();

            // Bắt đầu gửi request với timeout
            var sendOp = req.SendWebRequest();
            float elapsed = 0f;

            while (!sendOp.isDone)
            {
                if (elapsed >= timeoutSeconds)
                {
                    req.Abort();
                    onNetworkError?.Invoke("Timeout: Không thể kết nối. Vui lòng thử lại.");
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (IsNetworkError(req))
            {
                onNetworkError?.Invoke("Mất kết nối. Vui lòng kiểm tra mạng và thử lại.");
                yield break;
            }

            onDone?.Invoke(req);
        }

        // ── HandleResponse ────────────────────────────────────────────
        //private void HandleResponse<T>(UnityWebRequest req,
        //    Action<T> onSuccess,
        //    Action<string> onServerError,
        //    Action<string> onNetworkError)
        //{
        //    if (IsNetworkError(req))
        //    {
        //        onNetworkError?.Invoke(req.error);
        //        return;
        //    }

        //    if (req.result == UnityWebRequest.Result.Success)
        //    {
        //        try { onSuccess?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text)); }
        //        catch (Exception e) { onServerError?.Invoke($"Parse error: {e.Message}"); }
        //    }
        //    else
        //    {
        //        onServerError?.Invoke(req.downloadHandler?.text ?? req.error);
        //    }
        //}
        private void HandleResponse<T>(UnityWebRequest req,
            Action<T> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError)
        {
            if (IsNetworkError(req))
            {
                onNetworkError?.Invoke(req.error);
                return;
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Đổi JsonUtility → JsonConvert
                    Debug.Log("[RAW] " + req.downloadHandler.text);
                    var result = JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
                    onSuccess?.Invoke(result);
                }
                catch (Exception e) { onServerError?.Invoke($"Parse error: {e.Message}"); }
            }
            else
            {
                onServerError?.Invoke(req.downloadHandler?.text ?? req.error);
            }
        }
        public IEnumerator Get<T>(string endpoint,
           Action<T> onSuccess,
           Action<string> onServerError,
           Action<string> onNetworkError = null)
        {
            yield return WaitUntilReadyOrTimeout();
            if (LoadError != null) { onNetworkError?.Invoke(LoadError); yield break; }

            yield return SendWithRetry(
                buildRequest: () =>
                {
                    var r = UnityWebRequest.Get(baseUrl + endpoint);
                    r.SetRequestHeader("Content-Type", "application/json");
                    return r;
                },
                onDone: req => HandleResponse(req, onSuccess, onServerError, onNetworkError),
                onNetworkError: onNetworkError
            );
        }

        public IEnumerator Post<T>(string endpoint, object body,
            Action<T> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            yield return WaitUntilReadyOrTimeout();
            if (LoadError != null) { onNetworkError?.Invoke(LoadError); yield break; }

            string json = body != null ? JsonUtility.ToJson(body) : "{}";

            yield return SendWithRetry(
                buildRequest: () =>
                {
                    byte[] raw = Encoding.UTF8.GetBytes(json);
                    var r = new UnityWebRequest(baseUrl + endpoint, "POST");
                    r.uploadHandler = new UploadHandlerRaw(raw);
                    r.downloadHandler = new DownloadHandlerBuffer();
                    r.SetRequestHeader("Content-Type", "application/json");
                    return r;
                },
                onDone: req => HandleResponse(req, onSuccess, onServerError, onNetworkError),
                onNetworkError: onNetworkError
            );
        }

        public IEnumerator Patch<T>(string endpoint, object body,
            Action<T> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            yield return WaitUntilReadyOrTimeout();
            if (LoadError != null) { onNetworkError?.Invoke(LoadError); yield break; }

            string json = JsonUtility.ToJson(body);

            yield return SendWithRetry(
                buildRequest: () =>
                {
                    byte[] raw = Encoding.UTF8.GetBytes(json);
                    var r = new UnityWebRequest(baseUrl + endpoint, "PATCH");
                    r.uploadHandler = new UploadHandlerRaw(raw);
                    r.downloadHandler = new DownloadHandlerBuffer();
                    r.SetRequestHeader("Content-Type", "application/json");
                    return r;
                },
                onDone: req => HandleResponse(req, onSuccess, onServerError, onNetworkError),
                onNetworkError: onNetworkError
            );
        }

        public IEnumerator Delete<T>(string endpoint,
            Action<T> onSuccess,
            Action<string> onServerError,
            Action<string> onNetworkError = null)
        {
            yield return WaitUntilReadyOrTimeout();
            if (LoadError != null) { onNetworkError?.Invoke(LoadError); yield break; }

            yield return SendWithRetry(
                buildRequest: () =>
                {
                    var r = UnityWebRequest.Delete(baseUrl + endpoint);
                    r.downloadHandler = new DownloadHandlerBuffer();
                    r.SetRequestHeader("Content-Type", "application/json");
                    return r;
                },
                onDone: req => HandleResponse(req, onSuccess, onServerError, onNetworkError),
                onNetworkError: onNetworkError
            );
        }
        private IEnumerator WaitUntilReadyOrTimeout(float timeoutSeconds = 10f)
        {
            float elapsed = 0f;
            yield return new WaitUntil(() => {
                elapsed += Time.deltaTime;
                return IsReady || LoadError != null || elapsed >= timeoutSeconds;
            });

            if (!IsReady && LoadError == null)
                LoadError = "Timeout: Không thể kết nối config sau " + timeoutSeconds + "s.";
        }
    }
}

