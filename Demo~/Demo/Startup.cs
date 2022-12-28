using Sturfee.DigitalTwin.CMS;
using Sturfee.DigitalTwin.Tiles;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class Startup : MonoBehaviour
    {
        [Header("Config")]
        private ServiceType serviceType;
        [SerializeField]
        private MyLogLevel _logLevel = MyLogLevel.INFO;
        
        private void Start()
        {
            ServiceManager.Instance.Init(serviceType);

            var spacesManager = SpacesManager.Instance;
            //var playerInfoManager = PlayerInfoManager.Instance;
            var cmsLoader = CMSLoader.Instance;
            var spacesListManager = SpacesListManager.Instance;
            var loadScreenManager = LoadScreenManager.Instance;
            var toastManager = MobileToastManager.Instance;
            var alertManager = AlertDialogManager.Instance;

//#if !DEBUG
//        _logLevel = MyLogLevel.ERROR;
//#endif
            MyLogger.LogFileDirectory = Path.Combine(Application.persistentDataPath, "my-logs");

        }
    }
}