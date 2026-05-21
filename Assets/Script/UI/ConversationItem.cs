using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Models;

namespace ChatApp.UI
{
    public class ConversationItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text  titleText;
        [SerializeField] private Button    selectButton;
        [SerializeField] private Button    renameButton;
        [SerializeField] private Button    deleteButton;
        [SerializeField] private TMP_InputField renameInputField;
        [SerializeField] private GameObject     renamePanel; 
        private Canvas rootCanvas; //
        private Action                    _onSelect;
        private Action<string>            _onRename;
        private Action                    _onDelete;
        void Awake()
        {
            // Tìm Canvas gốc
            rootCanvas = FindObjectOfType<Canvas>();

            // Reparent RenamePanel lên Canvas, thoát khỏi Scroll View Mask
            renamePanel.transform.SetParent(rootCanvas.transform, false);
            renamePanel.SetActive(false);
        }

        public void Setup(Conversation conv,
            Action onSelect, Action<string> onRename, Action onDelete)
        {
            titleText.text = conv.title ?? "Cuộc trò chuyện mới";
            _onSelect = onSelect;
            _onRename = onRename;
            _onDelete = onDelete;

            selectButton.onClick.AddListener(() => _onSelect?.Invoke());
            deleteButton.onClick.AddListener(() => _onDelete?.Invoke());
            renameButton.onClick.AddListener(ShowRenamePanel);

            renamePanel.SetActive(false);
        }

        private void ShowRenamePanel()
        {
            Debug.Log("SHOW RENAME");

            Debug.Log(renamePanel);
            renamePanel.SetActive(true);
            renameInputField.text = titleText.text;
            renameInputField.Select();
        }

        public void ConfirmRename()
        {
            string newTitle = renameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newTitle))
                _onRename?.Invoke(newTitle);
            renamePanel.SetActive(false);
        }

        public void CancelRename() => renamePanel.SetActive(false);

        public void SetTitle(string title) => titleText.text = title;

        public void OnRenameClick()
        {
            renamePanel.SetActive(true);

            // Convert vị trí item sang tọa độ Local của Canvas
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            RectTransform itemRect = GetComponent<RectTransform>();
            RectTransform panelRect = renamePanel.GetComponent<RectTransform>();

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, itemRect.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, null, out Vector2 localPoint
            );

            panelRect.anchoredPosition = localPoint;
        }

        void OnDestroy()
        {
            if (renamePanel != null)
                Destroy(renamePanel);
        }
    }
}