/*
 * HamoodMenu  Managers/PresenceManager.cs
 * Lobby presence phone-home for the Flask/Vercel backend.
 *
 * User builds heartbeat their lobby + user id + tier every 30s.
 * Owner builds query the backend to find which lobby user builds
 * sit in, then join them through the normal room join path.
 *
 * Set PresenceEndpoint to your Vercel URL and OwnerKey to match
 * the OWNER_KEY env on the backend.
 *
 * Copyright (C) 2026  HamoodMenu
 * https://github.com/Seralyth/Seralyth-Menu
 */

using Photon.Pun;
using GorillaNetworking;
using Seralyth.Classes.Menu;
using Seralyth.Managers;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json.Linq;
using MenuConsole = Seralyth.Classes.Menu.Console;

namespace Seralyth.Managers
{
    public class PresenceManager : MonoBehaviour
    {
        public static PresenceManager instance;

        // Set this to your deployed Vercel URL, e.g.
        // https://hamood-presence.vercel.app
        public static string PresenceEndpoint = "https://hamood-presence.vercel.app";

        // Must match OWNER_KEY env on the backend. Owner build only.
        public static string OwnerKey = "SIGMAMAN";

        private static float nextHeartbeat;

        public void Awake() => instance = this;

        public void Update()
        {
            try
            {
                if (Time.time < nextHeartbeat) return;
                nextHeartbeat = Time.time + 30f;
                if (instance != null)
                    instance.StartCoroutine(SendHeartbeat());
            }
            catch { }
        }

        public static IEnumerator SendHeartbeat()
        {
            string userId = "";
            string nick = "?";
            try
            {
                if (PhotonNetwork.LocalPlayer != null)
                {
                    userId = PhotonNetwork.LocalPlayer.UserId ?? "";
                    nick = PhotonNetwork.LocalPlayer.NickName ?? "?";
                }
            }
            catch { }
            if (string.IsNullOrEmpty(userId)) yield break;

            string lobby = "offline";
            try { lobby = PhotonNetwork.CurrentRoom?.Name ?? "offline"; } catch { }

            JObject body = new JObject
            {
                ["user_id"] = userId,
                ["nickname"] = nick,
                ["lobby"] = lobby,
                ["tier"] = MenuTierSystem.TierTag,
                ["version"] = PluginInfo.Version
            };

            using (UnityWebRequest req = new UnityWebRequest(PresenceEndpoint + "/presence/heartbeat", "POST"))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body.ToString());
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();
            }
        }

        // Owner: who is in this lobby code per the backend?
        public static IEnumerator QueryLobby(string code, Action<string> onDone)
        {
            string url = PresenceEndpoint + "/presence/lobby/" + code.ToUpper();
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("X-Owner-Key", OwnerKey);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    onDone?.Invoke(req.downloadHandler.text);
                else
                    onDone?.Invoke(null);
            }
        }

        // Owner: full presence map, grouped by lobby.
        public static IEnumerator QueryAll(Action<string> onDone)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(PresenceEndpoint + "/presence/all"))
            {
                req.SetRequestHeader("X-Owner-Key", OwnerKey);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    onDone?.Invoke(req.downloadHandler.text);
                else
                    onDone?.Invoke(null);
            }
        }

        public static void FindUsersInCurrentLobby()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                string code = PhotonNetwork.CurrentRoom?.Name ?? "";
                if (string.IsNullOrEmpty(code))
                {
                    MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Join a room first.");
                    return;
                }
                if (instance == null) return;
                instance.StartCoroutine(QueryLobby(code, json =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(json))
                        {
                            MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend unreachable. Check PresenceEndpoint.");
                            return;
                        }
                        JObject root = JObject.Parse(json);
                        if (root.Value<bool?>("ok") != true)
                        {
                            MenuConsole.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend: {root.Value<string>("error") ?? "refused"}.");
                            return;
                        }
                        JToken dataTok = root["data"];
                        if (dataTok == null || dataTok.Type != JTokenType.Object)
                        {
                            MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend sent bad shape.");
                            return;
                        }
                        JObject data = (JObject)dataTok;
                        int count = data.Value<int?>("count") ?? 0;
                        JArray users = data["users"] as JArray;
                        if (count == 0 || users == null)
                        {
                            MenuConsole.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Backend sees nobody in {code}.");
                            return;
                        }
                        var names = new System.Collections.Generic.List<string>();
                        foreach (JToken u in users)
                        {
                            if (u == null || u.Type != JTokenType.Object) continue;
                            names.Add((u.Value<string>("nickname") ?? "?") + $" [{u.Value<string>("tier") ?? "user"}]");
                        }
                        MenuConsole.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Backend: {count} in {code}: " + string.Join(", ", names), 6000);
                    }
                    catch { }
                }));
            }
            catch { }
        }

        public static void FindUsersEverywhere()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (instance == null) return;
                instance.StartCoroutine(QueryAll(json =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(json))
                        {
                            MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend unreachable. Check PresenceEndpoint.");
                            return;
                        }
                        JObject rootAll = JObject.Parse(json);
                        if (rootAll.Value<bool?>("ok") != true)
                        {
                            MenuConsole.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend: {rootAll.Value<string>("error") ?? "refused"}.");
                            return;
                        }
                        JToken dataTok = rootAll["data"];
                        if (dataTok == null || dataTok.Type != JTokenType.Object)
                        {
                            MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend sent bad shape.");
                            return;
                        }
                        JObject data = (JObject)dataTok;
                        int count = data.Value<int?>("count") ?? 0;
                        JObject lobbies = data["lobbies"] as JObject;
                        if (lobbies == null)
                        {
                            MenuConsole.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Backend sent bad shape.");
                            return;
                        }
                        var parts = new System.Collections.Generic.List<string>();
                        foreach (var kv in lobbies.Properties())
                            parts.Add($"{kv.Name} ({(kv.Value as JArray)?.Count ?? 0})");
                        MenuConsole.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Backend tracking {count}: " + string.Join(", ", parts), 8000);
                    }
                    catch { }
                }));
            }
            catch { }
        }

        public static void JoinUserLobby(string code)
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (string.IsNullOrEmpty(code)) return;
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(code.ToUpper(), GorillaNetworking.JoinType.Solo);
            }
            catch { }
        }
    }
}
