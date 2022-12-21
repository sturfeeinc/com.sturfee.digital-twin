using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SturfeeVPS.Networking;
using SturfeeVPS.SDK;
using UnityEngine;
using Amazon.CognitoIdentity;
#if CLIENT
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Client = SturfeeVPS.Networking.GameLiftClient;
#endif


namespace Sturfee.DigitalTwin.Demo
{
    public class GameSessionManager : SimpleSingleton<GameSessionManager>
    {
#if CLIENT
        public async Task Join(XrSceneData sharedSpace)
        {
            try
            {
                // Initialize client
                var credentials = new CognitoAWSCredentials(AWSConfig.IdentityPoolId, AWSConfig.Region);
                Client.Instance.Initialize(credentials, AWSConfig.Region);

                // search if any gamesession is running for this space           
                var gameSession = await SearchGameSessionAsync(sharedSpace);

                // if not running then create a new gamesession
                if (gameSession == null)
                {
                    MyLogger.Log($" No active gamesession found for {sharedSpace.Id}");
                    gameSession = await CreateGameSessionAsync(sharedSpace);
                }

                // Join gamesession
                await JoinGameSessionAsync(sharedSpace, gameSession);

                // stop showing loader if network authentication failed
                var authenticator = (GameLiftAuthenticator)Mirror.NetworkManager.singleton.authenticator;
                authenticator.OnClientAuthenticated.AddListener(() =>
                {
                    authenticator.OnAuthenticationFailed -= err;
                });
                authenticator.OnAuthenticationFailed += err;

            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(FleetCapacityExceededException))
                {
                    MobileToastManager.Instance.ShowToast("All public spaces are running at full capacity. Please try again later ", -1, true);
                    // send notification or log this
                }
                else
                {
                    MobileToastManager.Instance.ShowToast("Error Loading Game. Please try again.", -1, true);
                }
                MyLogger.LogError($"{ex.GetType()} : {ex.Message} ");
                throw;
            }
        }

        public async Task JoinGameSessionAsync(XrSceneData sharedSpace, GameSession gameSession)
        {
            MyLogger.Log($" Joining gamesession for {sharedSpace.Name} ({sharedSpace.Id})");

            while (gameSession.Status != GameSessionStatus.ACTIVE)
            {
                MyLogger.Log($" Waiting for Game Session status to be active. Current status : {gameSession.Status} ");
                await Task.Delay(1000);
                gameSession = await Client.Instance.DescribeGameSessionAsync(gameSession);
            }

            MyLogger.Log($"Active Gamesession found for {sharedSpace.Name}({sharedSpace.Id})");
            MyLogger.Log(JsonConvert.SerializeObject(gameSession));

            // CreatePlayerSession authenticates and starts NetworkManager Client (NetworkManager.Singleton.StartClient)
            var playerSession = await Client.Instance.CreatePlayerSessionAsync(gameSession);

            MyLogger.Log($" playersession status {playerSession.Status}");

            //SpacesManager.CurrentSpace = Space;
            //MyLogger.Log($"Current Space : {JsonUtility.ToJson(Space)}");
        }

        public async Task<GameSession> CreateGameSessionAsync(XrSceneData sharedSpace)
        {
            MyLogger.Log($"Creating new Game Session for Space {sharedSpace.Name}");

            List<GameProperty> gameProperties = new List<GameProperty>()
                {
                    new GameProperty()
                    {
                        Key = "SpaceId",
                        Value = $"{sharedSpace.Id}"
                    }
                };

            var gameSession = await Client.Instance.CreateGameSessionAsync(gameProperties, AWSConfig.AliasId);
            MyLogger.Log(JsonConvert.SerializeObject(gameSession));

            return gameSession;
        }

        public async Task<GameSession> SearchGameSessionAsync(XrSceneData sharedSpace)
        {
            // Throttle
            await Task.Delay(250);

            MyLogger.Log($" Searching for Space {sharedSpace.Id} {sharedSpace.Name}");
            string filterExpression = $"gameSessionProperties.SpaceId = \'{sharedSpace.Id}\'";

            GameSession gameSession;
            if (Client.Instance.Local)
            {
                gameSession = await GetGameSessionAsync(sharedSpace);
            }
            else
            {
                gameSession = await Client.Instance.SearchGameSessionAsync(filterExpression, AWSConfig.AliasId);

                // Fix for Gamesessions active on gamelift without any process 
                if (gameSession?.Name == "corrupted")
                {
                    Debug.Log($"{gameSession.GameSessionId} ({sharedSpace.Name}) is corrupted");
                    return null;
                }
            }

            if (gameSession != null)
            {
                MyLogger.Log($" GameSession found for space {sharedSpace.Id} ");
            }
            else
            {
                MyLogger.Log(" GameSession not found");
            }

            return gameSession;
        }

        public async Task<GameSession> GetGameSessionAsync(XrSceneData sharedSpace)
        {
            try
            {
                var gameSessions = await Client.Instance.DescribeGameSessionsAsync(AWSConfig.AliasId);

                foreach (var gameSession in gameSessions)
                {
                    if (gameSession.GameProperties.Count > 0 && gameSession.Status == Amazon.GameLift.GameSessionStatus.ACTIVE)
                    {
                        foreach (var gp in gameSession.GameProperties)
                        {
                            if (gp.Key == "SpaceId")
                            {
                                if (gp.Value == $"{sharedSpace.Id}")
                                {
                                    return gameSession;
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MyLogger.LogError(ex.Message);
                throw;
            }
        }

        private void err(string error)
        {
            MobileToastManager.Instance.ShowToast("Error Loading Game. Please try again.", -1, true);
            // Hide Loader
        }
#endif

    }
}