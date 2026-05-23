//using System;
//using System.Collections.Generic;
//namespace ChatApp.Models
//{
//    [Serializable]
//    public class Message
//    {
//        public string id;
//        public string conversation_id;
//        public string role; // "user" hoặc "assistant"
//        public string content;
//        public string created_at;
//        public Dictionary<string, string> metadata;
//    }

//    [Serializable]
//    public class SendMessageRequest
//    {
//        public string content;
//    }

//    [Serializable]
//    public class SendMessageResponse
//    {
//        public Message user_message;
//        public Message assistant_message;
//    }

//    [Serializable]
//    public class MessageListResponse
//    {
//        public Message[] messages;
//    }
//}

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatApp.Models
{
    public class Message
    {
        public string id;
        public string conversation_id;
        public string role;
        public string content;
        public string created_at;

        [JsonProperty("metadata")]
        public JToken metadata_raw { get; set; }

        public string GetMeta(string key)
        {
            if (metadata_raw == null) return null;
            try
            {
                if (metadata_raw.Type == JTokenType.Object)
                {
                    var val = metadata_raw[key];
                    if (val == null) return null;
                    if (val.Type == JTokenType.Array)
                        return val[0]?.ToString();
                    return val.ToString();
                }
            }
            catch { }
            return null;
        }
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