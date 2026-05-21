using UnityEngine;
using TMPro;

namespace ChatApp.UI
{
    public class MessageItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text contentText;

        public void Setup(string content)
        {
            contentText.text = content;
        }
    }
}