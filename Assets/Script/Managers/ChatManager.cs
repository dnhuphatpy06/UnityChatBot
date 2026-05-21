using System;
using System.Collections.Generic;
using UnityEngine;
using ChatApp.Models;
using ChatApp.Network;

namespace ChatApp.Managers
{
    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; }

        public List<Message> CurrentMessages { get; private set; } = new();
        public bool IsSending { get; private set; }

        public event Action<List<Message>> OnMessagesLoaded;
        public event Action<Message>       OnUserMessageAdded;
        public event Action<Message>       OnAssistantMessageReceived;
        public event Action<bool>          OnSendingStateChanged; // true = đang gửi
        public event Action<string>        OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Load messages of a conversation ──────────────────────────
        public void LoadMessages(string convId)
        {
            CurrentMessages.Clear();
            StartCoroutine(APIClient.Instance.Get<MessageListResponse>(
                $"/api/conversations/{convId}/messages",
                res =>
                {
                    CurrentMessages = new List<Message>(res.messages ?? Array.Empty<Message>());
                    OnMessagesLoaded?.Invoke(CurrentMessages);
                },
                err => OnError?.Invoke(err)
            ));
        }

        // ── Send message ──────────────────────────────────────────────
        public void SendMessage(string convId, string content)
        {
            if (IsSending) return;

            // Hiển thị tin nhắn user ngay lập tức
            var optimistic = new Message
            {
                role    = "user",
                content = content,
                id      = Guid.NewGuid().ToString()
            };
            CurrentMessages.Add(optimistic);
            OnUserMessageAdded?.Invoke(optimistic);

            SetSending(true);

            var body = new SendMessageRequest { content = content };
            StartCoroutine(APIClient.Instance.Post<SendMessageResponse>(
                $"/api/conversations/{convId}/messages", body,
                res =>
                {
                    // Thay thế optimistic message bằng tin nhắn thật từ server
                    var idx = CurrentMessages.FindIndex(m => m.id == optimistic.id);
                    if (idx >= 0 && res.user_message != null)
                        CurrentMessages[idx] = res.user_message;

                    if (res.assistant_message != null)
                    {
                        CurrentMessages.Add(res.assistant_message);
                        OnAssistantMessageReceived?.Invoke(res.assistant_message);
                    }
                    SetSending(false);
                },
                err =>
                {
                    // Rollback optimistic message
                    CurrentMessages.RemoveAll(m => m.id == optimistic.id);
                    OnError?.Invoke(err);
                    SetSending(false);
                }
            ));
        }

        // ── Auto-create conversation rồi send ─────────────────────────
        public void SendMessageAutoCreate(string userId, string content)
        {
            ConversationManager.Instance.CreateConversation(userId, conv =>
            {
                ConversationManager.Instance.SetActiveConversation(conv);
                SendMessage(conv.id_conversation, content);
            });
        }

        private void SetSending(bool value)
        {
            IsSending = value;
            OnSendingStateChanged?.Invoke(value);
        }
    }
}