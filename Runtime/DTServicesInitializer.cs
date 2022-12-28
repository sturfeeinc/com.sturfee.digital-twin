using Sturfee.DigitalTwin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DTServicesInitializer : MonoBehaviour
{
    private void Awake()
    {
        ServiceManager.Instance.Init(ServiceType.Web);
    }
}
