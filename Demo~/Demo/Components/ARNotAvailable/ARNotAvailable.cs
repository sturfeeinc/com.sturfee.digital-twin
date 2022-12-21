using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARNotAvailable : MonoBehaviour
{
    private void Start()
    {
        SturfeeEventManager.OnProviderRegister += OnProviderRegister;
        SturfeeEventManager.OnLocalizationRequested += OnLocalizationRequested;
    }    

    private void OnDestroy()
    {
        SturfeeEventManager.OnProviderRegister -= OnProviderRegister;
        SturfeeEventManager.OnLocalizationRequested -= OnLocalizationRequested;
    }

    private void OnProviderRegister(IProvider provider)
    {
        if (provider is ILocalizationProvider localizatinoProvider)
        {
            localizatinoProvider.DisableLocalization();
            VpsButton.CurrentInstance.SetState(VpsScanState.Off);
        }
    }

    private void OnLocalizationRequested()
    {
        var distance = GeoLocation.Distance(XrCamera.Pose.GeoLocation, Converters.UnityToGeoLocation(Vector3.zero));
        if (distance > 300)
        {
            AlertDialogManager.Instance.ShowAlert(
                "AR Not Available",
                "Visit real location to experience AR",
                (ok) =>
                {
                    GameManager.CurrentInstance.SwitchSpaceMode(SpaceMode.VR);
                },
                "Switch to VR"
            );

        }
    }
}
