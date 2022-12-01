using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sturfee.DigitalTwin.Demo
{
    public delegate void PlayerInfoDelegate(PlayerInfo playerInfo);

    public class PlayerInfoManager : SimpleSingleton<PlayerInfoManager>
    {
        public event PlayerInfoDelegate OnPlayerInfoChanged;

        public string DefaultPlayerName = "Player";

        [SerializeField]
        private PlayerInfo _playerInfo;

        private void Start()
        {
            string json = PlayerPrefs.GetString("playerInfo", "");
            
            if (!string.IsNullOrEmpty(json))
            {
                MyLogger.Log($" Loading playerinfo from PlayerPrefs  {json}");
                _playerInfo = JsonUtility.FromJson<PlayerInfo>(json);
            }            
        }

        public void SetData(PlayerInfo playerInfo)
        {
            _playerInfo = playerInfo;
            OnPlayerInfoChanged?.Invoke(playerInfo);

            PlayerPrefs.SetString("playerInfo", JsonUtility.ToJson(playerInfo));
        }

   
        public PlayerInfo PlayerInfo
        {
            get 
            { 
                if (_playerInfo == null)
                {
                    _playerInfo =  new PlayerInfo
                    {
                        Name = DefaultPlayerName,
                        SpaceMode = SpaceMode.VR,
                        Color = Color.white
                    };
                }

                return _playerInfo;
            }
        }

        public bool IsEqual(PlayerInfo other)
        {            
            if (other == null)
                return false;

            var myPlayerInfo = PlayerInfo;
            if (myPlayerInfo.Name != other.Name) return false;
            if (myPlayerInfo.SpaceMode != other.SpaceMode) return false;
            if (myPlayerInfo.Color != other.Color) return false;

            return true;
        }
    }
}