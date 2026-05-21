using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;
using ChatApp.Models;
using System.Collections.Generic;

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

        private GameObject _typingIndicatorInstance;

        private void Start()
        {
            Debug.Log("CHAT SCREEN OBJECT = " + gameObject.name);

            Debug.Log("errorText = " + errorText);

            if (errorText != null)
                Debug.Log("errorText NAME = " + errorText.name);
            var conv = ConversationManager.Instance.ActiveConversation;
            if (conv == null) { UIManager.Instance.GoToConversations(); return; }

            convTitleText.text = conv.title ?? "Cuộc trò chuyện";
            backButton.onClick.AddListener(() => UIManager.Instance.GoToConversations());
            sendButton.onClick.AddListener(OnSendClicked);
            inputField.onSubmit.AddListener(_ => OnSendClicked());

            // Subscribe events
            ChatManager.Instance.OnMessagesLoaded += RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded += AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived += OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged += OnSendingStateChanged;
            ChatManager.Instance.OnError += ShowError;

            ChatManager.Instance.LoadMessages(conv.id_conversation);
        }

        private void OnDestroy()
        {
            ChatManager.Instance.OnMessagesLoaded -= RenderAllMessages;
            ChatManager.Instance.OnUserMessageAdded -= AppendUserMessage;
            ChatManager.Instance.OnAssistantMessageReceived -= OnAssistantReceived;
            ChatManager.Instance.OnSendingStateChanged -= OnSendingStateChanged;
            ChatManager.Instance.OnError -= ShowError;
        }

        // ── Render ────────────────────────────────────────────────────
        private void RenderAllMessages(List<Message> messages)
        {
            foreach (Transform child in messageContent) Destroy(child.gameObject);
            foreach (var msg in messages) SpawnMessage(msg);
            ScrollToBottom();
        }

        private void AppendUserMessage(Message msg)
        {
            SpawnMessage(msg);
            ScrollToBottom();
        }

        private void OnAssistantReceived(Message msg)
        {
            RemoveTypingIndicator();
            SpawnMessage(msg);
            ScrollToBottom();
        }

        private void SpawnMessage(Message msg)
        {
            var prefab = msg.role == "user" ? userMessagePrefab : assistantMessagePrefab;
            var go = Instantiate(prefab, messageContent);
            go.GetComponent<MessageItem>().Setup(msg.content);
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
            if (_typingIndicatorInstance != null)
            {
                Destroy(_typingIndicatorInstance);
                _typingIndicatorInstance = null;
            }
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
                ChatManager.Instance.SendMessage(conv.id_conversation, text);
            else
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

        private void ShowError(string err)
        {
            Debug.Log("ERROR MESSAGE = " + err);
            errorText.text = err;
            errorText.gameObject.SetActive(true);
        }

        //private void HideError() => {errorText.gameObject.SetActive(false)};

        private void HideError()
        {
            Debug.Log(errorText);

            if (errorText == null)
            {
                Debug.LogError("errorText is NULL");
                return;
            }

            errorText.gameObject.SetActive(false);
        }
    }
}