using UnityEngine;

namespace Sturfee.DigitalTwin.Auth
{
    public enum AppKeySupportedPlatforms
    {
        Android,
        IOS,
        Desktop
    }

    public class AppKeyConfig : ScriptableObject
    {
        public string ApiKey;
        public string SourceHeader;
        public string SourceId;
    }


}