using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Fleeted.Discord;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public class DiscordIntegration : MonoBehaviour
{
    public enum Activities
    {
        InitialActivity,
        InMenu,
        InLobby,
        InSinglePlayerGame,
    }

    public const long discordClientID = 1126986351812812855;

    public long startSessionTime;

    private ActivityManager _activityManager;

    private TimedAction _activityUpdate = new(5f);
    private Discord.Discord _discord;

    private void Start()
    {
        try
        {
            _discord = new Discord.Discord(discordClientID, (UInt64) CreateFlags.NoRequireDiscord);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Failed to initialize Discord pipe:\n{ex}");
            return;
        }

        Plugin.Logger.LogInfo("Discord Instance created");
        startSessionTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        _activityManager = _discord.GetActivityManager();

        _activityManager.OnActivityJoin += secret =>
        {
            secret = secret.Replace("_join", "");

            Plugin.Logger.LogInfo($"OnJoin {secret}");
            var lobbyID = ulong.Parse(secret);

            if (InGameNetManager.Instance.inGame)
            {
                var rcInstance = ResultsController.resultsController;

                //rcInstance.Exit();
                typeof(ResultsController).GetMethod("Exit", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rcInstance, null);
            }
            else
            {
                Plugin.Logger.LogInfo("Already Out Of Game");
            }

            StartCoroutine(CustomLobbyMenu.Instance.TransitionToLobby(lobbyID));
        };

        _activityManager.OnActivityJoinRequest += (ref User user) =>
        {
            Plugin.Logger.LogInfo($"OnJoinRequest {user.Username} {user.Id}");
        };


        StartCoroutine(StartActivities());
        _activityUpdate.Start();
    }

    private void FixedUpdate()
    {
        if (_discord == null)
            return;

        _discord.RunCallbacks();

        if (_activityUpdate.TrueDone())
        {
            ChangeActivityDynamically();
            _activityUpdate.Start();
        }
    }

    private IEnumerator StartActivities()
    {
        UpdateActivity(_discord, Activities.InitialActivity);
        yield return new WaitUntil(() => GlobalController.globalController.screen == GlobalController.screens.mainmenu);
        UpdateActivity(_discord, Activities.InMenu);
    }

    private void ChangeActivityDynamically()
    {
        var isInGame = InGameNetManager.Instance.inGame;
        var isInLobby = LobbyManager.Instance.inLobby;

        if (isInGame && !isInLobby)
        {
            UpdateActivity(_discord, Activities.InSinglePlayerGame, true);
        }
        else if (isInLobby)
        {
            var currentLobbyMembers = LobbyManager.Instance.CurrentLobby.MemberCount +
                                      LobbyManager.Instance.Players.Values.Count(player => player.IsBot);
            var currentLobbyMemberCap = LobbyManager.Instance.CurrentLobby.MaxMembers;
            var settings = LobbyManager.Instance.Settings;


            var lobbyID = LobbyManager.Instance.CurrentLobby.Id;
            if (!isInGame) // Waiting in Lobby
            {
                UpdateActivity(_discord, Activities.InLobby, false, settings.Mode.ToString(), settings.Stage.ToString(),
                    currentLobbyMembers, currentLobbyMemberCap, lobbyID.ToString());
            }
            else // Playing in a Lobby
            {
                UpdateActivity(_discord, Activities.InLobby, true, settings.Mode.ToString(), settings.Stage.ToString(),
                    currentLobbyMembers, currentLobbyMemberCap, lobbyID.ToString());
            }
        }
        else // Left the lobby
        {
            UpdateActivity(_discord, Activities.InMenu);
        }
    }

    private void UpdateActivity(Discord.Discord discord, Activities activity, bool inGame = false,
        string gameMode = "None", string stage = "None", int currentPlayers = 1, int maxPlayers = 2,
        string lobbyID = "None")
    {
        var activityManager = discord.GetActivityManager();
        var activityPresence = new Activity();

        switch (activity)
        {
            case Activities.InitialActivity:
                activityPresence = new Activity()
                {
                    State = "Just Started Playing",
                    Assets =
                    {
                        LargeImage = "shipped_img_1",
                        LargeText = "Shipped",
                    },
                    Instance = true,
                };
                break;
            case Activities.InMenu:
                activityPresence = new Activity()
                {
                    State = "Waiting In Menu",
                    Assets =
                    {
                        LargeImage = "shipped_img_1",
                        LargeText = "Shipped",
                    },
                    Instance = true,
                };
                break;
            case Activities.InLobby:
                var state = inGame ? "Playing Multiplayer" : "Waiting In Lobby";
                activityPresence = new Activity()
                {
                    State = state,
                    Details = $"Game Mode: {gameMode}",
                    Timestamps =
                    {
                        Start = startSessionTime,
                    },
                    Assets =
                    {
                        LargeImage = "shipped_img_1",
                        LargeText = "Shipped",
                        SmallImage = "shipped_img_2",
                        SmallText = stage,
                    },
                    Party =
                    {
                        Id = lobbyID,
                        Size =
                        {
                            CurrentSize = currentPlayers,
                            MaxSize = maxPlayers,
                        },
                    },
                    Secrets =
                    {
                        Join = lobbyID + "_join",
                    },
                    Instance = true,
                };
                break;
            case Activities.InSinglePlayerGame:
                activityPresence = new Activity()
                {
                    State = "Playing Singleplayer",
                    Timestamps =
                    {
                        Start = startSessionTime,
                    },
                    Assets =
                    {
                        LargeImage = "shipped_img_1",
                        LargeText = "Shipped",
                    },
                    Instance = true,
                };
                break;
        }

        activityManager.UpdateActivity(activityPresence, result =>
        {
            if (result != Result.Ok)
            {
                Plugin.Logger.LogError($"Update discord activity error {result}");
            }
        });
    }
}