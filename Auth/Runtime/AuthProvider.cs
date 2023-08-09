using Newtonsoft.Json;
using Sturfee.XRCS.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Sturfee.Auth
{
    public interface IAuthProvider
    {
        XrcsUserData CurrentUser { get; }

        Task<XrcsUserData> GetUser(string email, string password);
        Task SetCachedUser(XrcsUserData data);
        Task<XrcsUserData> GetCachedUser();
        void Logout();
    }

    public class WebAuthProvider : IAuthProvider
    {
        public XrcsUserData CurrentUser => _currentUser;


        private string _eKey = $"sturfeekey1234551298765134567890"; // for AES, must be 24 or 32 characters

        private LocalFilesAuthProvider _localFilesUserProvider;
        private string _localUsersPath;

        private int _cacheTimeout = 7 * 24; // in hours (7 days)

        private XrcsUserData _currentUser;

        public WebAuthProvider()
        {
            _localFilesUserProvider = new LocalFilesAuthProvider();
            _localUsersPath = $"{Application.persistentDataPath}/{XrConstants.LOCAL_USER_PATH}/";
        }

        public async Task<XrcsUserData> GetCachedUser()
        {
            if (!Directory.Exists($"{_localUsersPath}")) { return null; }
            if (!File.Exists($"{_localUsersPath}/cached_user")) { return null; }

            using (StreamReader r = new StreamReader($"{_localUsersPath}/cached_user"))
            {
                var encoded = await r.ReadToEndAsync();
                var json = Base64Encoder.Decode(encoded);
                var data = JsonConvert.DeserializeObject<CachedXrcsUserData>(json);

                if (data.IsDemoUser)
                {
                    _currentUser = data;
                    return _currentUser;
                }

                //if ((DateTime.UtcNow - data.LoginDate) > TimeSpan.FromMinutes(_cacheTimeout)) // for testing
                if ((DateTime.UtcNow - data.LoginDate) > TimeSpan.FromHours(_cacheTimeout))
                {
                    MyLogger.Log($"Cached User Expired. Trying to Refresh...\n{data.LoginDate.ToString()}");
                    // try to refresh the user
                    var email = data.Email;
                    var encryptedPw = data.Password;
                    var pw = EncryptionUtils.DecryptString(_eKey, encryptedPw);

                    // use stored credentials to get the user from the User API
                    var user = await GetUser(email, pw);
                    if (user != null)
                    {
                        MyLogger.Log($"  Cached User Refreshed!!!");
                        _currentUser = user;
                        //await SetCachedUser(_currentUser);
                        return _currentUser;
                    }

                    File.Delete($"{_localUsersPath}/cached_user");
                    return null; // otherwise, cached user is VOID
                }

                _currentUser = data;
                return _currentUser;
            }
        }

        public async Task<XrcsUserData> GetUser(string email, string password)
        {
            MyLogger.Log($"Fetching User {email}");
            XrcsUserData result = null;

            var sturfeeToken = await GetSturfeeToken(email, password);

            // authenticate with XRCS API
            result = await GetXrcsUser(sturfeeToken);

            // hash password for storage
            result.Password = EncryptionUtils.EncryptString(_eKey, password);

            _currentUser = result;
            MyLogger.Log($"User Fetched from API: {JsonUtility.ToJson(result)}");

            return result;
        }

        public void Logout()
        {
            _localFilesUserProvider.Logout();
            _currentUser = null;
        }

        public async Task SetCachedUser(XrcsUserData data)
        {
            await _localFilesUserProvider.SetCachedUser(data);
            _currentUser = _localFilesUserProvider.CurrentUser;
        }

        private async Task<XrcsUserData> GetXrcsUser(string sturfeeToken)
        {
            XrcsUserData result = null;

            HttpWebRequest xrcsAuthRequest = (HttpWebRequest)WebRequest.Create($"{XrConstants.XRCS_API}/authenticate");
            xrcsAuthRequest.Method = "POST";
            xrcsAuthRequest.ContentType = "application/json; charset=utf-8";

            MyLogger.Log($"tokenRequest = {XrConstants.XRCS_API}/authenticate");

            using (var streamWriter = new StreamWriter(xrcsAuthRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(
                    new LoginResponse // re-use the LoginResponse schema (same for this auth request)
                    {
                        Token = sturfeeToken
                    }
                );

                streamWriter.Write(json);
                streamWriter.Flush();
            }
            try
            {
                var tokenResponse = await xrcsAuthRequest.GetResponseAsync() as HttpWebResponse;
                if (tokenResponse.StatusCode != HttpStatusCode.OK)
                {
                    Debug.LogError($"ERROR:: User API => {tokenResponse.StatusCode} - {tokenResponse.StatusDescription}");
                    return null;
                }

                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    string jsonResponse = reader.ReadToEnd();
                    var authResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(jsonResponse);

                    MyLogger.Log($"Found XRCS User and Account data: \n{jsonResponse}");

                    // TODO: what to do with data from XRCS API?
                    result = new XrcsUserData();
                    result.Id = authResponse.User.Id;
                    result.Name = authResponse.User.Name;
                    result.Email = authResponse.User.Email;
                    result.AccountId = authResponse.User.Id; // TODO: this is temporary
                    result.AccountIds = authResponse.User.AccountUsers.Select(x => x.AccountId).ToList();
                    result.AccountUsers = authResponse.User.AccountUsers;

                    // TODO: save token
                    result.Token = authResponse.Token;
                    MyLogger.Log($"XRCS Token = {authResponse.Token}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return result;
        }

        private async Task<string> GetSturfeeToken(string email, string password)
        {
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create($"{XrConstants.STURFEE_API}/login");
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/json; charset=utf-8";
            tokenRequest.Referer = "https://developer.sturfee.com";
            tokenRequest.Headers.Add("Origin", "https://developer.sturfee.com");

            MyLogger.Log($"tokenRequest = {XrConstants.STURFEE_API}/login");

            using (var streamWriter = new StreamWriter(tokenRequest.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(new LoginRequest
                {
                    Email = email,
                    Password = password //,
                                        //RememberMe = true
                });

                streamWriter.Write(json);
                streamWriter.Flush();
            }

            LoginResponse loginData;
            try
            {
                var tokenResponse = await tokenRequest.GetResponseAsync() as HttpWebResponse;
                if (tokenResponse.StatusCode != HttpStatusCode.OK)
                {
                    Debug.LogError($"ERROR:: User API => {tokenResponse.StatusCode} - {tokenResponse.StatusDescription}");
                    return null;
                }

                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    string jsonResponse = reader.ReadToEnd();
                    loginData = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);
                }

                MyLogger.Log($"Sturfee Token = {loginData.Token}");
                return loginData.Token;
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        Debug.LogError($"Error Response Code = {errorResponse.StatusCode} ({(int)errorResponse.StatusCode})");

                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            if (reader != null)
                            {
                                string error = reader.ReadToEnd();
                                loginData = JsonConvert.DeserializeObject<LoginResponse>(error);

                                Debug.LogError($"{JsonUtility.ToJson(loginData)}");
                            }
                        }
                    }
                }

                //return null;
                throw;
            }
        }
    }

    public class LocalFilesAuthProvider : IAuthProvider
    {
        public XrcsUserData CurrentUser => _currentUser;

        private XrcsUserData _currentUser;

        private int _version = 3;
        private bool _forceUpdate = false;

        private string _localUsersPath;
        private int _cacheTimeout = 7; // in DAYS

        public LocalFilesAuthProvider()
        {
            _localUsersPath = $"{Application.persistentDataPath}/{XrConstants.LOCAL_USER_PATH}/";

            CheckVersion();

            if (_forceUpdate && File.Exists($"{_localUsersPath}/cached_user"))
            {
                File.Delete($"{_localUsersPath}/cached_user");
            }

        }

        public async Task<XrcsUserData> GetUser(string email, string password)
        {
            // TODO: error handling and logging
            if (email == string.Empty) { return null; }
            if (password == string.Empty) { return null; }

            var userPath = $"{_localUsersPath}/{email}";

            if (!Directory.Exists(userPath)) { return null; }

            using (StreamReader r = new StreamReader($"{userPath}/user"))
            {
                var encoded = await r.ReadToEndAsync();
                var json = Base64Encoder.Decode(encoded);
                var storedUser = JsonConvert.DeserializeObject<XrcsUserData>(json);

                if (storedUser.Email == email && storedUser.Password == password)
                {
                    _currentUser = storedUser;
                    return _currentUser;
                }

                return null;
            }
        }

        public async Task<XrcsUserData> GetCachedUser()
        {
            // TODO: error handling and logging

            if (!Directory.Exists($"{_localUsersPath}")) { return null; }
            if (!File.Exists($"{_localUsersPath}/cached_user")) { return null; }

            using (StreamReader r = new StreamReader($"{_localUsersPath}/cached_user"))
            {
                var encoded = await r.ReadToEndAsync();
                var json = Base64Encoder.Decode(encoded);
                var data = JsonConvert.DeserializeObject<CachedXrcsUserData>(json);

                if ((DateTime.UtcNow - data.LoginDate) > TimeSpan.FromHours(24 * _cacheTimeout))
                {
                    File.Delete($"{_localUsersPath}/cached_user");
                    return null;
                }
                else
                {
                    _currentUser = data;
                }

                return data;
            }
        }

        public async Task SetCachedUser(XrcsUserData data)
        {
            var cachedUser = JsonConvert.DeserializeObject<CachedXrcsUserData>(JsonConvert.SerializeObject(data));
            cachedUser.LoginDate = DateTime.UtcNow;

            var json = JsonConvert.SerializeObject(cachedUser);
            var encoded = Base64Encoder.Encode(json);
            using (StreamWriter r = new StreamWriter($"{_localUsersPath}/cached_user"))
            {
                await r.WriteAsync(encoded);
            }

            _currentUser = data;
        }

        public void Logout()
        {
            _currentUser = null;

            // delete cached user
            if (File.Exists($"{_localUsersPath}/cached_user"))
            {
                File.Delete($"{_localUsersPath}/cached_user");
            }
        }

        private void CheckVersion()
        {
            MyLogger.Log($"Checking Version...");
            var versionFileName = $"{_localUsersPath}/version";

            if (File.Exists(versionFileName))
            {
                var fileContents = File.ReadAllText(versionFileName);
                MyLogger.Log($"   Version Found ({fileContents})...");

                int currentVersion;
                try
                {
                    currentVersion = int.Parse(fileContents);
                    if (currentVersion == _version) { return; }
                }
                catch
                {
                    currentVersion = 0;
                }

                Debug.LogWarning($"   User Data Version Changed");
                _forceUpdate = true;
                File.WriteAllText(versionFileName, $"{_version}", System.Text.Encoding.UTF8);
            }
            else
            {
                MyLogger.Log($"   Setting up versioning...");
                if (!Directory.Exists($"{_localUsersPath}")) { Directory.CreateDirectory($"{_localUsersPath}"); }

                _forceUpdate = true;
                File.WriteAllText(versionFileName, $"{_version}", System.Text.Encoding.UTF8);
            }
            MyLogger.Log($"   Done.");
        }
    }
}