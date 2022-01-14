// Copyright 2016 Google Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace OAuthApp
{
    // largely inspired from
    // https://github.com/googlesamples/oauth-apps-for-windows
    public sealed class OAuthRequest
    {
        private const string ClientId = "foobarfoobarfoobar";
        private const string AuthorizationEndpoint = "https://{pool-name}.auth.{region}.amazoncognito.com/oauth2/authorize";
        private const string TokenEndpoint = "https://{pool-name}.auth.{region}.amazoncognito.com/oauth2/token";
        private const string UserInfoEndpoint = "https://{pool-name}.auth.{region}.amazoncognito.com/oauth2/userInfo";
        private const string RevocationEndpoint = "https://{pool-name}.auth.{region}.amazoncognito.com/oauth2/revoke";

        private OAuthRequest()
        {
        }

        public string AuthorizationRequestUri { get; private set; }
        public string State { get; private set; }
        public string RedirectUri { get; private set; }
        public string CodeVerifier { get; private set; }
        public string[] Scopes { get; private set; }

        /// <summary>
        /// Build PKCE parameters(CodeVerifier, codeChallenge) and State in an authoirzation endpoint
        /// </summary>
        public static OAuthRequest BuildRequest(params string[] scopes)
        {
            var request = new OAuthRequest
            {
                CodeVerifier = RandomDataBase64Url(32),
                Scopes = scopes
            };

            string codeChallenge = Base64UrlEncodeNoPadding(Sha256(request.CodeVerifier));
            const string codeChallengeMethod = "S256";

            request.RedirectUri = string.Format("{0}://{1}/oauth2/callback", App.UriScheme, App.UriHost);
            request.State = RandomDataBase64Url(32);
            request.AuthorizationRequestUri = string.Format("{0}?response_type=code&scope{6}&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                AuthorizationEndpoint,
                Uri.EscapeDataString(request.RedirectUri),
                ClientId,
                request.State,
                codeChallenge,
                codeChallengeMethod,
                BuildScopes(scopes));

            return request;
        }

        /// <summary>
        /// Exchange authorization code for refresh and access tokens
        /// </summary>
        public async Task<OAuthToken> ExchangeCodeForAccessTokenAsync(string code)
        {
            output("Exchange code for access token...");

            if (code == null)
                throw new ArgumentNullException(nameof(code));

            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&scope=&grant_type=authorization_code",
                code,
                Uri.EscapeDataString(RedirectUri),
                ClientId,
                CodeVerifier
                );

            var res = await RequestAsync(TokenEndpoint, tokenRequestBody);
            var token = Deserialize<OAuthToken>(res);
            token.ExpirationDate = DateTime.Now + new TimeSpan(0, 0, token.ExpiresIn);
            var user = GetUserInfo(token.AccessToken);

            token.Id = user.Id;
            token.Username = user.Username;
            token.PhoneNumber = user.PhoneNumber;
            token.Scopes = Scopes;
            return token;
        }

        /// <summary>
        /// Retrieve info of current user
        /// </summary>
        private static UserInfo GetUserInfo(string accessToken)
        {
            output("Making API Call to Userinfo...");

            var request = (HttpWebRequest)WebRequest.Create(UserInfoEndpoint);
            request.Method = "GET";
            request.Headers.Add(string.Format("Authorization: Bearer {0}", accessToken));
            var response = request.GetResponse();
            using (var stream = response.GetResponseStream())
            {
                // 把 response 記錄到 console
                var sr = new StreamReader(stream);
                var res = sr.ReadToEnd();
                output(res);

                return Deserialize<UserInfo>(res);
            }
        }

        /// <summary>
        /// Revoke refresh token and any derived access token
        /// </summary>
        public async void RevokeAsync(string refreshToken)
        {
            output("Revoke token...");

            if (refreshToken == null)
                throw new ArgumentNullException(nameof(refreshToken));

            string requestBody = string.Format("token={0}&client_id={1}",
                refreshToken,
                ClientId
                );

            var res = await RequestAsync(RevocationEndpoint, requestBody);
            if (res == null || res == "")
            {
                output("Revoke success...");
            }
            else
            {
                output("Revoke failed!");
                output(res);
            }
        }

        /// <summary>
        /// Refreshing an access token
        /// </summary>
        public async Task<OAuthToken> RefreshAsync(OAuthToken oldToken)
        {
            if (oldToken == null)
                throw new ArgumentNullException(nameof(oldToken));

            string tokenRequestBody = string.Format("refresh_token={0}&client_id={1}&grant_type=refresh_token",
                oldToken.RefreshToken,
                ClientId
                );

            var res = await RequestAsync(TokenEndpoint, tokenRequestBody);
            var token = Deserialize<OAuthToken>(res);
            token.ExpirationDate = DateTime.Now + new TimeSpan(0, 0, token.ExpiresIn);
            var user = GetUserInfo(token.AccessToken);

            token.Id = user.Id;
            token.Username = user.Username;
            token.PhoneNumber = user.PhoneNumber;
            token.Scopes = oldToken.Scopes;
            return token;;
        }

        /// <summary>
        /// Send post request
        /// </summary>
        private static async Task<string> RequestAsync(string endpoint, string body)
        {
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] bytes = Encoding.ASCII.GetBytes(body);
            using (var requestStream = request.GetRequestStream())
            {
                await requestStream.WriteAsync(bytes, 0, bytes.Length);
                requestStream.Close();
            }

            try
            {
                var response = await request.GetResponseAsync();
                using (var responseStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        output("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = reader.ReadToEnd();
                            output(responseText);
                        }
                    }
                }
                return null;
            }
        }

        private static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            return Deserialize<T>(Encoding.UTF8.GetBytes(json));
        }

        private static T Deserialize<T>(byte[] json)
        {
            if (json == null || json.Length == 0)
                return default(T);

            using (var ms = new MemoryStream(json))
            {
                return Deserialize<T>(ms);
            }
        }

        private static T Deserialize<T>(Stream json)
        {
            if (json == null)
                return default(T);

            var ser = CreateSerializer(typeof(T));
            return (T)ser.ReadObject(json);
        }

        private static DataContractJsonSerializer CreateSerializer(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var settings = new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-dd'T'HH:mm:ss.fffK")
            };
            return new DataContractJsonSerializer(type, settings);
        }

        private static string RandomDataBase64Url(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Base64UrlEncodeNoPadding(bytes);
            }
        }

        private static byte[] Sha256(string text)
        {
            using (var sha256 = new SHA256Managed())
            {
                return sha256.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
        }

        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            string b64 = Convert.ToBase64String(buffer);
            // converts base64 to base64url.
            b64 = b64.Replace('+', '-');
            b64 = b64.Replace('/', '_');
            // strips padding.
            b64 = b64.Replace("=", "");
            return b64;
        }

        /// <summary>
        /// Build scopes list into URL format
        /// </summary>
        private static string BuildScopes(string[] scopes)
        {
            var encodedScopes = Array.ConvertAll(scopes, s => Uri.EscapeDataString(s));
            return string.Join("%20", scopes);
        }

        /// <summary>
        /// On-screen log
        /// </summary>
        /// <param name="output">string to be appended</param>
        public static void output(string output)
        {
            // https://stackoverflow.com/a/34554362
            ((MainWindow)Application.Current.MainWindow).output(output);
        }

        [DataContract]
        private class UserInfo
        {
            [DataMember(Name = "username")]
            public string Username { get; set; }

            [DataMember(Name = "phone_number")]
            public string PhoneNumber { get; set; }

            [DataMember(Name = "sub")]
            public string Id { get; set; }
        }
    }

}