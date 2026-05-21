//using System;
//using System.Collections;
//using System.Text;
//using UnityEngine;
//using UnityEngine.Networking;

//namespace ChatApp.Network
//{
//    public class APIClient : MonoBehaviour
//    {
//        public static APIClient Instance { get; private set; }
//        public bool IsReady { get; private set; } = false;
//        public string LoadError { get; private set; } = null;

//        [Header("Config")]
//        [SerializeField] private string baseUrl = "http://localhost:3000";

//        private void Awake()
//        {
//            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        private IEnumerator Start()
//        {
//            var loader = GetComponent<FirebaseConfigLoader>();
//            if (loader == null)
//            {
//                LoadError = "FirebaseConfigLoader không tìm thấy.";
//                yield break;
//            }

//            loader.OnConfigLoaded += url => { baseUrl = url; IsReady = true; LoadError = null; };
//            loader.OnLoadFailed += msg => { IsReady = false; LoadError = msg; };

//            yield return loader.LoadConfig();
//        }

//        public IEnumerator Retry()
//        {
//            IsReady = false; LoadError = null; baseUrl = "";
//            var loader = GetComponent<FirebaseConfigLoader>();
//            if (loader != null) yield return loader.LoadConfig();
//        }

//        // ── GET ──────────────────────────────────────────────────────
//        public IEnumerator Get<T>(string endpoint,
//            Action<T> onSuccess, Action<string> onError)
//        {
//            yield return new WaitUntil(() => IsReady);
//            string url = baseUrl + endpoint;
//            using var req = UnityWebRequest.Get(url);
//            req.SetRequestHeader("Content-Type", "application/json");
//            yield return req.SendWebRequest();
//            HandleResponse(req, onSuccess, onError);
//        }

//        // ── POST ─────────────────────────────────────────────────────
//        public IEnumerator Post<T>(string endpoint, object body,
//            Action<T> onSuccess, Action<string> onError)
//        {
//            yield return new WaitUntil(() => IsReady);
//            string url  = baseUrl + endpoint;
//            string json = body != null ? JsonUtility.ToJson(body) : "{}";
//            Debug.Log("POST URL = " + url);
//            Debug.Log("JSON BODY = " + json);
//            byte[] raw  = Encoding.UTF8.GetBytes(json);

//            using var req = new UnityWebRequest(url, "POST");
//            req.uploadHandler   = new UploadHandlerRaw(raw);
//            req.downloadHandler = new DownloadHandlerBuffer();
//            req.SetRequestHeader("Content-Type", "application/json");
//            yield return req.SendWebRequest();
//            HandleResponse(req, onSuccess, onError);
//        }

//        // ── PATCH ────────────────────────────────────────────────────
//        public IEnumerator Patch<T>(string endpoint, object body,
//            Action<T> onSuccess, Action<string> onError)
//        {
//            yield return new WaitUntil(() => IsReady);
//            string url  = baseUrl + endpoint;
//            string json = JsonUtility.ToJson(body);
//            byte[] raw  = Encoding.UTF8.GetBytes(json);

//            using var req = new UnityWebRequest(url, "PATCH");
//            req.uploadHandler   = new UploadHandlerRaw(raw);
//            req.downloadHandler = new DownloadHandlerBuffer();
//            req.SetRequestHeader("Content-Type", "application/json");
//            yield return req.SendWebRequest();
//            HandleResponse(req, onSuccess, onError);
//        }

//        // ── DELETE ───────────────────────────────────────────────────
//        public IEnumerator Delete<T>(string endpoint,
//            Action<T> onSuccess, Action<string> onError)
//        {
//            yield return new WaitUntil(() => IsReady);
//            string url = baseUrl + endpoint;
//            using var req = UnityWebRequest.Delete(url);
//            req.downloadHandler = new DownloadHandlerBuffer();
//            req.SetRequestHeader("Content-Type", "application/json");
//            yield return req.SendWebRequest();
//            HandleResponse(req, onSuccess, onError);
//        }

//        // ── internal handler ─────────────────────────────────────────
//        private void HandleResponse<T>(UnityWebRequest req,
//            Action<T> onSuccess, Action<string> onError)
//        {
//            if (req.result == UnityWebRequest.Result.Success)
//            {
//                try
//                {
//                    T result = JsonUtility.FromJson<T>(req.downloadHandler.text);
//                    onSuccess?.Invoke(result);
//                }
//                catch (Exception e)
//                {
//                    onError?.Invoke($"Parse error: {e.Message}\n{req.downloadHandler.text}");
//                }
//            }
//            else
//            {
//                string msg = req.downloadHandler?.text ?? req.error;
//                onError?.Invoke(msg);
//            }
//        }
//    }

//}

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ChatApp.Network
{
    public class APIClient : MonoBehaviour
    {
        public static APIClient Instance { get; private set; }
        public bool IsReady { get; private set; } = false;
        public string LoadError { get; private set; } = null;

        [Header("Config")]
        [SerializeField] private string baseUrl = "http://localhost:3000";

        private FirebaseConfigLoader _loader;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

        public IEnumerator Retry()
        {
            IsReady = false;
            LoadError = null;
            baseUrl = "";
            yield return LoadConfig();
        }

        // ── Private: load + subscribe 1 lần duy nhất ─────────────────
        private IEnumerator LoadConfig()
        {
            if (_loader == null) yield break;

            // Unsubscribe trước để tránh chồng event khi retry
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

            // Unsubscribe ngay sau khi nhận được kết quả
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

        // ── GET ───────────────────────────────────────────────────────
        public IEnumerator Get<T>(string endpoint,
            Action<T> onSuccess, Action<string> onError)
        {
            yield return new WaitUntil(() => IsReady || LoadError != null);
            if (LoadError != null) { onError?.Invoke(LoadError); yield break; }

            using var req = UnityWebRequest.Get(baseUrl + endpoint);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        // ── POST ──────────────────────────────────────────────────────
        public IEnumerator Post<T>(string endpoint, object body,
            Action<T> onSuccess, Action<string> onError)
        {
            yield return new WaitUntil(() => IsReady || LoadError != null);
            if (LoadError != null) { onError?.Invoke(LoadError); yield break; }

            string json = body != null ? JsonUtility.ToJson(body) : "{}";
            byte[] raw = Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(baseUrl + endpoint, "POST");
            req.uploadHandler = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        // ── PATCH ─────────────────────────────────────────────────────
        public IEnumerator Patch<T>(string endpoint, object body,
            Action<T> onSuccess, Action<string> onError)
        {
            yield return new WaitUntil(() => IsReady || LoadError != null);
            if (LoadError != null) { onError?.Invoke(LoadError); yield break; }

            string json = JsonUtility.ToJson(body);
            byte[] raw = Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(baseUrl + endpoint, "PATCH");
            req.uploadHandler = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        // ── DELETE ────────────────────────────────────────────────────
        public IEnumerator Delete<T>(string endpoint,
            Action<T> onSuccess, Action<string> onError)
        {
            yield return new WaitUntil(() => IsReady || LoadError != null);
            if (LoadError != null) { onError?.Invoke(LoadError); yield break; }

            using var req = UnityWebRequest.Delete(baseUrl + endpoint);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            HandleResponse(req, onSuccess, onError);
        }

        // ── Internal handler ──────────────────────────────────────────
        private void HandleResponse<T>(UnityWebRequest req,
            Action<T> onSuccess, Action<string> onError)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    onSuccess?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text));
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Parse error: {e.Message}\n{req.downloadHandler.text}");
                }
            }
            else
            {
                onError?.Invoke(req.downloadHandler?.text ?? req.error);
            }
        }
    }
}