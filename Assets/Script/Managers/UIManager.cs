using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChatApp.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private const string SCENE_LOGIN = "LoginScene";
        private const string SCENE_CONV  = "ConversationListScene";
        private const string SCENE_CHAT  = "ChatScene";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void GoToLogin()          => SceneManager.LoadScene(SCENE_LOGIN);
        public void GoToConversations()  => SceneManager.LoadScene(SCENE_CONV);
        public void GoToChat()           => SceneManager.LoadScene(SCENE_CHAT);
    }
}