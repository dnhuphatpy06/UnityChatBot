using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Managers;
using ChatApp.Models;

namespace ChatApp.UI
{
    public class ConversationListController : MonoBehaviour
    {
        [SerializeField] private Transform      listContent;
        [SerializeField] private GameObject     convItemPrefab;
        [SerializeField] private Button         newChatButton;
        [SerializeField] private Button         logoutButton;
        [SerializeField] private TMP_Text       userLabel;
        [SerializeField] private TMP_Text       errorText;
        [SerializeField] private GameObject     loadingIndicator;

        private void Start()
        {

            var user = AuthManager.Instance.CurrentUser;
            if (user == null) { UIManager.Instance.GoToLogin(); return; }

            userLabel.text = user.IsGuest ? "Khách" : user.username;
            newChatButton.onClick.AddListener(OnNewChat);
            logoutButton.onClick.AddListener(OnLogout);

            ConversationManager.Instance.OnConversationsLoaded += RenderList;
            ConversationManager.Instance.OnError += ShowError;

            SetLoading(true);
            ConversationManager.Instance.LoadConversations(user.id);
        }

        private void OnDestroy()
        {
            ConversationManager.Instance.OnConversationsLoaded -= RenderList;
            ConversationManager.Instance.OnError -= ShowError;
        }

        private void RenderList(List<Conversation> list)
        {
            //foreach (var conv in list)
            //{
            //    Debug.Log("RENDER ID = " + conv.id_conversation);
            //};
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
                    onDelete: () => { 
                        //Debug.Log("DELETE ID = " + conv.id_conversation);
                        ConversationManager.Instance.DeleteConversation(
                            conv.id_conversation,
                            () => Destroy(go)
                        );
            }
                );
            }
        }

        private void OnNewChat()
        {
            var userId = AuthManager.Instance.CurrentUser.id;
            ConversationManager.Instance.CreateConversation(userId,
                conv => UIManager.Instance.GoToChat());
        }

        private void OnLogout()
        {
            AuthManager.Instance.Logout(() => UIManager.Instance.GoToLogin());
        }

        private void ShowError(string err)
        {
            SetLoading(false);
            errorText.text = err;
            errorText.gameObject.SetActive(true);
        }

        private void SetLoading(bool on) => loadingIndicator.SetActive(on);
    }
}