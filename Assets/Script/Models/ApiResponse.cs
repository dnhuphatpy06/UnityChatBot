using System;

namespace ChatApp.Models
{
    [Serializable]
    public class ApiError
    {
        public string error;
        public string message;
    }

    [Serializable]
    public class DeleteResponse
    {
        public string status;
    }
}