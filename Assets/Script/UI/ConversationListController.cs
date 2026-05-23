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
    public class ConversationListController : MonoBehaviour
    {
        [SerializeField] private Transform listContent;
        [SerializeField] private GameObject convItemPrefab;
        [SerializeField] private Button newChatButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private TMP_Text userLabel;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Network Error Popup")]
        [SerializeField] private GameObject networkErrorOverlay;
        [SerializeField] private Button btnReload;
        [SerializeField] private Button btnExit;

        private string _currentUserId;
        private Action _pendingAction;

        private void Start()
        {
            var user = AuthManager.Instance.CurrentUser;
            if (user == null) { UIManager.Instance.GoToLogin(); return; }

            _currentUserId = user.id;
            userLabel.text = user.IsGuest ? "Khách" : user.username;

            newChatButton.onClick.AddListener(OnNewChat);
            logoutButton.onClick.AddListener(OnLogout);

            btnReload.onClick.AddListener(OnReloadClicked);
            btnExit.onClick.AddListener(OnExitClicked);

            networkErrorOverlay.SetActive(false);

            ConversationManager.Instance.OnConversationsLoaded += RenderList;
            ConversationManager.Instance.OnError += ShowError;
            ConversationManager.Instance.OnNetworkError += ShowNetworkError;

            LoadConversations();
        }

        private void OnDestroy()
        {
            ConversationManager.Instance.OnConversationsLoaded -= RenderList;
            ConversationManager.Instance.OnError -= ShowError;
            ConversationManager.Instance.OnNetworkError -= ShowNetworkError;
        }

        private void LoadConversations()
        {
            _pendingAction = LoadConversations;
            SetLoading(true);
            ConversationManager.Instance.LoadConversations(_currentUserId);
        }

        private void RenderList(List<Conversation> list)
        {
            _pendingAction = null;
            SetLoading(false);
            foreach (Transform child in listContent) Destroy(child.gameObject);
            foreach (var conv in list)
            {
                var go = Instantiate(convItemPrefab, listContent);
                var item = go.GetComponent<ConversationItem>();
                item.Setup(conv,
                    onSelect: () =>
                    {
                        ConversationManager.Instance.SetActiveConversation(conv);
                        UIManager.Instance.GoToChat();
                    },
                    onRename: newTitle =>
                        ConversationManager.Instance.RenameConversation(
                            conv.id_conversation, newTitle,
                            () => item.SetTitle(newTitle)
                        ),
                    onDelete: () =>
                        ConversationManager.Instance.DeleteConversation(
                            conv.id_conversation,
                            () => Destroy(go)
                        )
                );
            }
        }

        private void OnNewChat()
        {
            Debug.Log("[ConvList] OnNewChat clicked");
            ConversationManager.Instance.CreateConversation(_currentUserId,
                conv =>
                {
                    _pendingAction = null;
                    UIManager.Instance.GoToChat();
                }
            );
        }

        private void OnLogout()
        {
            AuthManager.Instance.Logout(() => UIManager.Instance.GoToLogin());
        }

        // ── Error ─────────────────────────────────────────────────────
        private void ShowError(string err)
        {
            _pendingAction = null;
            SetLoading(false);
            errorText.text = err;
            errorText.gameObject.SetActive(true);
        }

        private void ShowNetworkError(string err)
        {
            SetLoading(false);
            if (_pendingAction == null)
                _pendingAction = LoadConversations;
            networkErrorOverlay.SetActive(true);
        }

        private void OnReloadClicked() => StartCoroutine(RetryConnection());

        private IEnumerator RetryConnection()
        {
            Debug.Log("<b>[Retry]</b> BẮT ĐẦU reload...");
            networkErrorOverlay.SetActive(false);
            SetLoading(true);

            // Lấy lại link Firebase trước
            Debug.Log("<b>[Retry]</b> Đang chờ APIClient.Instance.Retry() hoàn thành...");
            yield return APIClient.Instance.Retry();
            Debug.Log("<b>[Retry]</b> APIClient.Instance.Retry() đã xong.");

            // Chờ thêm nếu chưa xong
            float elapsed = 0f;
            const float timeout = 10f;
            while (elapsed < timeout)
            {
                if (APIClient.Instance.IsReady || APIClient.Instance.LoadError != null)
                {
                    Debug.Log($"<b>[Retry]</b> Thoát vòng lặp chờ sớm tại giây {elapsed:F2}. IsReady: {APIClient.Instance.IsReady} | LoadError: {APIClient.Instance.LoadError}");
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetLoading(false);
            bool timedOut = !APIClient.Instance.IsReady && APIClient.Instance.LoadError == null;

            // In ra tổng hợp trạng thái trước khi quyết định rẽ nhánh
            Debug.Log($"<b>[Retry]</b> TRẠNG THÁI CUỐI: timedOut={timedOut} | IsReady={APIClient.Instance.IsReady} | LoadError={(APIClient.Instance.LoadError == null ? "NULL" : APIClient.Instance.LoadError.ToString())}");

            if (timedOut || APIClient.Instance.LoadError != null)
            {
                // Vẫn lỗi → hiện lại popup
                Debug.LogWarning("<b>[Retry]</b> -> THẤT BẠI. Đang bật lại popup lỗi.");
                networkErrorOverlay.SetActive(true);
            }
            else
            {
                // Có mạng → replay action gốc
                Debug.Log("<b>[Retry]</b> -> THÀNH CÔNG (Có mạng). Kiểm tra hành động tiếp theo...");

                if (_pendingAction != null)
                {
                    Debug.Log("<b>[Retry]</b> Đang chạy _pendingAction.Invoke() vì _pendingAction KHÁC null. (LoadConversations BỊ BỎ QUA)");
                    _pendingAction.Invoke();
                }
                else
                {
                    Debug.Log("<b>[Retry]</b> _pendingAction là null. Bắt đầu gọi LoadConversations()!");
                    LoadConversations();
                }
            }
        }

        private void OnExitClicked()
        {
            Debug.Log("[ConvList] OnExitClicked clicked");
            _pendingAction = null;
            networkErrorOverlay.SetActive(false);
            UIManager.Instance.GoToLogin();
        }

        private void SetLoading(bool on) => loadingIndicator.SetActive(on);
    }
}