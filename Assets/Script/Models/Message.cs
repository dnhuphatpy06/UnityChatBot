using System;

namespace ChatApp.Models
{
    [Serializable]
    public class Message
    {
        public string id;
        public string conversation_id;
        public string role; // "user" hoặc "assistant"
        public string content;
        public string created_at;
    }

    [Serializable]
    public class SendMessageRequest
    {
        public string content;
    }

    [Serializable]
    public class SendMessageResponse
    {
        public Message user_message;
        public Message assistant_message;
    }

    [Serializable]
    public class MessageListResponse
    {
        public Message[] messages;
    }
}