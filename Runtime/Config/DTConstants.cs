using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.DigitalTwin
{
    public static class DtConstants
    {
        public static readonly string LOCAL_SPACES_PATH = "DigitalTwin/Spaces";
        public static readonly string LOCAL_ASSETS_PATH = "DigitalTwin/Assets";

#if DTE_DEV
        public static readonly string SPACES_API = "https://digitwin.devsturfee.com/api/v2.0";
        public static readonly string DTE_API = "https://digitwin.devsturfee.com/api/v2.0";

#else
        public static readonly string SPACES_API = "https://sharedspaces-api.sturfee.com/api/v2.0";
        public static readonly string DTE_API = "https://sharedspaces-api.sturfee.com/api/v2.0";
#endif

    }
}