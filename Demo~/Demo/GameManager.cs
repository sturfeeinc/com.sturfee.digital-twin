using Mirror;
using Sturfee.DigitalTwin.CMS;
using Sturfee.DigitalTwin.Demo;
using Sturfee.XRCS.Config;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SpaceMode
{
    AR,
    VR
}

public class GameManager : SceneSingleton<GameManager>
{
    public GameObject SinglePlayerPrefab;
    public bool IsLoading;

    [SerializeField]
    private XrSwitch _xrSwitch;

    public SpaceMode CurrentSpaceMode => _currentSpaceMode;
    [SerializeField]
    private SpaceMode _currentSpaceMode;

#if CLIENT
    private async void Start()
    {
        IsLoading = true;
        LoadScreenManager.Instance.ShowLoadingScreen();

        // space location
        var space = SpacesManager.Instance.CurrentSpace;
        var location = new GeoLocation
        {
            Latitude = space.Location.Latitude,
            Longitude = space.Location.Longitude,
            Altitude = space.Location.Altitude,
        };

        // Load XrSession
        SturfeeXrSession sturfeeXrSession = FindObjectOfType<SturfeeXrSession>();
        sturfeeXrSession.Location = location;
        sturfeeXrSession.StartSet = SpacesManager.Instance.StartSpaceMode == SpaceMode.AR ? 0 : 1;
        sturfeeXrSession.CreateSession();      

        // Load CMS
        await CMSLoader.Instance.LoadAssets(space);

        if(SpacesManager.Instance.GameMode == GameMode.Single)
        {
            Instantiate(SinglePlayerPrefab);
        }

        SwitchSpaceMode(SpacesManager.Instance.StartSpaceMode);

        IsLoading = false;
        LoadScreenManager.Instance.HideLoadingScreen();

    }
#endif

    public void SwitchSpaceMode(SpaceMode spaceMode)
    {        
        OnProviderSetChange(spaceMode == SpaceMode.AR ? 0 : 1);

        _xrSwitch.SetValue(_currentSpaceMode == SpaceMode.AR ? 0 : 1);

    }

    public void Exit()
    {
        if (SpacesManager.Instance.GameMode == GameMode.Single)
        {
            SceneManager.LoadScene("2.Dashboard");
        }
        NetworkManager.singleton.StopClient();
    }

    public void OnProviderSetChange(int setNum)
    {
        // 0 => AR, 1 => VR
        _currentSpaceMode = setNum == 0 ? SpaceMode.AR : SpaceMode.VR;

        var playerInfo = PlayerInfoManager.Instance.PlayerInfo;
        playerInfo.SpaceMode = _currentSpaceMode;
        PlayerInfoManager.Instance.SetData(playerInfo);

        if (_currentSpaceMode == SpaceMode.AR)
        {
            XrCamera.Camera.cullingMask |= 1 << LayerMask.NameToLayer($"{XrLayers.XrAssets}") ;
        }
        else
        {
            // VR uses third person camera to look at XrAssets so remove it from XrCamera
            XrCamera.Camera.cullingMask &= ~(1 << LayerMask.NameToLayer($"{XrLayers.XrAssets}"));

        }
    }

}
