using Newtonsoft.Json;
using Sturfee.XRCS.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Sturfee.XRCS
{
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

        // Player Prefs
        private string _authSessionIdKey = "sessionid";
        private string _authTokenKey = "authtoken";
        private string _playerInfoKey = "PlayerInfo";

        private void Awake()
        {
            _sessionId = PlayerPrefs.GetString(_authSessionIdKey);
            _token = PlayerPrefs.GetString(_authTokenKey);

            MyLogger.Log($"Found Auth: \ntoken={_token}\nsessionid={_sessionId}");
        }

        public void SetAuthSessionId(string sessionId)
        {
            MyLogger.Log($"Saving Session ID = {sessionId}");
            _sessionId = sessionId;
            PlayerPrefs.SetString(_authSessionIdKey, sessionId);
        }

        public void SetAuthToken(string token)
        {
            _token = token;
            PlayerPrefs.SetString(_authTokenKey, token);
        }

        public async Task<bool> StartLoginFlow(string username, string password, string code)
        {
            // use code OR user/password to start session

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

            HttpWebRequest xrcsAuthRequest = (HttpWebRequest)WebRequest.Create($"{XrConstants.XRCS_API}/accounts/auth/login");
            xrcsAuthRequest.Method = "POST";
            xrcsAuthRequest.ContentType = "application/json; charset=utf-8";
            xrcsAuthRequest.ContentLength = 0;
            AuthHelper.AddXrcsTokenAuthHeader(xrcsAuthRequest);

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

        public void LoginAsGuest()
        {
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
            MyLogger.Log($"LOGOUT!!!");

            _token = string.Empty;
            _sessionId = string.Empty;

            PlayerPrefs.SetString(_authSessionIdKey, "");
            PlayerPrefs.SetString(_authTokenKey, "");

            PlayerPrefs.DeleteKey(_authSessionIdKey);
            PlayerPrefs.DeleteKey(_authTokenKey);
            PlayerPrefs.DeleteKey(_playerInfoKey);
            _currentUser = null;

            AuthHelper.SessionId = string.Empty;
            AuthHelper.Token = string.Empty;
        }
    } 
}
