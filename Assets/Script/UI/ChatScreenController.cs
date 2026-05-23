using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;
using ChatApp.Models;
using ChatApp.Network;

namespace ChatApp.UI
{
    public class ChatScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text convTitleText;
        [SerializeField] private Button backButton;

        [Header("Message List")]
        [SerializeField] private Transform messageContent;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject assistantMessagePrefab;
        [SerializeField] private GameObject typingIndicatorPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Input Area")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Error")]
        [SerializeField] private TMP_Text errorText;

        [Header("Network Error Popup")]
        [SerializeField] private GameObject networkErrorOverlay;
        [SerializeField] private Button btnReload;
        [SerializeField] private Button btnExit;

        private GameObject _typingIndicatorInstance;
        private string _convId;
        private Action _pendingAction; // Lưu action gốc để replay khi retry

        private void Start()
        {
            var conv = ConversationManager.Instance.ActiveConversation;
            if (conv == null) { UIManager.Instance.GoToConversations(); return; }

            _convId = conv.id_conversation;
            convTitleText.text = conv.title ?? "Cuộc trò chuyện";

            backButton.onClick.AddListener(() => UIManager.Instance.GoToConversations());
            sendButton.onClick.AddListener(OnSendClicked);
            inputField.onSubmit.AddListener(_ => OnSendClicked());

            btnReload.onClick.AddListener(OnReloadClicked);
            btnExit.onClick.AddListener(OnExitClicked);

            networkErrorOverlay.SetActive(false);

            ChatManager.Instance.OnMessagesLoaded += RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded += AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived += OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged += OnSendingStateChanged;
            ChatManager.Instance.OnError += ShowError;
            ChatManager.Instance.OnNetworkError += ShowNetworkError;

            LoadMessages();
        }

        private void OnDestroy()
        {
            ChatManager.Instance.OnMessagesLoaded -= RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded -= AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived -= OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged -= OnSendingStateChanged;
            ChatManager.Instance.OnError -= ShowError;
            ChatManager.Instance.OnNetworkError -= ShowNetworkError;
        }

        // ── Load ──────────────────────────────────────────────────────
        private void LoadMessages()
        {
            _pendingAction = LoadMessages; // Lưu lại để retry
            ChatManager.Instance.LoadMessages(_convId);
        }

        // ── Render ────────────────────────────────────────────────────
        private void RenderAllMessages(List<Message> messages)
        {
            _pendingAction = null; // Thành công → xóa pending
            foreach (Transform child in messageContent) Destroy(child.gameObject);
            foreach (var msg in messages) SpawnMessage(msg);
            ScrollToBottom();
        }

        private void AppendUserMessage(Message msg) { SpawnMessage(msg); ScrollToBottom(); }

        private void OnAssistantReceived(Message msg)
        {
            _pendingAction = null; // Thành công → xóa pending
            RemoveTypingIndicator();
            SpawnMessage(msg);
            ScrollToBottom();
        }

        //private void SpawnMessage(Message msg)
        //{
        //    var prefab = msg.role == "user" ? userMessagePrefab : assistantMessagePrefab;
        //    var go = Instantiate(prefab, messageContent);
        //    go.GetComponent<MessageItem>().Setup(msg.content);
        //}
        //private void SpawnMessage(Message msg)
        //{
        //    var prefab = msg.role == "user" ? userMessagePrefab : assistantMessagePrefab;
        //    var go = Instantiate(prefab, messageContent);
        //    var item = go.GetComponent<MessageItem>();
        //    item.Setup(msg.content);

        //    if (msg.role == "assistant"
        //        && msg.metadata != null
        //        && msg.metadata.TryGetValue("recommend_target", out string target))
        //    {
        //        item.SetRecommend("Đến " + target + " ngay", () =>
        //        {
        //            Debug.Log("Đang dẫn đường đến " + target);
        //        });
        //    }
        //}
        private void SpawnMessage(Message msg)
        {
            var prefab = msg.role == "user" ? userMessagePrefab : assistantMessagePrefab;
            var go = Instantiate(prefab, messageContent);
            var item = go.GetComponent<MessageItem>();
            item.Setup(msg.content);

            if (msg.role == "assistant")
            {
                string target = msg.GetMeta("recommend_target");
                if (!string.IsNullOrEmpty(target))
                {
                    string displayName = GetBuildingName(target);
                    item.SetRecommend("  Đến " + displayName + " ngay", () =>
                    {
                        Debug.Log("Đang dẫn đường đến " + displayName);
                    });
                }
            }
        }
        private string GetBuildingName(string code)
        {
            switch (code)
            {
                case "building_ndh": return "Nhà Điều Hành";
                case "building_A": return "Toà A";
                case "building_B": return "Toà B";
                default: return code;
            }
        }
        // ── Typing indicator ──────────────────────────────────────────
        private void ShowTypingIndicator()
        {
            RemoveTypingIndicator();
            _typingIndicatorInstance = Instantiate(typingIndicatorPrefab, messageContent);
            ScrollToBottom();
        }

        private void RemoveTypingIndicator()
        {
            if (_typingIndicatorInstance == null) return;
            Destroy(_typingIndicatorInstance);
            _typingIndicatorInstance = null;
        }

        // ── Send ──────────────────────────────────────────────────────
        private void OnSendClicked()
        {
            string text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text) || ChatManager.Instance.IsSending) return;

            inputField.text = "";
            HideError();

            var conv = ConversationManager.Instance.ActiveConversation;
            var userId = AuthManager.Instance.CurrentUser.id;

            if (conv != null)
            {
                string convId = conv.id_conversation;
                _pendingAction = null;
                SendMessage(convId, text);
            }
            else
            {
                _pendingAction = null;
                SendMessageAutoCreate(userId, text);
            }
        }

        private void SendMessage(string convId, string text)
        {
            ChatManager.Instance.SendMessage(convId, text);
        }

        private void SendMessageAutoCreate(string userId, string text)
        {
            ChatManager.Instance.SendMessageAutoCreate(userId, text);
        }

        // ── State ─────────────────────────────────────────────────────
        private void OnSendingStateChanged(bool sending)
        {
            sendButton.interactable = !sending;
            inputField.interactable = !sending;
            if (sending) ShowTypingIndicator();
            else RemoveTypingIndicator();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // ── Error ─────────────────────────────────────────────────────
        private void ShowError(string err)
        {
            _pendingAction = null; // Lỗi server → không retry mạng
            RemoveTypingIndicator();
            errorText.text = err;
            errorText.gameObject.SetActive(true);
        }

        private void HideError()
        {
            if (errorText != null)
                errorText.gameObject.SetActive(false);
        }

        private void ShowNetworkError(string err)
        {
            RemoveTypingIndicator();
            networkErrorOverlay.SetActive(true);
            // _pendingAction giữ nguyên để retry
        }

        // ── Popup buttons ─────────────────────────────────────────────
        private void OnReloadClicked() => StartCoroutine(RetryConnection());

        private IEnumerator RetryConnection()
        {
            networkErrorOverlay.SetActive(false);
            HideError();
            SetLoading(true);


            // 2. Fetch lại Firebase link, chờ coroutine này xong hẳn
            yield return APIClient.Instance.Retry();

            // 3. Poll thêm tối đa 10s nếu Retry() chưa set cờ xong
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

            bool failed = !APIClient.Instance.IsReady; // LoadError != null hoặc timeout
            if (failed)
            {
                networkErrorOverlay.SetActive(true);
                yield break;
            }

            // 4. Firebase OK → replay action thất bại
            if (_pendingAction != null)
                _pendingAction.Invoke();
            else
                LoadMessages();
        }

        private void SetLoading(bool isLoading)
        {
            // Tuỳ UI của bạn — ví dụ disable nút reload, hiện spinner
            btnReload.interactable = !isLoading;
        }

        private void OnExitClicked()
        {
            _pendingAction = null;
            networkErrorOverlay.SetActive(false);
            UIManager.Instance.GoToLogin();
        }
    }
}