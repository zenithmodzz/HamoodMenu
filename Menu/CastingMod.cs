/*
 * HamoodMenu  Menu/CastingMod.cs
 * Futuristic casting panel: lists everyone in the room, click a name to
 * spectate them in first or third person with smooth rig lerp.
 *
 * Copyright (C) 2026  HamoodMenu
 * https://github.com/Seralyth/Seralyth-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Photon.Pun;
using Photon.Realtime;
using Seralyth.Classes.Menu;
using Seralyth.Managers;
using System.Linq;
using UnityEngine;
using static Seralyth.Menu.Main;

namespace Seralyth.Menu
{
    public class CastingMod : MonoBehaviour
    {
        public static CastingMod Instance;
        public static bool visible = true;

        private const int WindowId = 0xCA57;
        private Rect windowRect = new Rect(10f, 660f, 340f, 380f);

        private Vector2 playerScroll;
        private bool stylesBuilt;
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle nameStyle;
        private GUIStyle nameActiveStyle;
        private GUIStyle modeStyle;
        private GUIStyle modeActiveStyle;
        private GUIStyle stopStyle;

        private Texture2D bgTex;
        private Texture2D panelTex;
        private Texture2D accentTex;

        private bool spectating;
        private int targetActor = -1;
        private bool firstPerson = true;
        private bool snapped;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                visible = !visible;

            FollowSpectateTarget();
        }

        private static Texture2D MakeTex(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void BuildStyles()
        {
            stylesBuilt = true;

            bgTex = MakeTex(new Color(0.008f, 0.03f, 0.055f, 0.94f));
            panelTex = MakeTex(new Color(0.02f, 0.07f, 0.12f, 0.95f));
            accentTex = MakeTex(new Color(0f, 0.9f, 1f, 1f));

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = bgTex, textColor = new Color(0.62f, 0.96f, 1f) },
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(10, 10, 10, 10),
                fontSize = 13
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 0.95f, 1f) },
                alignment = TextAnchor.MiddleLeft
            };

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.35f, 0.65f, 0.8f) },
                alignment = TextAnchor.MiddleLeft
            };

            nameStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { background = panelTex, textColor = new Color(0.85f, 0.95f, 1f) },
                hover = { background = panelTex, textColor = Color.white },
                active = { background = panelTex, textColor = Color.white },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 4, 4)
            };

            nameActiveStyle = new GUIStyle(nameStyle)
            {
                normal = { background = MakeTex(new Color(0f, 0.35f, 0.48f, 0.95f)), textColor = Color.white },
                hover = { background = MakeTex(new Color(0f, 0.35f, 0.48f, 0.95f)), textColor = Color.white },
                fontStyle = FontStyle.Bold
            };

            modeStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTex(new Color(0.12f, 0.16f, 0.22f, 1f)), textColor = new Color(0.55f, 0.8f, 0.9f) },
                hover = { background = MakeTex(new Color(0.16f, 0.22f, 0.3f, 1f)), textColor = Color.white },
                active = { background = MakeTex(new Color(0.16f, 0.22f, 0.3f, 1f)), textColor = Color.white }
            };

            modeActiveStyle = new GUIStyle(modeStyle)
            {
                normal = { background = MakeTex(new Color(0f, 0.4f, 0.55f, 1f)), textColor = Color.white },
                hover = { background = MakeTex(new Color(0f, 0.4f, 0.55f, 1f)), textColor = Color.white }
            };

            stopStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTex(new Color(0.45f, 0.12f, 0.12f, 1f)), textColor = new Color(1f, 0.6f, 0.6f) },
                hover = { background = MakeTex(new Color(0.55f, 0.15f, 0.15f, 1f)), textColor = Color.white },
                active = { background = MakeTex(new Color(0.55f, 0.15f, 0.15f, 1f)), textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            if (!stylesBuilt)
                BuildStyles();

            float height = Mathf.Min(420f, Screen.height - 20f);
            windowRect.height = height;
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Screen.height - height);

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "", windowStyle);
        }

        private void DrawWindow(int id)
        {
            Color pulse = Color.HSVToRGB((Time.time * 0.08f) % 1f, 0.85f, 1f);
            Color oldColor = GUI.color;
            GUI.color = pulse;
            GUILayout.Box(accentTex, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
            GUI.color = oldColor;

            GUILayout.Label("CASTING MOD", headerStyle);
            GUILayout.Label(spectating ? "SPECTATING " + CurrentTargetName() : "SELECT A PLAYER", subHeaderStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("FIRST", firstPerson ? modeActiveStyle : modeStyle, GUILayout.Height(26f)))
                firstPerson = true;
            if (GUILayout.Button("THIRD", !firstPerson ? modeActiveStyle : modeStyle, GUILayout.Height(26f)))
                firstPerson = false;
            if (spectating && GUILayout.Button("STOP", stopStyle, GUILayout.Height(26f)))
                StopSpectating();
            GUILayout.EndHorizontal();

            playerScroll = GUILayout.BeginScrollView(playerScroll);

            Player[] players;
            try { players = PhotonNetwork.PlayerList; }
            catch { players = null; }

            if (players == null || players.Length == 0)
            {
                GUILayout.Label("// NO SIGNAL - NOT IN A ROOM", subHeaderStyle);
            }
            else
            {
                int localActor = -1;
                try { localActor = PhotonNetwork.LocalPlayer.ActorNumber; } catch { }

                foreach (Player player in players)
                {
                    if (player == null)
                        continue;

                    string name = NoRichtextTags(player.NickName ?? ("PLAYER_" + player.ActorNumber));
                    bool isSelf = player.ActorNumber == localActor;
                    bool isTarget = spectating && player.ActorNumber == targetActor;

                    if (GUILayout.Button((isTarget ? "> " : "") + name + (isSelf ? " (YOU)" : ""), isTarget ? nameActiveStyle : nameStyle, GUILayout.Height(26f)))
                    {
                        if (!isSelf)
                            StartSpectating(player.ActorNumber);
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Label("[F9] TOGGLE CASTING", subHeaderStyle);

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
        }

        private string CurrentTargetName()
        {
            try
            {
                Player player = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(targetActor);
                if (player != null)
                    return NoRichtextTags(player.NickName ?? ("PLAYER_" + targetActor)).ToUpper() + (firstPerson ? " (FIRST)" : " (THIRD)");
            }
            catch { }
            return "UNKNOWN";
        }

        public static void StartSpectating(int actorNumber)
        {
            if (Instance == null)
                return;

            // Fail fast with a reason instead of starting then dying silent.
            VRRig check = Instance.ResolveRig(actorNumber);
            if (check == null)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=red>CAST</color><color=grey>]</color> Can't find that player's rig yet. Try again in a second.");
                return;
            }
            Transform view = Instance.GetViewTransform();
            if (view == null)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=red>CAST</color><color=grey>]</color> Can't find your camera. Are you spawned in?");
                return;
            }

            Instance.targetActor = actorNumber;
            Instance.spectating = true;
            Instance.snapped = false;
            Instance.SaveMyView();
            NotificationManager.SendNotification("<color=grey>[</color><color=green>CAST</color><color=grey>]</color> Casting " + NoRichtextTags(Seralyth.Extensions.VRRigExtensions.GetName(check)) + ". STOP to get your eyes back.");
        }

        public static void StopSpectating()
        {
            if (Instance == null)
                return;

            Instance.spectating = false;
            Instance.targetActor = -1;
            Instance.RestoreMyView();
        }

        private Vector3 savedCamPos;
        private Quaternion savedCamRot;
        private Transform viewTransform;

        private Transform GetViewTransform()
        {
            try
            {
                if (viewTransform != null) return viewTransform;
                Camera main = Camera.main;
                if (main != null) { viewTransform = main.transform; return viewTransform; }
                GameObject go = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera");
                if (go != null) { viewTransform = go.transform; return viewTransform; }
            }
            catch { }
            return null;
        }

        private void SaveMyView()
        {
            try
            {
                Transform view = GetViewTransform();
                if (view == null) return;
                savedCamPos = view.position;
                savedCamRot = view.rotation;
            }
            catch { }
        }

        private void RestoreMyView()
        {
            try
            {
                viewTransform = null;
                Transform view = GetViewTransform();
                if (view == null) return;
                // Body moved on while we looked elsewhere: re-anchor view to self.
                Transform head = null;
                try { head = GorillaTagger.Instance.headCollider.transform; } catch { }
                if (head != null)
                {
                    view.position = head.position;
                    view.rotation = head.rotation;
                }
                else
                {
                    view.position = savedCamPos;
                    view.rotation = savedCamRot;
                }
                snapped = false;
            }
            catch { }
        }

        // Leave-GUI controls, callable from menu buttons.

        public static void ShowCasting() => visible = true;

        public static void HideCasting()
        {
            StopSpectating();
            visible = false;
        }

        public static void LeaveCasting()
        {
            StopSpectating();
            visible = false;
            if (Instance != null)
                Instance.windowRect = new Rect(10f, 660f, 340f, 380f);
        }

        public static void ToggleCasting() => visible = !visible;

        private static NetPlayer NetPlayerFromActor(int actorNumber)
        {
            try
            {
                foreach (NetPlayer p in NetworkSystem.Instance.AllNetPlayers)
                {
                    if (p != null && p.ActorNumber == actorNumber)
                        return p;
                }
            }
            catch { }
            return null;
        }

        private VRRig ResolveRig(int actorNumber)
        {
            try
            {
                // Path 1: direct manager lookup.
                NetPlayer netPlayer = NetPlayerFromActor(actorNumber);
                VRRig rig = netPlayer != null ? Console.GetVRRigFromPlayer(netPlayer) : null;
                if (rig != null) return rig;

                // Path 2: the list the whole menu trusts, matched by actor.
                foreach (VRRig r in Seralyth.Extensions.VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (r == null) continue;
                        NetPlayer p = r.Creator;
                        if (p == null)
                        {
                            try { p = Seralyth.Utilities.RigUtilities.GetPlayerFromVRRig(r); } catch { }
                        }
                        if (p != null && p.ActorNumber == actorNumber)
                            return r;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private void FollowSpectateTarget()
        {
            if (!spectating)
                return;

            try
            {
                VRRig rig = ResolveRig(targetActor);
                if (rig == null)
                {
                    StopSpectating();
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>CAST</color><color=grey>]</color> Lost their rig. Eyes back.");
                    return;
                }

                Transform view = GetViewTransform();
                if (view == null)
                {
                    StopSpectating();
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>CAST</color><color=grey>]</color> Lost your camera. Eyes back.");
                    return;
                }

                if (firstPerson)
                {
                    // Sit slightly in front of their face: outside the skull,
                    // matching their gaze. Never inside geometry, never void.
                    Transform head = rig.headMesh.transform;
                    Vector3 eyePos = head.position + head.forward * 0.15f + Vector3.up * 0.05f;
                    if (!snapped)
                    {
                        view.position = eyePos;
                        view.rotation = head.rotation;
                        snapped = true;
                    }
                    else
                    {
                        float t = 1f - Mathf.Exp(-12f * Time.deltaTime);
                        view.position = Vector3.Lerp(view.position, eyePos, t);
                        view.rotation = Quaternion.Slerp(view.rotation, head.rotation, t);
                    }
                }
                else
                {
                    Vector3 anchor = rig.transform.position;
                    Vector3 flatForward = rig.headMesh.transform.forward;
                    flatForward.y = 0f;
                    if (flatForward.sqrMagnitude < 0.001f)
                        flatForward = Vector3.forward;
                    flatForward.Normalize();

                    Vector3 desired = anchor - flatForward * 4f + Vector3.up * 2f;

                    if (!snapped)
                    {
                        view.position = desired;
                        snapped = true;
                    }
                    else
                    {
                        float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
                        view.position = Vector3.Lerp(view.position, desired, t);
                    }

                    Vector3 lookTarget = rig.headMesh.transform.position;
                    Vector3 lookDir = lookTarget - view.position;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(lookDir);
                        float t = 1f - Mathf.Exp(-5f * Time.deltaTime);
                        view.rotation = Quaternion.Slerp(view.rotation, lookRot, t);
                    }
                }
            }
            catch (System.Exception e)
            {
                LogManager.LogError($"Casting follow errored: {e.Message}");
                StopSpectating();
            }
        }
    }
}
