using System;
using System.Runtime.Serialization;

namespace OAuthApp
{
    // This is a sample. Fille information (email, etc.) can depend on scopes
    [DataContract]
    public class OAuthToken
    {
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "token_type")]
        public string TokenType { get; set; }

        [DataMember(Name = "expires_in")]
        public int ExpiresIn { get; set; }

        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string PhoneNumber { get; set; }

        [DataMember]
        public string[] Scopes { get; set; }

        // not from google's response, but we store this
        public DateTime ExpirationDate { get; set; }
    }
}