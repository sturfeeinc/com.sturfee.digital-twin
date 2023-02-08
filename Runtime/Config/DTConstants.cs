using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.DigitalTwin
{
    public static class DtConstants
    {
        public static readonly string LOCAL_SPACES_PATH = "DigitalTwin/Spaces";
        public static readonly string LOCAL_ASSETS_PATH = "DigitalTwin/Assets";

        public static readonly string SPACES_API = "https://sharedspaces-api.sturfee.com/api/v2.0";

        public static readonly string DTE_OUTDOOR_TILES_API = "https://digitaltwin.sturfee.com/street/tiles/zip";
        public static readonly string DTE_OUTDOOR_COVERAGE_API = "https://digitaltwin.sturfee.com/street/tiles/zip/coverage";

        public static readonly string SturfeeXrSessionVR = "SturfeeXrSession-VR";
        public static readonly string SturfeeXrSessionARVR = "SturfeeXrSession-AR+VR";

    }
}