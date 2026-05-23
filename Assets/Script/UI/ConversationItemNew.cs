using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Models;

namespace ChatApp.UI
{
    public class ConversationItemNew : MonoBehaviour
    {
        [Header("Normal Mode")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button deleteButton;

        [Header("Rename Mode")]
        [SerializeField] private TMP_InputField renameInputField;
        [SerializeField] private Button confirmRenameButton;
        [SerializeField] private Button cancelRenameButton;

        [Header("Title (trên Btn_Select)")]
        [SerializeField] private TMP_Text titleText;

        private Action _onSelect;
        private Action<string> _onRename;
        private Action _onDelete;
        private string _convId;

        // ───────────────────────────────────────────────
        void Awake()
        {
            // Gắn sự kiện cho các nút
            selectButton.onClick.AddListener(OnSelectClicked);
            renameButton.onClick.AddListener(OnRenameClicked);
            deleteButton.onClick.AddListener(OnDeleteClicked);
            confirmRenameButton.onClick.AddListener(OnConfirmRename);
            cancelRenameButton.onClick.AddListener(OnCancelRename);

            // Trạng thái khởi đầu: Normal Mode
            SetNormalMode();
        }

        // ───────────────────────────────────────────────
        /// <summary>Gọi từ ConversationListManager khi tạo item.</summary>
        public void Setup(Conversation conv,
            Action onSelect, Action<string> onRename, Action onDelete)
        {
            _convId = conv.id_conversation;
            titleText.text = conv.title ?? "Cuộc trò chuyện mới";
            _onSelect = onSelect;
            _onRename = onRename;
            _onDelete = onDelete;

            SetNormalMode();
        }

        // ───────────────────────────────────────────────
        #region Mode Switching

        /// <summary>Hiện Select / Rename / Delete — ẩn Input / Confirm / Cancel.</summary>
        private void SetNormalMode()
        {
            selectButton.gameObject.SetActive(true);
            renameButton.gameObject.SetActive(true);
            deleteButton.gameObject.SetActive(true);

            renameInputField.gameObject.SetActive(false);
            confirmRenameButton.gameObject.SetActive(false);
            cancelRenameButton.gameObject.SetActive(false);
        }

        /// <summary>Ẩn Select / Rename / Delete — hiện Input / Confirm / Cancel.</summary>
        private void SetRenameMode()
        {
            selectButton.gameObject.SetActive(false);
            renameButton.gameObject.SetActive(false);
            deleteButton.gameObject.SetActive(false);

            renameInputField.gameObject.SetActive(true);
            confirmRenameButton.gameObject.SetActive(true);
            cancelRenameButton.gameObject.SetActive(true);

            // Điền sẵn tên hiện tại và focus
            renameInputField.text = titleText.text;
            renameInputField.Select();
            renameInputField.ActivateInputField();
        }

        #endregion

        // ───────────────────────────────────────────────
        #region Button Handlers

        private void OnSelectClicked() => _onSelect?.Invoke();

        private void OnDeleteClicked() => _onDelete?.Invoke();

        private void OnRenameClicked() => SetRenameMode();

        private void OnConfirmRename()
        {
            string newTitle = renameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newTitle))
            {
                titleText.text = newTitle;
                _onRename?.Invoke(newTitle);
            }
            SetNormalMode();
        }

        private void OnCancelRename() => SetNormalMode();

        #endregion

        // ───────────────────────────────────────────────
        /// <summary>Cập nhật tiêu đề từ bên ngoài.</summary>
        public void SetTitle(string title) => titleText.text = title;
    }
}