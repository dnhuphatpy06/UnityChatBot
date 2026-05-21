using System;
using System.Collections.Generic;
using UnityEngine;
using ChatApp.Models;
using ChatApp.Network;

namespace ChatApp.Managers
{
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        public List<Conversation> Conversations { get; private set; } = new();
        public Conversation ActiveConversation  { get; private set; }

        public event Action<List<Conversation>> OnConversationsLoaded;
        public event Action<Conversation>       OnConversationCreated;
        public event Action<string>             OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Load list ────────────────────────────────────────────────
        public void LoadConversations(string userId)
        {
            Debug.Log($"[ConversationManager] LoadConversations called. userId = {userId}");
            StartCoroutine(APIClient.Instance.Get<ConversationListResponse>(
                $"/api/conversations?id={userId}",
                res =>
                {
                    Conversations = new List<Conversation>(res.conversations ?? Array.Empty<Conversation>());
                    OnConversationsLoaded?.Invoke(Conversations);
                },
                err => OnError?.Invoke(err)
            ));
        }

        // ── Create ────────────────────────────────────────────────────
        public void CreateConversation(string userId,
            Action<Conversation> onSuccess = null)
        {
            var body = new CreateConversationRequest { id = userId };
            StartCoroutine(APIClient.Instance.Post<Conversation>(
                "/api/conversations", body,
                conv =>
                {
                    Conversations.Insert(0, conv);
                    ActiveConversation = conv;
                    OnConversationCreated?.Invoke(conv);
                    onSuccess?.Invoke(conv);
                },
                err => OnError?.Invoke(err)
            ));
        }

        // ── Rename ────────────────────────────────────────────────────
        public void RenameConversation(string convId, string newTitle,
            Action onSuccess = null)
        {
            var body = new RenameConversationRequest { title = newTitle };
            StartCoroutine(APIClient.Instance.Patch<Conversation>(
                $"/api/conversations/{convId}", body,
                conv =>
                {
                    var idx = Conversations.FindIndex(c => c.id_conversation == convId);
                    if (idx >= 0) Conversations[idx].title = newTitle;
                    onSuccess?.Invoke();
                },
                err => OnError?.Invoke(err)
            ));
        }

        // ── Delete ────────────────────────────────────────────────────
        public void DeleteConversation(string convId, Action onSuccess = null)
        {
            StartCoroutine(APIClient.Instance.Delete<DeleteResponse>(
                $"/api/conversations/{convId}",
                _ =>
                {
                    Conversations.RemoveAll(c => c.id_conversation == convId);
                    if (ActiveConversation?.id_conversation == convId)
                        ActiveConversation = null;
                    onSuccess?.Invoke();
                },
                err => OnError?.Invoke(err)
            ));
        }

        public void SetActiveConversation(Conversation conv)
            => ActiveConversation = conv;
    }
}