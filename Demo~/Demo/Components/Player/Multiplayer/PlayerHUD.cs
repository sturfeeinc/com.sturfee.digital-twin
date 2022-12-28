using Mirror;
using TMPro;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class PlayerHUD : NetworkBehaviour
    {
        [SerializeField]
        private GameObject _canvas;
        [SerializeField]
        private TextMeshProUGUI _playerName;
        [SerializeField]
        private GameObject _arIcon;
        [SerializeField]
        private GameObject _vrIcon;

        private void Start()
        {
            _canvas.SetActive(!isLocalPlayer);
        }
                
        public void SetData(PlayerInfo playerInfo)
        {
            _playerName.text = playerInfo.Name;
            _arIcon.SetActive(playerInfo.SpaceMode == SpaceMode.AR);
            _vrIcon.SetActive(playerInfo.SpaceMode == SpaceMode.VR);
        }
    }
}