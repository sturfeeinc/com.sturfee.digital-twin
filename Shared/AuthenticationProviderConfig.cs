using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.Auth
{
    public enum AuthenticationProvider
    {
        Sturfee,
        AwsCognito
    }

    public enum CognitoAuthTokenType
    {
        IdToken,
        AccessToken
    }

    public class AuthenticationProviderConfig : ScriptableObject
    {
        public AuthenticationProvider Provider;
        public string Region;
        public string PoolId;
        public string ClientId;
        public CognitoAuthTokenType Token;
        public bool IsBearer;
    }
}


