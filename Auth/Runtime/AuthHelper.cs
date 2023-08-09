using Sturfee.DigitalTwin.Auth;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sturfee.Auth
{
    public static class AuthHelper
    {
        public static string Token { get; set; }
        public static string SessionId { get; set; }

        public static void AddAuthHeaders(HttpWebRequest request, bool addApiKey = true)
        {
            CheckToken();

            if (AuthManager.Instance.AuthProviderType == AuthenticationProvider.Sturfee) //!string.IsNullOrEmpty(Token)) //false) //AuthManager.Instance.UseDesktopAuth)
            {
                MyLogger.Log($"Adding Sturfee Suth Code to request \n{SessionId}");
                //request.Headers.Add(HttpRequestHeader.Authorization, $"{Code}"); // add the token to auth header
                request.Headers.Add(HttpRequestHeader.Authorization, $"{SessionId}"); // add the token to auth header
                request.Headers.Add("sturfee_sid", $"{SessionId}"); // add the token to auth header
            }
            else
            {                
                // var authHeader = AuthManager.Instance.AuthProvider.IsBearer ? $"Bearer {Token}" : Token;
                // MyLogger.Log($"Adding Token to request \n{authHeader}");
                // request.Headers.Add(HttpRequestHeader.Authorization, $"{authHeader}"); // add the token to auth header
                MyLogger.Log($"Adding Token to request \n{Token}");
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}"); // add the token to auth header
            }

            if(addApiKey) { AddApiKey(request); }                
        }

        public static void AddAuthHeaders(UnityWebRequest request, bool addApiKey = true)
        {
            CheckToken();

            if (AuthManager.Instance.AuthProviderType == AuthenticationProvider.Sturfee) //!string.IsNullOrEmpty(Token)) //false) //AuthManager.Instance.UseDesktopAuth)
            {
                MyLogger.Log($"Adding Sturfee Suth Code to request \n{SessionId}");
                request.SetRequestHeader("Authorization", $"{SessionId}"); // add the token to auth header
                request.SetRequestHeader("sturfee_sid", $"{SessionId}"); // add the token to auth header
            }
            else
            {                
                // var authHeader = AuthManager.Instance.AuthProvider.IsBearer ? $"Bearer {Token}" : Token;
                // MyLogger.Log($"Adding Token to request \n{authHeader}");
                // request.Headers.Add(HttpRequestHeader.Authorization, $"{authHeader}"); // add the token to auth header
                MyLogger.Log($"Adding Token to request \n{Token}");
                request.SetRequestHeader("Authorization", $"Bearer {Token}"); // add the token to auth header
            }

            if(addApiKey) { AddApiKey(request); }                
        }

        public static void AddXrcsTokenAuthHeader(HttpWebRequest request, bool addApiKey = true)
        {
            CheckToken();

            request.Headers.Add("AuthCode", $"{SessionId}"); // add the token to auth header

            if (!string.IsNullOrEmpty(Token)) //false) //AuthManager.Instance.UseDesktopAuth)
            {
                MyLogger.Log($"Adding Token to request \n{Token}");
                // request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}"); // add the token to auth header
                request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}"); // add the token to auth header
            }
            else
            {
                MyLogger.Log($"Adding Code to request \n{SessionId}");
                //request.Headers.Add(HttpRequestHeader.Authorization, $"{Code}"); // add the token to auth header
                request.Headers.Add(HttpRequestHeader.Authorization, $"{SessionId}"); // add the token to auth header
                request.Headers.Add("sturfee_sid", $"{SessionId}"); // add the token to auth header
            }

            if(addApiKey)
                AddApiKey(request);
        }

        public static void AddApiKey(HttpWebRequest request)
        {
            AppKeySupportedPlatforms platform = AppKeySupportedPlatforms.Desktop;
#if UNITY_ANDROID
            platform = AppKeySupportedPlatforms.Android;
#elif UNITY_IOS
            platform = AppKeySupportedPlatforms.IOS;
#else
            platform = AppKeySupportedPlatforms.Desktop;
#endif

            var appKeySO = Resources.Load<AppKeyConfig>($"Sturfee/Auth/AppKeys/{platform}");

            if (appKeySO == null)
            {
                Debug.LogWarning($"No AppKey added to headers : AppKeyConfig not found for {platform}. Check Assets/Resources/Sturfee/Auth/AppKeys/{platform}.asset ");
            }
            else
            {
                if (string.IsNullOrEmpty(appKeySO.ApiKey) || appKeySO.ApiKey.StartsWith("*"))
                {
                    Debug.LogWarning($"No AppKey added to headers : ApiKey is Empty/Invalid for {platform}. Check Assets/Resources/Sturfee/Auth/AppKeys/{platform}.asset ");
                    return;
                }

                MyLogger.Log($" Adding App Key to request\nx-sturfee-api-key:{appKeySO.ApiKey} | {appKeySO.SourceHeader}:{appKeySO.SourceId}");
                request.Headers.Add("x-sturfee-api-key", appKeySO.ApiKey);
                request.Headers.Add(appKeySO.SourceHeader, appKeySO.SourceId);
            }

        }

        public static void AddApiKey(UnityWebRequest request)
        {
            AppKeySupportedPlatforms platform = AppKeySupportedPlatforms.Desktop;
#if UNITY_ANDROID
            platform = AppKeySupportedPlatforms.Android;
#elif UNITY_IOS
            platform = AppKeySupportedPlatforms.IOS;
#else
            platform = AppKeySupportedPlatforms.Desktop;
#endif

            var appKeySO = Resources.Load<AppKeyConfig>($"Sturfee/Auth/AppKeys/{platform}");

            if (appKeySO == null)
            {
                Debug.LogWarning($"No AppKey added to headers : AppKeyConfig not found for {platform}. Check Assets/Resources/Sturfee/Auth/AppKeys/{platform}.asset ");
            }
            else
            {
                if (string.IsNullOrEmpty(appKeySO.ApiKey) || appKeySO.ApiKey.StartsWith("*"))
                {
                    Debug.LogWarning($"No AppKey added to headers : ApiKey is Empty/Invalid for {platform}. Check Assets/Resources/Sturfee/Auth/AppKeys/{platform}.asset ");
                    return;
                }

                MyLogger.Log($" Adding App Key to request");
                request.SetRequestHeader("x-sturfee-api-key", appKeySO.ApiKey);
                request.SetRequestHeader(appKeySO.SourceHeader, appKeySO.SourceId);
            }

        }

        public static void AddXrcsTokenAuthHeader(WebClient request)
        {
            MyLogger.Log($"Adding Token to request");
            CheckToken();
            request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {Token}"); // add the token to auth header
        }

        private static void CheckToken()
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                SessionId = AuthManager.Instance.SessionId;
            }
            if (string.IsNullOrEmpty(Token))
            {
                Token = AuthManager.Instance.AuthToken;
            }
        }
    }
}