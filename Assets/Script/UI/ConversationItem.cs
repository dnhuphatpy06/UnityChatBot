//using System;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using ChatApp.Models;

//namespace ChatApp.UI
//{
//    public class ConversationItem : MonoBehaviour
//    {
//        [SerializeField] private TMP_Text  titleText;
//        [SerializeField] private Button    selectButton;
//        [SerializeField] private Button    renameButton;
//        [SerializeField] private Button    deleteButton;
//        [SerializeField] private TMP_InputField renameInputField;
//        [SerializeField] private GameObject     renamePanel; 
//        private Canvas rootCanvas; //
//        private Action                    _onSelect;
//        private Action<string>            _onRename;
//        private Action                    _onDelete;
//        void Awake()
//        {
//            // Tìm Canvas gốc
//            rootCanvas = FindObjectOfType<Canvas>();

//            // Reparent RenamePanel lên Canvas, thoát khỏi Scroll View Mask
//            renamePanel.transform.SetParent(rootCanvas.transform, false);
//            renamePanel.SetActive(false);
//        }

//        public void Setup(Conversation conv,
//            Action onSelect, Action<string> onRename, Action onDelete)
//        {
//            titleText.text = conv.title ?? "Cuộc trò chuyện mới";
//            _onSelect = onSelect;
//            _onRename = onRename;
//            _onDelete = onDelete;

//            selectButton.onClick.AddListener(() => _onSelect?.Invoke());
//            deleteButton.onClick.AddListener(() => _onDelete?.Invoke());
//            renameButton.onClick.AddListener(ShowRenamePanel);

//            renamePanel.SetActive(false);
//        }

//        private void ShowRenamePanel()
//        {
//            Debug.Log("SHOW RENAME");

//            Debug.Log(renamePanel);
//            renamePanel.SetActive(true);
//            renameInputField.text = titleText.text;
//            renameInputField.Select();
//        }

//        public void ConfirmRename()
//        {
//            string newTitle = renameInputField.text.Trim();
//            if (!string.IsNullOrEmpty(newTitle))
//                _onRename?.Invoke(newTitle);
//            renamePanel.SetActive(false);
//        }

//        public void CancelRename() => renamePanel.SetActive(false);

//        public void SetTitle(string title) => titleText.text = title;

//        public void OnRenameClick()
//        {
//            renamePanel.SetActive(true);

//            // Convert vị trí item sang tọa độ Local của Canvas
//            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//            RectTransform itemRect = GetComponent<RectTransform>();
//            RectTransform panelRect = renamePanel.GetComponent<RectTransform>();

//            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, itemRect.position);

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                canvasRect, screenPoint, null, out Vector2 localPoint
//            );

//            panelRect.anchoredPosition = localPoint;
//        }

//        void OnDestroy()
//        {
//            if (renamePanel != null)
//                Destroy(renamePanel);
//        }
//    }
//}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatApp.Models;

namespace ChatApp.UI
{
    public class ConversationItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_InputField renameInputField;
        [SerializeField] private GameObject renamePanel;

        private Action _onSelect;
        private Action<string> _onRename;
        private Action _onDelete;

        void Awake()
        {
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
            selectButton.gameObject.SetActive(false);
            renamePanel.SetActive(true);
            AlignRenamePanelElements();
            renameInputField.text = titleText.text;
            renameInputField.Select();
        }

        /// <summary>
        /// Copy vị trí và kích thước của 3 button gốc
        /// sang 3 element tương ứng trong RenamePanel.
        /// </summary>
        private void AlignRenamePanelElements()
        {
            // RenameInputField  ← Btn_Select
            CopyRect(
                selectButton.GetComponent<RectTransform>(),
                renameInputField.GetComponent<RectTransform>()
            );

            // Btn_ConfirmRename ← Btn_Rename
            RectTransform confirmRect = renamePanel
                .transform.Find("Btn_ConfirmRename")
                .GetComponent<RectTransform>();
            CopyRect(
                renameButton.GetComponent<RectTransform>(),
                confirmRect
            );

            // Btn_CancelRename  ← Btn_Delete
            RectTransform cancelRect = renamePanel
                .transform.Find("Btn_CancelRename")
                .GetComponent<RectTransform>();
            CopyRect(
                deleteButton.GetComponent<RectTransform>(),
                cancelRect
            );
        }

        /// <summary>
        /// Sao chép anchoredPosition, sizeDelta, anchorMin/Max, pivot từ src sang dst.
        /// Hoạt động đúng khi cả hai nằm dưới cùng một parent (RenamePanel).
        /// Nếu parent khác nhau, dùng phiên bản WorldToScreenPoint bên dưới.
        /// </summary>
        private static void CopyRect(RectTransform src, RectTransform dst)
        {
            // Trường hợp cùng parent: sao chép trực tiếp
            if (src.parent == dst.parent)
            {
                dst.anchorMin = src.anchorMin;
                dst.anchorMax = src.anchorMax;
                dst.pivot = src.pivot;
                dst.anchoredPosition = src.anchoredPosition;
                dst.sizeDelta = src.sizeDelta;
                return;
            }

            // Trường hợp khác parent: chuyển đổi qua world-space
            // Lấy 4 góc của src trong world space
            Vector3[] corners = new Vector3[4];
            src.GetWorldCorners(corners);
            // corners: [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right

            // Tính kích thước thực trong world space
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);

            // Tính center world
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            // Đặt dst về parent space
            RectTransform dstParent = dst.parent as RectTransform;
            Vector2 localCenter;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dstParent,
                RectTransformUtility.WorldToScreenPoint(null, worldCenter),
                null,
                out localCenter
            );

            dst.anchorMin = new Vector2(0.5f, 0.5f);
            dst.anchorMax = new Vector2(0.5f, 0.5f);
            dst.pivot = new Vector2(0.5f, 0.5f);
            dst.anchoredPosition = localCenter;

            // Tính sizeDelta theo tỉ lệ scale của dst parent
            float scaleX = dstParent != null ? dstParent.lossyScale.x : 1f;
            float scaleY = dstParent != null ? dstParent.lossyScale.y : 1f;
            dst.sizeDelta = new Vector2(
                scaleX > 0 ? worldWidth / scaleX : worldWidth,
                scaleY > 0 ? worldHeight / scaleY : worldHeight
            );
        }

        public void ConfirmRename()
        {
            string newTitle = renameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newTitle))
            {
                _onRename?.Invoke(newTitle);
                titleText.text = newTitle;        
            }
            renamePanel.SetActive(false);
            selectButton.gameObject.SetActive(true);
        }

        public void CancelRename()
        {
            renamePanel.SetActive(false);
            selectButton.gameObject.SetActive(true); 
        }

        public void SetTitle(string title) => titleText.text = title;

        void OnDestroy()
        {
            if (renamePanel != null)
                Destroy(renamePanel);
        }
    }
}