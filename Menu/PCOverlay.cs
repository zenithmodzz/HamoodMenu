/*
 * HamoodMenu  Menu/PCOverlay.cs
 * Futuristic PC overlay control panel for HamoodMenu with every mod.
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
using Seralyth.Classes.Menu;
using Seralyth.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using static Seralyth.Menu.Main;

namespace Seralyth.Menu
{
    public class PCOverlay : MonoBehaviour
    {
        public static PCOverlay Instance;
        public static bool visible = true;

        private const int WindowId = 0x7A00D;
        private Rect windowRect = new Rect(10f, 10f, 920f, 640f);

        private string search = "";
        private int categoryIndex;
        private Vector2 categoryScroll;
        private Vector2 modScroll;

        private bool stylesBuilt;
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle categoryStyle;
        private GUIStyle categoryActiveStyle;
        private GUIStyle modNameStyle;
        private GUIStyle onStyle;
        private GUIStyle offStyle;
        private GUIStyle runStyle;
        private GUIStyle searchStyle;
        private GUIStyle statusStyle;

        private Texture2D bgTex;
        private Texture2D panelTex;
        private Texture2D accentTex;

        private void Awake()
        {
            Instance = this;
            SetVisible(visible);
        }

        private static bool cursorFreedByUs;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                SetVisible(!visible);

            EnforceCursor();
        }

        private static void EnforceCursor()
        {
            try
            {
                bool wantCursor = visible;
                try { wantCursor = wantCursor || !XRSettings.isDeviceActive; } catch { }
                if (wantCursor)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    cursorFreedByUs = true;
                }
                else if (cursorFreedByUs)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    cursorFreedByUs = false;
                }
            }
            catch { }
        }

        public static void SetVisible(bool value)
        {
            visible = value;

            try
            {
                if (visible)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            catch { }
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
                fontSize = 20,
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

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.55f, 0.85f, 0.95f) },
                alignment = TextAnchor.MiddleRight
            };

            categoryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { background = panelTex, textColor = new Color(0.55f, 0.8f, 0.9f) },
                hover = { background = panelTex, textColor = Color.white },
                active = { background = panelTex, textColor = Color.white },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 4, 4)
            };

            categoryActiveStyle = new GUIStyle(categoryStyle)
            {
                normal = { background = MakeTex(new Color(0f, 0.35f, 0.48f, 0.95f)), textColor = Color.white },
                hover = { background = MakeTex(new Color(0f, 0.35f, 0.48f, 0.95f)), textColor = Color.white },
                fontStyle = FontStyle.Bold
            };

            modNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.95f, 1f) },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };

            onStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTex(new Color(0f, 0.45f, 0.3f, 1f)), textColor = new Color(0.4f, 1f, 0.6f) },
                hover = { background = MakeTex(new Color(0f, 0.55f, 0.36f, 1f)), textColor = Color.white },
                active = { background = MakeTex(new Color(0f, 0.55f, 0.36f, 1f)), textColor = Color.white }
            };

            offStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTex(new Color(0.12f, 0.16f, 0.22f, 1f)), textColor = new Color(0.45f, 0.55f, 0.65f) },
                hover = { background = MakeTex(new Color(0.16f, 0.22f, 0.3f, 1f)), textColor = Color.white },
                active = { background = MakeTex(new Color(0.16f, 0.22f, 0.3f, 1f)), textColor = Color.white }
            };

            runStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = MakeTex(new Color(0f, 0.4f, 0.55f, 1f)), textColor = new Color(0.5f, 0.95f, 1f) },
                hover = { background = MakeTex(new Color(0f, 0.5f, 0.68f, 1f)), textColor = Color.white },
                active = { background = MakeTex(new Color(0f, 0.5f, 0.68f, 1f)), textColor = Color.white }
            };

            searchStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                normal = { background = panelTex, textColor = Color.white },
                hover = { background = panelTex, textColor = Color.white },
                active = { background = panelTex, textColor = Color.white },
                focused = { background = panelTex, textColor = Color.white },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 5, 5)
            };
        }

        private void OnGUI()
        {
            EnforceCursor();

            if (!visible)
                return;

            if (!stylesBuilt)
                BuildStyles();

            float width = Mathf.Min(960f, Screen.width - 20f);
            float height = Mathf.Min(680f, Screen.height - 20f);
            windowRect.width = width;
            windowRect.height = height;
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Screen.width - width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Screen.height - height);

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "", windowStyle);
        }

        private void DrawWindow(int id)
        {
            if (Buttons.buttons == null || Buttons.categoryNames == null)
            {
                GUILayout.Label("// INITIALIZING NEURAL LINK...", subHeaderStyle);
                GUI.DragWindow();
                return;
            }

            Color pulse = Color.HSVToRGB((Time.time * 0.08f) % 1f, 0.85f, 1f);
            Color oldColor = GUI.color;

            GUI.color = pulse;
            GUILayout.Box(accentTex, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
            GUI.color = oldColor;

            GUILayout.BeginHorizontal();
            GUILayout.Label("HAMOODMENU", headerStyle);
            GUILayout.Label("// PC OVERLAY v" + PluginInfo.Version, subHeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(BuildStatusText(), statusStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("SEARCH >", modNameStyle, GUILayout.Width(80f));
            search = GUILayout.TextField(search ?? "", searchStyle);
            if (GUILayout.Button("X", runStyle, GUILayout.Width(32f)))
                search = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            categoryScroll = GUILayout.BeginScrollView(categoryScroll, GUILayout.Width(210f));
            for (int i = 0; i < Buttons.categoryNames.Length; i++)
            {
                string name = Buttons.categoryNames[i] ?? ("CAT_" + i);
                bool active = string.IsNullOrEmpty(search) && i == categoryIndex;
                if (GUILayout.Button("> " + NoRichtextTags(name).ToUpper(), active ? categoryActiveStyle : categoryStyle, GUILayout.Height(26f)))
                {
                    categoryIndex = i;
                    modScroll = Vector2.zero;
                }
            }
            GUILayout.EndScrollView();

            modScroll = GUILayout.BeginScrollView(modScroll);
            DrawModList();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();

            GUILayout.Label("[F8] TOGGLE OVERLAY  //  CLICK A MOD TO TOGGLE OR EXECUTE", subHeaderStyle);

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
        }

        private string BuildStatusText()
        {
            int fps = Time.deltaTime > 0f ? Mathf.RoundToInt(1f / Time.deltaTime) : 0;
            string room;
            try
            {
                room = NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom && PhotonNetwork.CurrentRoom != null
                    ? PhotonNetwork.CurrentRoom.Name
                    : "OFFLINE";
            }
            catch { room = "OFFLINE"; }

            int enabled = 0;
            try
            {
                enabled = Buttons.buttons.SelectMany(list => list).Count(button => button != null && button.enabled);
            }
            catch { }

            return $"FPS {fps}  //  ROOM {room}  //  ACTIVE {enabled}";
        }

        private void DrawModList()
        {
            List<(ButtonInfo button, string category)> rows = new List<(ButtonInfo, string)>();

            if (!string.IsNullOrEmpty(search))
            {
                string query = search.ToLower();
                for (int c = 0; c < Buttons.buttons.Length && c < Buttons.categoryNames.Length; c++)
                {
                    foreach (ButtonInfo button in Buttons.buttons[c])
                    {
                        if (button == null || button.buttonText == null)
                            continue;

                        string haystack = (button.buttonText + " " + (button.overlapText ?? "") + " " + (button.aliases != null ? string.Join(" ", button.aliases) : "")).ToLower();
                        if (haystack.Contains(query))
                            rows.Add((button, Buttons.categoryNames[c]));
                    }
                }
            }
            else
            {
                if (categoryIndex < 0 || categoryIndex >= Buttons.buttons.Length)
                    categoryIndex = 0;

                foreach (ButtonInfo button in Buttons.buttons[categoryIndex])
                {
                    if (button == null)
                        continue;
                    rows.Add((button, null));
                }
            }

            if (rows.Count == 0)
            {
                GUILayout.Label("// NO MODULES FOUND", subHeaderStyle);
                return;
            }

            foreach ((ButtonInfo button, string category) in rows)
            {
                string display = NoRichtextTags(button.overlapText ?? button.buttonText);
                if (!string.IsNullOrEmpty(category))
                    display = $"[{NoRichtextTags(category)}] {display}";

                if (button.label)
                {
                    GUILayout.Label("/// " + display.ToUpper(), subHeaderStyle);
                    continue;
                }

                GUILayout.BeginHorizontal();

                if (button.detected)
                    GUILayout.Label("[!]", runStyle, GUILayout.Width(28f), GUILayout.Height(24f));

                GUILayout.Label(display, modNameStyle);
                GUILayout.FlexibleSpace();

                if (button.isTogglable)
                {
                    if (GUILayout.Button(button.enabled ? "ON" : "OFF", button.enabled ? onStyle : offStyle, GUILayout.Width(64f), GUILayout.Height(24f)))
                        Activate(button);
                }
                else
                {
                    if (GUILayout.Button("RUN", runStyle, GUILayout.Width(64f), GUILayout.Height(24f)))
                        Activate(button);
                }

                GUILayout.EndHorizontal();
            }
        }

        private static void Activate(ButtonInfo button)
        {
            if (button == null)
                return;

            if (button.detected && !allowDetected)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> This mod is detected and requires permission to run.");
                return;
            }

            try
            {
                Toggle(button.buttonText);
            }
            catch (System.Exception e)
            {
                LogManager.LogError($"PCOverlay failed to toggle {button.buttonText}: {e.Message}");
            }
        }
    }
}
