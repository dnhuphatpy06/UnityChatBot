using System;

namespace ChatApp.Models
{
    [Serializable]
    public class Conversation
    {
        public string id_conversation;
        public string title;
        public string user_id;
    }

    [Serializable]
    public class CreateConversationRequest
    {
        public string id; // user id
    }

    [Serializable]
    public class RenameConversationRequest
    {
        public string title;
    }

    [Serializable]
    public class ConversationListResponse
    {
        public Conversation[] conversations;
    }
}