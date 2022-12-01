using Mirror;

namespace Sturfee.DigitalTwin.Demo
{
    public class NetworkPlayerInfo : NetworkBehaviour
    {
        [SyncVar]
        public PlayerInfo PlayerInfo;
       
        private PlayerAvatar _playerAvatar;
        private PlayerHUD _playerHUD;

        private void Start()
        {
            _playerHUD = GetComponent<PlayerHUD>();
            _playerAvatar = GetComponentInChildren<PlayerAvatar>();

            if (isClient && isLocalPlayer)
            {
                CmdUpdatePlayerInfo(PlayerInfoManager.Instance.PlayerInfo);
                _playerAvatar.SetColor(PlayerInfoManager.Instance.PlayerInfo.Color);
            }
        }

        private void FixedUpdate()
        {
            if (!isClient)
                return;

            if (isLocalPlayer)
            {
                // Get latest state of playerinfo from PlayerInfoManager            
                if (!PlayerInfoManager.Instance.IsEqual(PlayerInfo))
                {
                    CmdUpdatePlayerInfo(PlayerInfoManager.Instance.PlayerInfo);
                    _playerAvatar.SetColor(PlayerInfoManager.Instance.PlayerInfo.Color);
                }
            }
            else
            {
                _playerHUD.SetData(PlayerInfo);
                _playerAvatar.SetColor(PlayerInfo.Color);
            }
        }

        [Command]
        private void CmdUpdatePlayerInfo(PlayerInfo playerInfo)
        {
            PlayerInfo = playerInfo;
        }
    }
}
