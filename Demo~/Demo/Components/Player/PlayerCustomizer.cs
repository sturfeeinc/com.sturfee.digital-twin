using Sturfee.XRCS;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sturfee.DigitalTwin.Demo
{
    public enum PlayerColors
    {
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 4
    }

    public class PlayerCustomizer : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _playerEmail;
        [SerializeField]
        private TMP_InputField _playerName;
        [SerializeField]
        private Button _submit;
        [SerializeField]
        private Button _logout;

        private Color _color;

        private void Start()
        {
            var playerInfo = PlayerInfoManager.Instance.PlayerInfo;
            if(playerInfo.Name != PlayerInfoManager.Instance.DefaultPlayerName)
            {
                gameObject.SetActive(false);
            }

            SetData(playerInfo);
            _submit.onClick.AddListener(Submit);
        }

        private void OnDestroy()
        {
            _submit.onClick.RemoveAllListeners();
        }

        public void SetData(PlayerInfo playerInfo)
        {
            _playerName.text = playerInfo.Name;
            _color = playerInfo.Color;

            // If not logged in as Guest => show logout option
            if (AuthManager.Instance.CurrentUser != null)
            {
                _playerEmail.text = AuthManager.Instance.CurrentUser.Email;
                _logout.gameObject.SetActive(true);
            }
        }

        public void SetPlayerColor(int playerColor)
        {
            switch ((PlayerColors)playerColor)
            {
                case PlayerColors.Red: _color = Color.red; break;
                case PlayerColors.Green: _color = Color.green; break;
                case PlayerColors.Blue: _color = Color.blue; break;
                case PlayerColors.Yellow: _color = Color.yellow; break;
            }
        }

        public void Submit()
        {
            PlayerInfo playerInfo = new PlayerInfo
            {
                Name = _playerName.text,
                Color = _color,
            };

            PlayerInfoManager.Instance.SetData(playerInfo);

            gameObject.SetActive(false);
        }

        public void Logout()
        {
            AuthManager.Instance.Logout();
            SpacesListManager.Instance.Clear();

            SceneManager.LoadScene("Login");
        }
    }
}