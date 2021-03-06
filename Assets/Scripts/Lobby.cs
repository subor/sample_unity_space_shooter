﻿using Ruyi.SDK.Online;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

public class Lobby : Panel
{
    public override void Open()
    {
        base.Open();

        ShowLoadingCircle();
        CleanProfileData();

        FindMyLobbies();
    }

    public override void Close()
    {
        base.Close();

        if (RuyiNet != null &&
            RuyiNet.IsRuyiNetAvailable &&
            mLobby != null)
        {
            RuyiNet.LobbyService.LeaveLobby(RuyiNet.ActivePlayerIndex, mLobby.LobbyId, null);
        }

        mLobby = null;
    }

    public void StartGame()
    {
        if (RuyiNet.ActivePlayer.profileId == mLobby.OwnerPlayerId)
        {
            var networkManager = FindObjectOfType<MyNetworkManager>();
            if (networkManager != null)
            {
                networkManager.StartMatchMaker();
                networkManager.matchMaker.CreateMatch("Name", 4, true, "", "", "", 0, 0, OnMatchCreate);
            }
        }
    }

    public void FindLobbies()
    {
        ShowLoadingCircle();
        RuyiNet.LobbyService.FindLobbies(RuyiNet.ActivePlayerIndex, 10, RuyiNetLobbyType.PLAYER, OnFindLobbies);
    }

    public void FindFriendsLobbies()
    {
        ShowLoadingCircle();
        RuyiNet.LobbyService.FindFriendsLobbies(RuyiNet.ActivePlayerIndex, OnFindLobbies);
    }
    
    private void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (success)
        {
            base.Close();

            var overlay = FindObjectOfType<TutorialInfo>();
            overlay.gameObject.SetActive(false);
            AudioListener.volume = 1f;
            Time.timeScale = 1f;

            var networkManager = FindObjectOfType<MyNetworkManager>();
            networkManager.StartHost(matchInfo);

            Debug.Log(matchInfo.networkId.ToString());
            RuyiNet.LobbyService.StartGame(RuyiNet.ActivePlayerIndex, mLobby.LobbyId,
            matchInfo.networkId.ToString(), null);
        }
    }

    private void Start()
    {
        RuyiNet.LobbyService.OnLobbyClosed += OnLobbyClosed;
        RuyiNet.LobbyService.OnLobbyCreated += OnLobbyCreated;
        RuyiNet.LobbyService.OnLobbyDestroyed += OnLobbyDestroyed;
        RuyiNet.LobbyService.OnLobbyGameStarted += OnLobbyGameStarted;
        RuyiNet.LobbyService.OnLobbyOpened += OnLobbyOpened;
        RuyiNet.LobbyService.OnLobbyPlayerJoined += OnLobbyPlayerJoined;
        RuyiNet.LobbyService.OnLobbyPlayerLeft += OnLobbyPlayerLeft;
    }

    private void OnListMatches(bool success, string extendedInfo, List<MatchInfoSnapshot> matchInfo)
    {
        Debug.Log("Num Matches found: " + matchInfo.Count);
        var networkManager = FindObjectOfType<MyNetworkManager>();
        if (networkManager != null)
        {
            var networkId = (NetworkID)Enum.Parse(typeof(NetworkID), mConnectionString);
            networkManager.matchMaker.JoinMatch(networkId, "", "", "", 0, 0, OnJoinMatch);
        }
    }

    private void OnJoinMatch(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        Debug.Log("Join Match: " + success + " - " + extendedInfo);
        if (success)
        {
            base.Close();

            var overlay = FindObjectOfType<TutorialInfo>();
            overlay.gameObject.SetActive(false);
            AudioListener.volume = 1f;
            Time.timeScale = 1f;

            var networkManager = FindObjectOfType<MyNetworkManager>();
            networkManager.StartClient(matchInfo);
        }
    }

    private void OnClosed()
    {
        Close();
    }

    private void QuickMatch()
    {
        RuyiNet.LobbyService.FindLobbies(RuyiNet.ActivePlayerIndex, 10, RuyiNetLobbyType.PLAYER, 1, OnQuickMatchFind);
    }

    private void FindMyLobbies()
    {
        RuyiNet.LobbyService.GetMyLobbies(RuyiNet.ActivePlayerIndex, OnFindMyLobbies);
    }

    private void OnFindMyLobbies(RuyiNetLobby[] lobbies)
    {
        if (lobbies != null &&
            lobbies.Length > 0)
        {
            UpdateLobbyInfo(lobbies[0]);
        }
        else
        {
            RuyiNet.LobbyService.FindLobbies(RuyiNet.ActivePlayerIndex, 10, RuyiNetLobbyType.PLAYER, OnFindLobbies);           
        }
    }

    private void OnFindLobbies(RuyiNetLobby[] lobbies)
    {

        var startButton = GameObject.FindGameObjectWithTag("StartGame").GetComponent<Button>();
        var startButtonText = startButton.GetComponentInChildren<Text>();
        startButtonText.text = "CREATE LOBBY";
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(CreateLobby);

        if (lobbies != null &&
            lobbies.Length > 0)
        {
            var lobbyOwnerIds = new List<String>();
            mPlayerIdsToLobbyIds = new Dictionary<string, string>();

            foreach (var i in lobbies)
            {
                lobbyOwnerIds.Add(i.OwnerPlayerId);
                mPlayerIdsToLobbyIds[i.OwnerPlayerId] = i.LobbyId;
            }

            RuyiNet.FriendService.GetProfiles(RuyiNet.ActivePlayerIndex, lobbyOwnerIds.ToArray(), OnGotLobbyProfiles);
        }
        else
        {
            HideLoadingCircle();
        }
    }

    private void CreateLobby()
    {
        var startButton = GameObject.FindGameObjectWithTag("StartGame").GetComponent<Button>();
        startButton.enabled = false;

        RuyiNet.LobbyService.CreateLobby(RuyiNet.ActivePlayerIndex, 4, RuyiNetLobbyType.PLAYER, UpdateLobbyInfo);
    }

    private void OnQuickMatchFind(RuyiNetLobby[] lobbies)
    {
        if (lobbies != null &&
            lobbies.Length > 0)
        {
            if (lobbies[0].MemberPlayerIds.Contains(RuyiNet.ActivePlayer.profileId))
            {
                UpdateLobbyInfo(lobbies[0]);
            }
            else
            {
                RuyiNet.LobbyService.JoinLobby(RuyiNet.ActivePlayerIndex, lobbies[0].LobbyId, UpdateLobbyInfo);
            }
        }
        else
        {
            RuyiNet.LobbyService.CreateLobby(RuyiNet.ActivePlayerIndex, 4, RuyiNetLobbyType.PLAYER, UpdateLobbyInfo);
        }
    }

    private void UpdateLobbyInfo(RuyiNetLobby lobby)
    {
        mLobby = lobby;
        if (mLobby != null)
        {
            ShowLoadingCircle();
            RuyiNet.FriendService.GetProfiles(RuyiNet.ActivePlayerIndex, mLobby.MemberPlayerIds.ToArray(), OnGotProfiles);

            if (mLobby.OwnerPlayerId == RuyiNet.ActivePlayer.profileId)
            {
                var startButton = GameObject.FindGameObjectWithTag("StartGame").GetComponent<Button>();
                startButton.enabled = true;
                var buttonText = startButton.GetComponentInChildren<Text>();
                buttonText.text = "START GAME";
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartGame);
            }
        }
        else
        {
            HideLoadingCircle();
        }
    }

    private void OnGotProfiles(RuyiNetGetProfilesResponse response)
    {
        HideLoadingCircle();
        CleanProfileData();
        var profiles = response.data.response;
        var y = START_Y_POSITION;
        foreach (var i in profiles)
        {
            var playerProfile = AddProfileEntry(y, i.profileName, i.profileId, i.pictureUrl, "");

            var button = playerProfile.GetComponentInChildren<Button>();
            var buttonText = button.GetComponentInChildren<Text>();

            if (i.profileId == RuyiNet.ActivePlayer.profileId)
            {
                button.gameObject.SetActive(false);
            }
            else if (i.friend)
            {
                button.onClick.AddListener(() =>
                {
                    RemoveFriend(button, i.profileId);
                });

                buttonText.text = "REMOVE FRIEND";
            }
            else
            {
                button.onClick.AddListener(() =>
                {
                    AddFriend(button, i.profileId);
                });

                buttonText.text = "ADD FRIEND";
            }

            y += Y_POSITION_OFFSET;
        }
    }

    private void OnGotLobbyProfiles(RuyiNetGetProfilesResponse response)
    {
        HideLoadingCircle();

        CleanProfileData();
        var profiles = response.data.response;
        var y = START_Y_POSITION;
        foreach (var i in profiles)
        {
            var playerProfile = AddProfileEntry(y, i.profileName, i.profileId, i.pictureUrl, "");

            var button = playerProfile.GetComponentInChildren<Button>();
            var buttonText = button.GetComponentInChildren<Text>();

            button.onClick.AddListener(() =>
            {
                JoinLobby(mPlayerIdsToLobbyIds[i.profileId]);
            });

            buttonText.text = "JOIN";

            y += Y_POSITION_OFFSET;
        }
    }

    private void JoinLobby(string lobbyId)
    {
        RuyiNet.LobbyService.JoinLobby(RuyiNet.ActivePlayerIndex, lobbyId, UpdateLobbyInfo);
    }

    private void RemoveFriend(Button button, string profileId)
    {
        button.interactable = false;
        var buttonText = button.GetComponentInChildren<Text>();
        buttonText.text = "REMOVING...";

        RuyiNet.FriendService.RemoveFriend(RuyiNet.ActivePlayerIndex, profileId, (RuyiNetResponse) =>
        {
            UpdateLobbyInfo(mLobby);
        });
    }

    private void AddFriend(Button button, string profileId)
    {
        button.interactable = false;
        var buttonText = button.GetComponentInChildren<Text>();
        buttonText.text = "ADDING...";

        RuyiNet.FriendService.AddFriend(RuyiNet.ActivePlayerIndex, profileId, (RuyiNetResponse) =>
        {
            buttonText.text = "ADDED";
        });
    }

    private void OnLobbyClosed(int clientIndex, string lobbyId)
    {
        if (mLobby != null &&
            mLobby.LobbyId == lobbyId)
        {
            Close();
        }
    }

    private void OnLobbyCreated(int clientIndex, string lobbyId, RuyiNetLobby lobby)
    {
        if (mLobby == null)
        {
            mLobby = lobby;
        }
    }

    private void OnLobbyDestroyed(int clientIndex, string lobbyId)
    {
        if (mLobby != null &&
            mLobby.LobbyId == lobbyId)
        {
            Close();
        }
    }

    private void OnLobbyGameStarted(int clientIndex, string lobbyId, string connectionString)
    {
        if (mLobby != null &&
            mLobby.LobbyId == lobbyId)
        {
            mConnectionString = connectionString;
            if (RuyiNet.ActivePlayer.profileId != mLobby.OwnerPlayerId)
            {
                var networkManager = FindObjectOfType<MyNetworkManager>();
                if (networkManager != null)
                {
                    networkManager.StartMatchMaker();
                    networkManager.matchMaker.ListMatches(0, 20, "", false, 0, 0, OnListMatches);
                }
            }
        }
    }

    private void OnLobbyOpened(int clientIndex, string lobbyId)
    {
        //  No-op
    }

    private void OnLobbyPlayerJoined(int clientIndex, string lobbyId, string playerId)
    {
        mLobby.MemberPlayerIds.Add(playerId);
        UpdateLobbyInfo(mLobby);
    }

    private void OnLobbyPlayerLeft(int clientIndex, string lobbyId, string playerId)
    {
        mLobby.MemberPlayerIds.Remove(playerId);
        UpdateLobbyInfo(mLobby);
    }

    RuyiNetLobby mLobby;
    Dictionary<string, string> mPlayerIdsToLobbyIds;
    string mConnectionString;
}
