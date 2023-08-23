using Newtonsoft.Json;
using Sturfee.XRCS.Config;
using SturfeeVPS.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

using System.Text;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;

namespace Sturfee.Auth
{
    public class CognitoAuthContext
    {
        public AuthenticationProvider Provider;
        public string Region;
        public string PoolId;
        public string ClientId;
        public CognitoAuthTokenType Token;
        public bool IsBearer;

        public string Username;
        public string IdToken;
        public string AccessToken;
        public string RefreshToken;
        public DateTime IssuedTime;
        public DateTime ExpirationTime;
    }

    public class NewSessionRequest
    {
        public string authenticationCode { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public class NewSessionResponse
    {
        public string sturfee_sid { get; set; }
        public string error_message { get; set; }
    }

    public class AuthManager : SimpleSingleton<AuthManager>
    {
        public string SessionId => _sessionId;
        private string _sessionId = string.Empty;

        public string AuthToken => _token;
        private string _token = string.Empty;

        public XrcsUserData CurrentUser => _currentUser;
        private XrcsUserData _currentUser = null;

        public AuthenticationProviderConfig AuthProvider => _authProvider;
        private AuthenticationProviderConfig _authProvider = null;

        public AuthenticationProvider AuthProviderType => _authProviderType;
        private AuthenticationProvider _authProviderType = AuthenticationProvider.Sturfee;

        private string _idToken = string.Empty;

        // Player Prefs
        private string _authSessionIdKey = "sessionid";
        private string _authTokenKey = "authtoken";
        private string _playerInfoKey = "PlayerInfo";
        private string _authContextKey = "authContext";

        public static string LocalPath => Path.Combine("Assets", "Resources", "Sturfee", "Auth");

        private void Awake()
        {
            _sessionId = PlayerPrefs.GetString(_authSessionIdKey);
            _token = PlayerPrefs.GetString(_authTokenKey);

            MyLogger.Log($"Found Auth: \ntoken={_token}\nsessionid={_sessionId}");
            GetSettings();
        }

        public void SetAuthSessionId(string sessionId)
        {
            MyLogger.Log($"Saving Session ID = {sessionId}");
            _sessionId = sessionId;
            // PlayerPrefs.SetString(_authSessionIdKey, sessionId);
            Dispatcher.RunOnMainThread(() => PlayerPrefs.SetString(_authSessionIdKey, sessionId));
        }

        public void SetAuthToken(string token)
        {
            _token = token;
            // PlayerPrefs.SetString(_authTokenKey, token);
            Dispatcher.RunOnMainThread(() => PlayerPrefs.SetString(_authTokenKey, token));
        }

        public async Task<bool> StartLoginFlow(string username, string password, string code)
        {
            GetSettings();

            if (_authProvider && _authProvider.Provider != AuthenticationProvider.Sturfee)
            {
                var success = await StartLoginFlowCognito(username, password, _authProvider);
                return success;
            }
            else
            {
                var success = await StartLoginFlowSturfee(username, password, code);
                return success;
            }
        }

        public async Task<bool> StartLoginFlowSturfee(string username, string password, string code)
        {
            // use code OR user/password to start session
            Debug.Log("[SturfeeAuthManager] :: Authenticating user...");
            MyLogger.Log($"Starting new AUTH SESSION => {XrConstants.AUTH_API}/startNewSession");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{XrConstants.AUTH_API}/startNewSession");
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(new NewSessionRequest
                {
                    authenticationCode = string.IsNullOrEmpty(code) ? null : code,
                    username = string.IsNullOrEmpty(username) ? null : username,
                    password = string.IsNullOrEmpty(password) ? null : password
                });
                Debug.Log(json);
                streamWriter.Write(json);
                streamWriter.Flush();
            }
            Debug.Log(request.RequestUri.ToString());
            NewSessionResponse sessionResponse;
            try
            {
                MyLogger.Log($"   Waiting for AUTH SESSION response...");
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string jsonResponse = reader.ReadToEnd();
                        MyLogger.Log($"AUTH SESSION from API:\n{jsonResponse}");

                        if (!string.IsNullOrEmpty(jsonResponse))
                        {
                            sessionResponse = JsonConvert.DeserializeObject<NewSessionResponse>(jsonResponse);
                            MyLogger.Log($"SessionId = {sessionResponse.sturfee_sid}");

                            if (!string.IsNullOrEmpty(sessionResponse.sturfee_sid))
                            {
                                SetAuthSessionId(sessionResponse.sturfee_sid);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.LogError(e);
                throw;
            }

            return false;
        }

        public async Task<bool> TryLogin()
        {
            // try to get the user's info from the server
            GetSettings();

            if (_authProvider.Provider != AuthenticationProvider.Sturfee)
            {
                _idToken = _token;
                if (string.IsNullOrEmpty(_idToken)) { return false; }

                await RefreshCognitoLogin();

                var userDataJson = ReadJwtTokenContent(_idToken); // user.SessionTokens.IdToken);
                Debug.Log($"userDataJson = {userDataJson}");
                var userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(userDataJson);                

                _currentUser = new XrcsUserData
                {
                    Id = Guid.Parse($"{userData["sub"]}"),
                    Name = $"{userData["email"]}", //userData["email"],
                    Email = $"{userData["email"]}",
                    RefId = $"{userData["custom:company_id"]}",
                };

                return true;
            }

            HttpWebRequest xrcsAuthRequest = (HttpWebRequest)WebRequest.Create($"{XrConstants.XRCS_API}/accounts/auth/login");
            xrcsAuthRequest.Method = "POST";
            xrcsAuthRequest.ContentType = "application/json; charset=utf-8";
            xrcsAuthRequest.ContentLength = 0;
            AuthHelper.AddAuthHeaders(xrcsAuthRequest);

            MyLogger.Log($"tokenRequest = {XrConstants.XRCS_API}/accounts/auth/login");
            try
            {
                var tokenResponse = await xrcsAuthRequest.GetResponseAsync() as HttpWebResponse;
                if (tokenResponse.StatusCode != HttpStatusCode.OK)
                {
                    MyLogger.LogError($"ERROR:: User API => {tokenResponse.StatusCode} - {tokenResponse.StatusDescription}");
                    return false;
                }

                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    string jsonResponse = reader.ReadToEnd();
                    MyLogger.Log($"Found XRCS User and Account data: \n{jsonResponse}");

                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        var userResponse = JsonConvert.DeserializeObject<XrcsUserData>(jsonResponse);

                        if (userResponse != null)
                        {
                            _currentUser = userResponse;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError(ex);
                Logout();
                throw;
            }

            return true;
        }


        public async Task<bool> StartLoginFlowCognito(string username, string password, AuthenticationProviderConfig config)
        {
            try
            {
                Debug.Log("[AwsCognitoAuthManager] :: Authenticating user...");
                AmazonCognitoIdentityProviderClient provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), RegionEndpoint.GetBySystemName($"{config.Region}")); // Amazon.RegionEndpoint.APNortheast1);

                CognitoUserPool userPool = new CognitoUserPool(config.PoolId, config.ClientId, provider);
                CognitoUser user = new CognitoUser(username, config.ClientId, userPool, provider);

                Debug.Log("[AwsCognitoAuthManager] :: Waiting for Authentication response...");
                AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest
                {
                    Password = password
                }).ConfigureAwait(false);


                if (authResponse.AuthenticationResult != null)
                {
                    Debug.Log("[AwsCognitoAuthManager] :: User successfully authenticated.");
                    _idToken = authResponse.AuthenticationResult.IdToken;
                    var accessToken = authResponse.AuthenticationResult.AccessToken;

                    if (config.Token == CognitoAuthTokenType.IdToken)
                    {
                        SetAuthToken(_idToken);
                    }
                    else
                    {
                        SetAuthToken(accessToken);
                    }

                    //var userDataJson = ReadJwtTokenContent(_idToken); // user.SessionTokens.IdToken);
                    //Debug.Log($"userDataJson = {userDataJson}");
                    //_userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(userDataJson);
                    //Debug.Log($"User Data = {JsonConvert.SerializeObject(_userData)}");

                    // //Debug.Log("Done!");
                    // //Debug.Log(user.SessionTokens.IdToken);
                    Debug.Log("[AwsCognitoAuthManager] :: Success");

                    // await GetShops();

                    var authContext = new CognitoAuthContext
                    {
                        Provider = config.Provider,
                        Region = config.Region,
                        PoolId = config.PoolId,
                        ClientId = config.ClientId,
                        Token = config.Token,
                        IsBearer = config.IsBearer,

                        Username = username,
                        IdToken = authResponse.AuthenticationResult.IdToken,
                        AccessToken = authResponse.AuthenticationResult.AccessToken,
                        RefreshToken = authResponse.AuthenticationResult.RefreshToken,
                        IssuedTime = user.SessionTokens.IssuedTime,
                        ExpirationTime = user.SessionTokens.ExpirationTime,
                    };

                    var authContextJson = JsonConvert.SerializeObject(authContext);
                    Debug.Log($"Login Context: {authContextJson}");

                    /*PlayerPrefs.SetString(_authContextKey, authContextJson)*/;
                    Dispatcher.RunOnMainThread(() => PlayerPrefs.SetString(_authContextKey, authContextJson));

                    return true;
                }
                else
                {
                    Debug.Log("Error in authentication process.");
                }

                // return user;
                return false;
            }
            catch (Exception ex)
            {
                Debug.Log("[AwsCognitoAuthManager] :: ERROR");
                Debug.Log(ex);
                throw;
            }

            return false;
        }

        public void LoginAsGuest()
        {
            GetSettings();
            GetGuestId();
        }

        public Guid GetGuestId()
        {
            var guestId = Guid.NewGuid();
            if (PlayerPrefs.HasKey("guestUserId"))
            {
                var id = PlayerPrefs.GetString("guestUserId");
                if (!Guid.TryParse(id, out guestId))
                {
                    MyLogger.LogError($"Invalid Guest ID");
                }
            }
            else
            {
                PlayerPrefs.SetString("guestUserId", $"{guestId}");
            }

            return guestId;
        }

        public void Logout()
        {
            Debug.Log($"LOGOUT!!!");
            MyLogger.Log($"LOGOUT!!!");

            _token = string.Empty;
            _sessionId = string.Empty;
            _idToken = string.Empty;

            PlayerPrefs.SetString(_authSessionIdKey, "");
            PlayerPrefs.SetString(_authTokenKey, "");

            PlayerPrefs.DeleteKey(_authSessionIdKey);
            PlayerPrefs.DeleteKey(_authTokenKey);
            PlayerPrefs.DeleteKey(_playerInfoKey);
            PlayerPrefs.DeleteKey(_authContextKey);
            _currentUser = null;

            AuthHelper.SessionId = string.Empty;
            AuthHelper.Token = string.Empty;
        }

        private async Task<bool> RefreshCognitoLogin()
        {
            // https://github.com/aws/aws-sdk-net-extensions-cognito/issues/24            


            var authContextJson = PlayerPrefs.GetString(_authContextKey);
            var authContext = JsonConvert.DeserializeObject<CognitoAuthContext>(authContextJson);

            // check if token is expired
            if (DateTime.Now < authContext.ExpirationTime) { return true; }

            if (authContext == null)
            {
                return false;
            }

            Debug.Log($"[AwsCognitoAuthManager] :: Refreshing authentication... \n{authContextJson}");
            AmazonCognitoIdentityProviderClient provider = new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName($"{authContext.Region}")); // Amazon.RegionEndpoint.APNortheast1);

            CognitoUserPool userPool = new CognitoUserPool(authContext.PoolId, authContext.ClientId, provider);
            CognitoUser user = new CognitoUser(authContext.Username, authContext.ClientId, userPool, provider);
            user.SessionTokens = new CognitoUserSession(
                authContext.IdToken,
                authContext.AccessToken,
                authContext.RefreshToken,
                authContext.IssuedTime, //user.SessionTokens.IssuedTime,
                DateTime.Now.AddHours(1)
            );
            var authResponse = await user.StartWithRefreshTokenAuthAsync(new InitiateRefreshTokenAuthRequest
            {
                AuthFlowType = AuthFlowType.REFRESH_TOKEN
            }).ConfigureAwait(false);

            if (authResponse.AuthenticationResult != null)
            {
                Debug.Log("[AwsCognitoAuthManager] :: User successfully authenticated (refresh).");
                _idToken = authResponse.AuthenticationResult.IdToken;
                var accessToken = authResponse.AuthenticationResult.AccessToken;

                if (authContext.Token == CognitoAuthTokenType.IdToken)
                {
                    SetAuthToken(_idToken);
                }
                else
                {
                    SetAuthToken(accessToken);
                }
                Debug.Log("[AwsCognitoAuthManager] :: Refresh Success");

                authContext.IdToken = authResponse.AuthenticationResult.IdToken;
                authContext.AccessToken = authResponse.AuthenticationResult.AccessToken;
                authContext.RefreshToken = authResponse.AuthenticationResult.RefreshToken;
                authContext.IssuedTime = user.SessionTokens.IssuedTime;
                authContext.ExpirationTime = user.SessionTokens.ExpirationTime;

                authContextJson = JsonConvert.SerializeObject(authContext);
                Debug.Log($"Login Context: {authContextJson}");
                Dispatcher.RunOnMainThread(() => PlayerPrefs.SetString(_authContextKey, authContextJson));

                return true;
            }
            else
            {
                Debug.Log("Error in authentication refresh process.");
            }

            return false;
        }


        protected void GetSettings()
        {
            // get the provider from settings; default to Sturfee login
            var authProviderSettings = Resources.Load<AuthenticationProviderConfig>($"Sturfee/Auth/AuthProvider");
            if (authProviderSettings != null)
            {
                Debug.Log($"[AuthManager] :: Auth Provder Settings Found!");
                _authProvider = authProviderSettings;
                _authProviderType = _authProvider.Provider;
            }
        }

        protected string ReadJwtTokenContent(string token)
        {
            var content = token.Split('.')[1];
            Debug.Log(content);

            var jsonPayload = Encoding.UTF8.GetString(
                this.Decode(content));
            Debug.Log(jsonPayload);

            return jsonPayload; // JsonSerializer.Deserialize<JwtTokenContent>(jsonPayload);
        }

        private byte[] Decode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.ArgumentOutOfRangeException("input", "Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }

}
