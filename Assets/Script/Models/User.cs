using System;

namespace ChatApp.Models
{
    [Serializable]
    public class User
    {
        public string id;
        public string username; // null nếu là guest
        public bool IsGuest => string.IsNullOrEmpty(username);
    }

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
        public string full_name;
        public string student_id;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }
}