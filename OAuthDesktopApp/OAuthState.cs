using System;
using System.ComponentModel;

namespace OAuthApp
{
    // state model
    public class OAuthState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OAuthToken _token;
        public OAuthToken Token
        {
            get => _token;
            set
            {
                if (_token == value)
                    return;

                _token = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Token)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSigned)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotSigned)));
            }
        }

        public bool IsSigned => Token != null && Token.ExpirationDate > DateTime.Now;
        public bool IsNotSigned => !IsSigned;
    }
}