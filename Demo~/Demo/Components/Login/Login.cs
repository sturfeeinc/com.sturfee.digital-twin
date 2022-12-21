using Sturfee.XRCS;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sturfee.DigitalTwin.Demo
{
    public class Login : MonoBehaviour
    {        
        public GameObject Loader;
        public TMP_InputField Username;
        public TMP_InputField Password;
        public TextMeshProUGUI Error;

        public Button SubmitButton;

        private void Awake()
        {
            Username.gameObject.SetActive(true);
            Password.gameObject.SetActive(true);
        }

        private void Start()
        {
            SubmitButton.onClick.AddListener(TryLogin);

            TryRelogin();
        }

        private void OnDestroy()
        {
            SubmitButton.onClick.RemoveAllListeners();
        }

        public void LoginAsGuest()
        {
            AuthManager.Instance.LoginAsGuest();
            HandleSuccessfuleLogin();
        }

        private async void TryLogin()
        {
            LoadScreenManager.Instance.ShowLoadingScreen();
            Error.gameObject.SetActive(false);


            var username = Username.text;
            var password = Password.text;
            //TokenLoginDesktop(username, password);
            try
            {
                if (await AuthManager.Instance.StartLoginFlow(username, password, null))
                {
                    TryRelogin();
                }
                else
                {
                    LoadScreenManager.Instance.HideLoadingScreen();
                    Error.gameObject.SetActive(true);
                    Error.text =
                        "Invalid Credentials\n" +
                        "Check your username and password and try again.";
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError(ex);
                LoadScreenManager.Instance.HideLoadingScreen();
                Error.gameObject.SetActive(true);
                Error.text =
                    "Invalid Credentials\n" +
                    "Check your username and password and try again.";

                throw;
            }

            //Loader.SetActive(false);
        }

        private async void TryRelogin()
        {
            LoadScreenManager.Instance.ShowLoadingScreen();
            Error.gameObject.SetActive(false);


            if (string.IsNullOrEmpty(AuthManager.Instance.SessionId))
            {
                LoadScreenManager.Instance.HideLoadingScreen();
                return;
            }

            try
            {
                if (await AuthManager.Instance.TryLogin())
                {
                    HandleSuccessfuleLogin();
                }

                LoadScreenManager.Instance.HideLoadingScreen();
            }
            catch (Exception e)
            {
                LoadScreenManager.Instance.HideLoadingScreen();
                MyLogger.LogError(e);
                throw;
            }
        }

        private void HandleSuccessfuleLogin()
        {
            MyLogger.Log(" Successful login");
            SceneManager.LoadScene("2.Dashboard");
        }
    }
}