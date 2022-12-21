using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontRenderBeforeLocalization : MonoBehaviour
{
    public LayerMask _initialMask;
#if CLIENT

    private void Start()
    {
        _initialMask = XrCamera.Camera.cullingMask;
    }

    
    private void Update()
    {
        var localizationProvider = XrSessionManager.GetSession().GetProvider<ILocalizationProvider>();
        if (localizationProvider == null || localizationProvider.GetProviderStatus() != ProviderStatus.Ready)
        {
            XrCamera.Camera.cullingMask = 0;
            return;
        }

        XrCamera.Camera.cullingMask = _initialMask;

    }
#endif
}
