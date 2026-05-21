using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ChatApp.Network
{
    /// <summary>
    /// Fetch base_url từ Firestore REST API.
    /// - KHÔNG có fallback URL cứng
    /// - Không mạng hoặc fetch lỗi → OnLoadFailed(message)
    /// - Thành công                 → OnConfigLoaded(publicUrl)
    /// </summary>
    public class FirebaseConfigLoader : MonoBehaviour
    {
        [Header("Firestore Config")]
        [Tooltip("https://firestore.googleapis.com/v1/projects/aicampus-858b3/databases/(default)/documents/server_config/backend_url")]
        [SerializeField] private string firestoreBaseUrl = "https://firestore.googleapis.com/v1/projects/aicampus-858b3/databases/(default)/documents";

        [Tooltip("Collection/DocumentId — ví dụ: server_config/backend_url")]
        [SerializeField] private string firestoreDocPath = "server_config/backend_url";

        [Header("Timeout (giây)")]
        [SerializeField] private float timeoutSeconds = 10f;

        // ── Events ───────────────────────────────────────────────────
        /// <summary>Gọi khi lấy được public URL.</summary>
        public event Action<string> OnConfigLoaded;

        /// <summary>Gọi khi thất bại, kèm message hiển thị cho user.</summary>
        public event Action<string> OnLoadFailed;

        // ── Public entry ─────────────────────────────────────────────
        public IEnumerator LoadConfig()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Fail("Không có kết nối Internet.\nVui lòng kiểm tra lại mạng.");
                yield break;
            }

            yield return FetchFromFirestore();
        }

        // ── Internal ─────────────────────────────────────────────────
        private IEnumerator FetchFromFirestore()
        {
            string url = $"{firestoreBaseUrl.TrimEnd('/')}/{firestoreDocPath}";
            Debug.Log($"[FirebaseConfig] Fetching: {url}");

            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.RoundToInt(timeoutSeconds);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"RESULT = {req.result}");
                Debug.Log($"HTTP CODE = {req.responseCode}");
                Debug.Log($"ERROR = {req.error}");
                Debug.Log($"BODY = {req.downloadHandler.text}");
                Fail("Không thể kết nối đến máy chủ.\nVui lòng thử lại.");
                yield break;
            }

            try
            {
                // Firestore REST response:
                // { "fields": { "base_url": { "stringValue": "https://xxxx.trycloudflare.com" } } }
                var doc = JsonUtility.FromJson<FirestoreDocument>(req.downloadHandler.text);
                string publicUrl = doc?.fields?.base_url?.stringValue;

                if (!string.IsNullOrEmpty(publicUrl))
                {
                    Debug.Log($"[FirebaseConfig] ✅ base_url = {publicUrl}");
                    OnConfigLoaded?.Invoke(publicUrl.TrimEnd('/'));
                }
                else
                {
                    Fail("Không lấy được địa chỉ máy chủ.\nVui lòng thử lại.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseConfig] Parse error: {e.Message}");
                Fail("Lỗi xử lý dữ liệu từ máy chủ.\nVui lòng thử lại.");
            }
        }

        private void Fail(string msg)
        {
            Debug.LogWarning($"[FirebaseConfig] ❌ {msg}");
            OnLoadFailed?.Invoke(msg);
        }

        // ── Firestore DTOs ───────────────────────────────────────────
        [Serializable] private class FirestoreDocument { public FirestoreFields fields; }
        [Serializable] private class FirestoreFields { public FirestoreStringValue base_url; }
        [Serializable] private class FirestoreStringValue { public string stringValue; }
    }
}