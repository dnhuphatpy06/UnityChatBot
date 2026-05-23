//using UnityEngine;
//using TMPro;

//namespace ChatApp.UI
//{
//    public class MessageItem : MonoBehaviour
//    {
//        [SerializeField] private TMP_Text contentText;

//        public void Setup(string content)
//        {
//            contentText.text = content;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatApp.UI
{
    public class MessageItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private GameObject btnRecommend;
        [SerializeField] private TMP_Text recommendLabel;
        [SerializeField] private Button recommendButton;

        public void Setup(string content)
        {
            contentText.text = content;
            if (btnRecommend) btnRecommend.SetActive(false);
        }

        public void SetRecommend(string labelText,
                                  UnityEngine.Events.UnityAction onClick)
        {
            if (btnRecommend == null) return;
            btnRecommend.SetActive(true);
            if (recommendLabel) recommendLabel.text = labelText;
            if (recommendButton)
            {
                recommendButton.onClick.RemoveAllListeners();
                recommendButton.onClick.AddListener(onClick);
            }
        }
    }
}