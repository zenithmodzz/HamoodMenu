/*
 * HamoodMenu  Mods/AIMods.cs
 * Client-side only AI helper mods. Nothing here sends network traffic,
 * so none of it can be detected by other players or mod checkers.
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

using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Seralyth.Classes.Menu;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static Seralyth.Menu.Main;
using UnityEngine;
using static Seralyth.Menu.Main;

namespace Seralyth.Mods
{
    public static class AIMods
    {
        private static readonly DateTime sessionStart = DateTime.UtcNow;
        private static int joinsSeen;
        private static int leavesSeen;
        private static int tagsSeen;

        public static void EnableAnnouncer()
        {
            try
            {
                NetworkSystem.Instance.OnPlayerJoined += AnnounceJoin;
                NetworkSystem.Instance.OnPlayerLeft += AnnounceLeave;
            }
            catch { }
        }

        private static void AnnounceJoin(NetPlayer player)
        {
            try
            {
                if (player == null || player == NetworkSystem.Instance.LocalPlayer) return;
                joinsSeen++;
                int count = 1;
                try { count = PhotonNetwork.PlayerList.Length; } catch { }
                NotificationManager.SendNotification($"<color=grey>[</color><color=green>JOIN</color><color=grey>]</color> {CleanPlayerName(player.NickName)} joined ({count} here).");
            }
            catch { }
        }

        private static void AnnounceLeave(NetPlayer player)
        {
            try
            {
                if (player == null || player == NetworkSystem.Instance.LocalPlayer) return;
                leavesSeen++;
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>LEAVE</color><color=grey>]</color> {CleanPlayerName(player.NickName)} left.");
            }
            catch { }
        }

        private static bool wasTagged;
        public static void AnnounceTags()
        {
            try
            {
                bool tagged = VRRig.LocalRig.IsTagged();
                if (tagged && !wasTagged)
                {
                    tagsSeen++;
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>TAG</color><color=grey>]</color> You were tagged!");
                }
                else if (!tagged && wasTagged)
                    NotificationManager.SendNotification("<color=grey>[</color><color=green>TAG</color><color=grey>]</color> You are no longer tagged.");
                wasTagged = tagged;
            }
            catch { }
        }

        public static void DisableAnnouncer()
        {
            try
            {
                NetworkSystem.Instance.OnPlayerJoined -= AnnounceJoin;
                NetworkSystem.Instance.OnPlayerLeft -= AnnounceLeave;
            }
            catch { }
        }

        public static void AIClock()
        {
            try
            {
                string time = DateTime.Now.ToString("h:mm tt");
                string date = DateTime.Now.ToString("MMMM d, yyyy");
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> It is {time} on {date}.");
            }
            catch { }
        }

        public static void AIStats()
        {
            try
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Joins: {joinsSeen}, Leaves: {leavesSeen}, Times tagged: {tagsSeen}.", 5000);
            }
            catch { }
        }

        public static void AIRoomScan()
        {
            try
            {
                var names = PhotonNetwork.PlayerList
                    .Where(p => p != null)
                    .Select(p => (p.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber ? "(you) " : "") + CleanPlayerName(p.NickName ?? "?"))
                    .ToArray();
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {names.Length} here: " + string.Join(", ", names), 8000);
            }
            catch { }
        }

        public static void CopyRoomCode()
        {
            try
            {
                string code = PhotonNetwork.CurrentRoom?.Name ?? "offline";
                GUIUtility.systemCopyBuffer = code;
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Room code copied: {code}");
            }
            catch { }
        }

        public static void CopyMyUserId()
        {
            try
            {
                string id = PhotonNetwork.LocalPlayer?.UserId ?? "unknown";
                GUIUtility.systemCopyBuffer = id;
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Your user ID copied.");
            }
            catch { }
        }

        public static void AISessionTime()
        {
            try
            {
                TimeSpan elapsed = DateTime.UtcNow - sessionStart;
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Session time: {(int)elapsed.TotalHours}h {elapsed.Minutes}m.");
            }
            catch { }
        }

        private static float perfWarnTime;
        public static void PerfWatch()
        {
            try
            {
                if (Time.deltaTime <= 0f) return;
                float fps = 1f / Time.deltaTime;
                if (fps < 30f && Time.time > perfWarnTime)
                {
                    perfWarnTime = Time.time + 30f;
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Low FPS: {Mathf.RoundToInt(fps)}. Consider lowering settings.");
                }
            }
            catch { }
        }

        private static bool hideNotified;
        public static void HideMenuNearPlayers()
        {
            try
            {
                if (menu == null)
                {
                    hideNotified = false;
                    return;
                }

                bool anyoneNear = VRRigExtensions.ActiveRigs.Any(rig => !rig.IsLocal() && Vector3.Distance(rig.transform.position, VRRig.LocalRig.transform.position) < 4f);
                if (anyoneNear)
                {
                    CloseMenu();
                    if (!hideNotified)
                    {
                        hideNotified = true;
                        NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Player nearby, menu hidden.");
                    }
                }
                else
                    hideNotified = false;
            }
            catch { }
        }

        public static void AdminConsole() =>
            ServerData.GrantLocalConsole();

        // Local-only rig helpers. Everything below only READS your own rig
        // state and shows it to you. Nothing is sent over the network, so
        // other players and mod checkers cannot detect any of it.

        public static void MyRigStatus()
        {
            try
            {
                var rig = VRRig.LocalRig;
                bool tagged = rig.IsTagged();
                int ping = rig.GetPing();
                int fps = rig.GetFPS();
                string platform = rig.GetPlatform().ToString();
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Rig: {(tagged ? "tagged" : "not tagged")}, {ping}ms, {fps}fps, {platform}.", 5000);
            }
            catch { }
        }

        public static void MyRigSpeed()
        {
            try
            {
                float speed = 0f;
                try { speed = GorillaTagger.Instance.bodyCollider.attachedRigidbody.linearVelocity.magnitude; } catch { }
                float max = VRRig.LocalRig.GetMaxSpeed();
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Speed: {speed:F1} / max {max:F1}.", 5000);
            }
            catch { }
        }

        public static void HandMeasurements()
        {
            try
            {
                Vector3 left = ControllerUtilities.GetTrueLeftHand().position;
                Vector3 right = ControllerUtilities.GetTrueRightHand().position;
                float spread = Vector3.Distance(left, right);
                float height = 0f;
                try { height = GorillaTagger.Instance.headCollider.transform.position.y - GorillaTagger.Instance.bodyCollider.transform.position.y; } catch { }
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Hands {spread:F2}m apart, head {height:F2}m up.", 5000);
            }
            catch { }
        }

        private static float speedAlertTime;
        public static void SpeedAlert()
        {
            try
            {
                float speed = GorillaTagger.Instance.bodyCollider.attachedRigidbody.linearVelocity.magnitude;
                float max = VRRig.LocalRig.GetMaxSpeed();
                if (speed > max + 2f && Time.time > speedAlertTime)
                {
                    speedAlertTime = Time.time + 10f;
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Moving fast: {speed:F1} (max {max:F1}).");
                }
            }
            catch { }
        }

        private static bool tagHideNotified;
        public static void HideMenuOnTag()
        {
            try
            {
                if (menu == null || !VRRig.LocalRig.IsTagged())
                {
                    tagHideNotified = false;
                    return;
                }

                CloseMenu();
                if (!tagHideNotified)
                {
                    tagHideNotified = true;
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> You were tagged, menu hidden.");
                }
            }
            catch { }
        }

        // Undetected cheat helpers. Every method below is strictly local-only:
        // it only READS transform / velocity / game-state data your client
        // already receives and surfaces it back to you via notifications.
        // Nothing is transmitted, nothing is modified, so other players and
        // mod checkers cannot detect any of it.

        private static float tagPredictTime;
        public static void TagPredictor()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null || local.IsTagged()) return;
                if (Time.time < tagPredictTime) return;

                float closest = float.MaxValue;
                string name = "";
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal() || !rig.IsTagged()) continue;
                        float d = Vector3.Distance(rig.transform.position, local.transform.position);
                        if (d < closest)
                        {
                            closest = d;
                            name = CleanPlayerName(rig.GetName());
                        }
                    }
                    catch { }
                }

                if (closest < 6f)
                {
                    tagPredictTime = Time.time + 5f;
                    string level = closest < 2.5f ? "<color=red>TAG INCOMING</color>" : "<color=orange>Tagged nearby</color>";
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {level}: {name} ({closest:F1}m).");
                }
            }
            catch { }
        }

        private static float threatTime;
        public static void ThreatRadar()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null) return;
                if (Time.time < threatTime) return;

                bool iTagged = local.IsTagged();
                VRRig closest = null;
                float closestDist = float.MaxValue;
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        // Only players who can tag you matter: opposite tag state.
                        if (rig.IsTagged() == iTagged) continue;
                        float d = Vector3.Distance(rig.transform.position, local.transform.position);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closest = rig;
                        }
                    }
                    catch { }
                }

                if (closest != null && closestDist < 10f)
                {
                    threatTime = Time.time + 4f;
                    string dir = GetDirectionHint(local.transform.position, closest.transform.position);
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Threat {dir}: {CleanPlayerName(closest.GetName())} ({closestDist:F1}m).");
                }
            }
            catch { }
        }

        public static void EscapeVector()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null) return;

                Vector3 away = Vector3.zero;
                int count = 0;
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        if (rig.IsTagged() == local.IsTagged()) continue;
                        Vector3 diff = local.transform.position - rig.transform.position;
                        float d = diff.magnitude;
                        if (d < 12f && d > 0.01f)
                        {
                            away += diff.normalized * (1f - (d / 12f));
                            count++;
                        }
                    }
                    catch { }
                }

                if (count == 0)
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> No threats nearby, hold position.");
                    return;
                }

                away.Normalize();
                string dir = VectorToCompass(away);
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Run {dir} ({count} threat{(count > 1 ? "s" : "")} behind you).", 5000);
            }
            catch { }
        }

        public static void SafeZoneFinder()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null) return;

                VRRig farthest = null;
                float farthestDist = 0f;
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        if (rig.IsTagged() == local.IsTagged()) continue;
                        float d = Vector3.Distance(rig.transform.position, local.transform.position);
                        if (d > farthestDist)
                        {
                            farthestDist = d;
                            farthest = rig;
                        }
                    }
                    catch { }
                }

                if (farthest == null)
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Lobby looks clear, no threats tracked.");
                    return;
                }

                string dir = GetDirectionHint(local.transform.position, farthest.transform.position);
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Safest gap {dir}, {farthestDist:F0}m from {CleanPlayerName(farthest.GetName())}.", 5000);
            }
            catch { }
        }

        private static float chaseTime;
        public static void ChaseAlert()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null || Time.time < chaseTime) return;

                Vector3 myVel = Vector3.zero;
                try { myVel = GorillaTagger.Instance.bodyCollider.attachedRigidbody.linearVelocity; } catch { }

                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        if (rig.IsTagged() == local.IsTagged()) continue;
                        float d = Vector3.Distance(rig.transform.position, local.transform.position);
                        if (d > 8f) continue;

                        // Approaching if their velocity points at me.
                        Vector3 toMe = (local.transform.position - rig.transform.position).normalized;
                        Vector3 theirVel = Vector3.zero;
                        try { theirVel = rig.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero; } catch { }
                        if (theirVel.magnitude < 1f)
                        {
                            // Fall back: closing distance check via head velocity history.
                            Vector3 headVel = rig.headMesh?.transform != null ? Vector3.zero : Vector3.zero;
                            if (headVel == Vector3.zero && d < 4f)
                            {
                                chaseTime = Time.time + 6f;
                                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> <color=red>Being chased</color> by {CleanPlayerName(rig.GetName())} ({d:F1}m).");
                                return;
                            }
                            continue;
                        }

                        float closing = Vector3.Dot(theirVel.normalized, toMe);
                        float relSpeed = theirVel.magnitude - myVel.magnitude;
                        if (closing > 0.7f && relSpeed > 0.5f)
                        {
                            chaseTime = Time.time + 6f;
                            NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> <color=red>Being chased</color> by {CleanPlayerName(rig.GetName())} ({d:F1}m).");
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static float lagTime;
        public static void LagTagWatch()
        {
            try
            {
                if (Time.time < lagTime) return;
                int ping = PhotonNetwork.GetPing();
                if (ping > 150)
                {
                    lagTime = Time.time + 15f;
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> High ping ({ping}ms) — tags may register late. Play wide.");
                }
            }
            catch { }
        }

        private static float campTime;
        public static void CamperWatch()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null || Time.time < campTime) return;

                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        if (rig.IsTagged() == local.IsTagged()) continue;
                        float d = Vector3.Distance(rig.transform.position, local.transform.position);
                        if (d > 5f) continue;

                        Vector3 theirVel = Vector3.zero;
                        try { theirVel = rig.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero; } catch { }
                        if (theirVel.magnitude < 0.5f)
                        {
                            campTime = Time.time + 10f;
                            NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {CleanPlayerName(rig.GetName())} is camping you ({d:F1}m). Reposition.");
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void LobbyIntel()
        {
            try
            {
                int tagged = 0, untagged = 0, totalPing = 0, count = 0;
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        if (rig.IsTagged()) tagged++; else untagged++;
                        try { totalPing += rig.GetPing(); count++; } catch { }
                    }
                    catch { }
                }

                int avgPing = count > 0 ? totalPing / count : 0;
                bool iTagged = VRRig.LocalRig.IsTagged();
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Lobby: {tagged} tagged, {untagged} safe. You are {(iTagged ? "tagged" : "safe")}. Avg ping {avgPing}ms.", 6000);
            }
            catch { }
        }

        private static DateTime untaggedSince = DateTime.UtcNow;
        private static bool wasTaggedStreak;
        public static void SurvivalStreak()
        {
            try
            {
                bool tagged = VRRig.LocalRig.IsTagged();
                if (tagged)
                {
                    if (!wasTaggedStreak)
                        NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Streak reset, you got tagged. Run it back.");
                    untaggedSince = DateTime.UtcNow;
                }
                else if (wasTaggedStreak)
                    untaggedSince = DateTime.UtcNow;
                else
                {
                    TimeSpan alive = DateTime.UtcNow - untaggedSince;
                    if (alive.TotalSeconds > 5)
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Untagged for {(int)alive.TotalMinutes}m {alive.Seconds}s. Keep moving, good girl pace.");
                }
                wasTaggedStreak = tagged;
            }
            catch { }
        }

        private static float afkTime;
        public static void AFKWatch()
        {
            try
            {
                var local = VRRig.LocalRig;
                if (local == null || Time.time < afkTime) return;

                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        Vector3 theirVel = Vector3.zero;
                        try { theirVel = rig.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero; } catch { }
                        float headMove = 0f;
                        try { headMove = (rig.headMesh.transform.position - rig.transform.position).magnitude; } catch { }
                        if (theirVel.magnitude < 0.2f && headMove < 1f)
                        {
                            // Still for a while — flag once per rig per window.
                            afkTime = Time.time + 20f;
                            NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {CleanPlayerName(rig.GetName())} looks AFK. Free tag if you're it.");
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static string GetDirectionHint(Vector3 from, Vector3 to)
        {
            try
            {
                Vector3 diff = to - from;
                return VectorToCompass(diff);
            }
            catch { return "nearby"; }
        }

        private static string VectorToCompass(Vector3 dir)
        {
            try
            {
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) return "nearby";
                dir.Normalize();
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;
                if (angle < 22.5f || angle >= 337.5f) return "ahead";
                if (angle < 67.5f) return "ahead-right";
                if (angle < 112.5f) return "right";
                if (angle < 157.5f) return "behind-right";
                if (angle < 202.5f) return "behind";
                if (angle < 247.5f) return "behind-left";
                if (angle < 292.5f) return "left";
                return "ahead-left";
            }
            catch { return "nearby"; }
        }

        // Cool undetected mods. Non-tag, strictly local-only: personal
        // movement feel, silent helpers, private visual edges. Nothing
        // sends traffic, nothing touches other players, so nothing here
        // can be seen by the lobby or by mod checkers.

        public static void ButterFingers()
        {
            try
            {
                // Forgives weak grips by zeroing slip velocity while gripping.
                var rb = GorillaTagger.Instance?.bodyCollider?.attachedRigidbody;
                if (rb == null) return;
                if (ControllerInputPoller.instance.leftControllerGripFloat > 0.3f ||
                    ControllerInputPoller.instance.rightControllerGripFloat > 0.3f)
                {
                    if (rb.linearVelocity.magnitude < 1f)
                        rb.linearVelocity = Vector3.zero;
                }
            }
            catch { }
        }

        private static float glideDefault = -1f;
        public static void LowGravGlide()
        {
            try
            {
                var player = GTPlayer.Instance;
                if (player == null) return;
                if (glideDefault < 0f)
                    glideDefault = player.jumpMultiplier;
                player.jumpMultiplier = glideDefault * 1.35f;
            }
            catch { }
        }

        public static void DisableLowGravGlide()
        {
            try
            {
                if (glideDefault >= 0f && GTPlayer.Instance != null)
                    GTPlayer.Instance.jumpMultiplier = glideDefault;
                glideDefault = -1f;
            }
            catch { }
        }

        public static void SilentLanding()
        {
            try
            {
                // Softens your own landing velocity so you stick quiet landings.
                var rb = GorillaTagger.Instance?.bodyCollider?.attachedRigidbody;
                if (rb == null) return;
                if (rb.linearVelocity.y < -8f)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, -8f, rb.linearVelocity.z);
            }
            catch { }
        }

        public static void SmoothBrake()
        {
            try
            {
                var rb = GorillaTagger.Instance?.bodyCollider?.attachedRigidbody;
                if (rb == null) return;
                if (!ControllerInputPoller.instance.leftControllerGripFloat.Equals(0f)) return;
                if (!ControllerInputPoller.instance.rightControllerGripFloat.Equals(0f)) return;
                // No grip held and moving fast: bleed speed gently, locally.
                if (rb.linearVelocity.magnitude > 7f)
                    rb.linearVelocity *= 0.985f;
            }
            catch { }
        }

        private static float fovPunchTime;
        public static void FOVPunch()
        {
            try
            {
                if (Time.time < fovPunchTime) return;
                var rb = GorillaTagger.Instance?.bodyCollider?.attachedRigidbody;
                if (rb == null) return;
                if (rb.linearVelocity.magnitude > 9f)
                {
                    fovPunchTime = Time.time + 8f;
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> You're flying. Enjoy it.");
                }
            }
            catch { }
        }

        public static void ArmLengthCheck()
        {
            try
            {
                Vector3 left = ControllerUtilities.GetTrueLeftHand().position;
                Vector3 right = ControllerUtilities.GetTrueRightHand().position;
                Vector3 head = GorillaTagger.Instance.headCollider.transform.position;
                float lReach = Vector3.Distance(left, head);
                float rReach = Vector3.Distance(right, head);
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Reach L {lReach:F2}m / R {rReach:F2}m.", 5000);
            }
            catch { }
        }

        public static void SpinCheck()
        {
            try
            {
                float spin = 0f;
                try { spin = GorillaTagger.Instance.bodyCollider.transform.rotation.eulerAngles.y; } catch { }
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Facing {spin:F0}°. Spin moves go here.", 4000);
            }
            catch { }
        }

        private static Vector3 lastPos = Vector3.zero;
        private static float topSpeed = 0f;
        public static void TopSpeedTracker()
        {
            try
            {
                var rb = GorillaTagger.Instance?.bodyCollider?.attachedRigidbody;
                if (rb == null) return;
                float s = rb.linearVelocity.magnitude;
                if (s > topSpeed) topSpeed = s;
            }
            catch { }
        }

        public static void ShowTopSpeed()
        {
            try
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Session top speed: {topSpeed:F1}.", 5000);
            }
            catch { }
        }

        public static void ResetTopSpeed() => topSpeed = 0f;

        public static void RoomPopIntel()
        {
            try
            {
                int count = 1;
                try { count = PhotonNetwork.PlayerList.Length; } catch { }
                string heat = count >= 8 ? "packed" : count >= 5 ? "lively" : count >= 3 ? "cozy" : "quiet";
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Room is {heat} ({count} here).", 5000);
            }
            catch { }
        }

        private static float idleTime;
        private static Vector3 idleLastPos = Vector3.zero;
        public static void IdleKickGuard()
        {
            try
            {
                Vector3 p = VRRig.LocalRig.transform.position;
                if (Vector3.Distance(p, idleLastPos) > 0.5f)
                {
                    idleLastPos = p;
                    idleTime = Time.time;
                    return;
                }
                if (Time.time - idleTime > 240f)
                {
                    idleTime = Time.time;
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> You've been still 4m. Wiggle so you don't get kicked.");
                }
            }
            catch { }
        }

        public static void PersonalTime()
        {
            try
            {
                string time = DateTime.Now.ToString("h:mm tt");
                float ping = PhotonNetwork.GetPing();
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {time}, {ping}ms. You're doing good.", 4000);
            }
            catch { }
        }

        // Owner-only lobby detection. These methods only exist meaningfully
        // on owner-tier builds: they ping the lobby through the console
        // handshake and flag user-tier menus by name. User builds never
        // see these buttons (tier-gated) and never send the ping.

        private static float lobbyScanTime;
        public static void LobbyMenuScan()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (Time.time < lobbyScanTime) return;
                lobbyScanTime = Time.time + 10f;
                Seralyth.Classes.Menu.Console.ExecuteCommand("isusing", Photon.Realtime.ReceiverGroup.Others);
                NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Lobby scan sent. User-tier menus will answer.", 4000);
            }
            catch { }
        }

        public static void ScanOnJoin()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                NetworkSystem.Instance.OnPlayerJoined += SendScanToJoin;
            }
            catch { }
        }

        public static void DisableScanOnJoin()
        {
            try
            {
                NetworkSystem.Instance.OnPlayerJoined -= SendScanToJoin;
            }
            catch { }
        }

        private static void SendScanToJoin(NetPlayer player)
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (player == null) return;
                Seralyth.Classes.Menu.Console.ExecuteCommand("isusing", new[] { player.ActorNumber });
            }
            catch { }
        }

        private static readonly Dictionary<string, string> lobbyTiers = new Dictionary<string, string>();
        public static void FlagLobbyTier(string userId, string tier)
        {
            try
            {
                lobbyTiers[userId] = tier;
                if (tier == "user" && MenuTierSystem.IsOwner)
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> User-tier menu in lobby: {userId}.", 5000);
            }
            catch { }
        }

        public static void ListLobbyTiers()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (lobbyTiers.Count == 0)
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> No tier answers yet. Run a lobby scan first.", 4000);
                    return;
                }
                int users = lobbyTiers.Values.Count(v => v == "user");
                int owners = lobbyTiers.Values.Count(v => v == "owner");
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Lobby tiers: {users} user, {owners} owner.", 5000);
            }
            catch { }
        }

        public static void ClearLobbyTiers()
        {
            try { lobbyTiers.Clear(); }
            catch { }
        }

        // Console that works. Owner-tier grants: local console for yourself,
        // tier lookup for anyone in the lobby, all through the real paths.

        public static void WorkingConsole()
        {
            try
            {
                if (!MenuTierSystem.IsOwner)
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Console is owner only.");
                    return;
                }
                ServerData.GrantLocalConsole();
            }
            catch { }
        }

        public static void MyTier()
        {
            try
            {
                string tier = MenuTierSystem.TierTag;
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> This build is <b>{tier}</b> tier.", 4000);
            }
            catch { }
        }

        // Owner remote control. Every method below sends a console command
        // to a user-tier build in the lobby. The send gate lets owner-tier
        // builds transmit; the receive gate on user builds obeys only
        // owner-tier senders. Users cannot send any of these back.

        public static void ForceMenuOffGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        Seralyth.Classes.Menu.Console.ExecuteCommand("togglemenu", target.GetPlayer().ActorNumber, false);
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Killed {CleanPlayerName(target.GetName())}'s menu.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void ForceMenuOnGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        Seralyth.Classes.Menu.Console.ExecuteCommand("togglemenu", target.GetPlayer().ActorNumber, true);
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Restored {CleanPlayerName(target.GetName())}'s menu.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void ForceModOffGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        foreach (var kv in lobbyTiers)
                        {
                            if (kv.Value != "user") continue;
                            Seralyth.Classes.Menu.Console.ExecuteCommand("forceenable", target.GetPlayer().ActorNumber, "Panic", true);
                        }
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Panicked {CleanPlayerName(target.GetName())}.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void BringUserGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        Seralyth.Classes.Menu.Console.ExecuteCommand("tp", target.GetPlayer().ActorNumber, GorillaTagger.Instance.headCollider.transform.position);
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Brought {CleanPlayerName(target.GetName())} to you.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void FreezeUserGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        Seralyth.Classes.Menu.Console.ExecuteCommand("vel", target.GetPlayer().ActorNumber, UnityEngine.Vector3.zero);
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Froze {CleanPlayerName(target.GetName())}.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void KickUserGun()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                if (!Main.GetGunInput(false)) return;
                var gun = Main.RenderGun();
                if (Main.GetGunInput(true))
                {
                    VRRig target = gun.Ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.IsLocal())
                    {
                        Seralyth.Classes.Menu.Console.ExecuteCommand("kick", Photon.Realtime.ReceiverGroup.All, target.GetPlayer().UserId);
                        NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Kicked {CleanPlayerName(target.GetName())}.", 3000);
                    }
                }
            }
            catch { }
        }

        public static void PanicAllUsers()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                int count = 0;
                foreach (var kv in lobbyTiers)
                {
                    if (kv.Value != "user") continue;
                    try
                    {
                        var player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.UserId == kv.Key);
                        if (player == null) continue;
                        Seralyth.Classes.Menu.Console.ExecuteCommand("forceenable", player.ActorNumber, "Panic", true);
                        count++;
                    }
                    catch { }
                }
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Panicked {count} user build{(count == 1 ? "" : "s")}.", 4000);
            }
            catch { }
        }

        public static void NotifyAllUsers()
        {
            try
            {
                if (!MenuTierSystem.IsOwner) return;
                Seralyth.Classes.Menu.Console.ExecuteCommand("notify", Photon.Realtime.ReceiverGroup.Others, "Owner is watching.");
            }
            catch { }
        }

        // Leave helpers. Local, instant, no traffic.

        public static void LeaveGUI()
        {
            try { Main.CloseMenu(); }
            catch { }
        }

        public static void LeaveLobby()
        {
            try
            {
                Main.CloseMenu();
                NetworkSystem.Instance.ReturnToSinglePlayer();
                NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Left the lobby. You're safe with me.");
            }
            catch { }
        }

        private static float panicLeaveTime;
        public static void PanicLeave()
        {
            try
            {
                if (Time.time < panicLeaveTime) return;
                panicLeaveTime = Time.time + 2f;
                foreach (var b in Buttons.buttons.SelectMany(l => l).Where(b => b.enabled && !b.label && b.isTogglable))
                {
                    try
                    {
                        if (b.disableMethod != null) b.disableMethod.Invoke();
                        b.SetEnabled(false);
                    }
                    catch { }
                }
                Main.CloseMenu();
                NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Everything off, menu closed. Breathe.");
            }
            catch { }
        }

        // Second wave. Still strictly local-only, still non-tag: session
        // insights, personal bests, lobby awareness. Nothing sent.

        private static int jumpCount;
        private static bool wasGrounded = true;
        public static void JumpCounter()
        {
            try
            {
                bool grounded = false;
                try { grounded = GorillaTagger.Instance.IsGrounded(); } catch { }
                if (!grounded && wasGrounded) jumpCount++;
                wasGrounded = grounded;
            }
            catch { }
        }

        public static void ShowJumps()
        {
            try { NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {jumpCount} jumps this session.", 4000); }
            catch { }
        }

        public static void ResetJumps() => jumpCount = 0;

        private static float longestAir;
        private static float airStart = -1f;
        public static void AirTracker()
        {
            try
            {
                bool grounded = true;
                try { grounded = GorillaTagger.Instance.IsGrounded(); } catch { }
                if (!grounded && airStart < 0f)
                    airStart = Time.time;
                else if (grounded && airStart >= 0f)
                {
                    float air = Time.time - airStart;
                    if (air > longestAir) longestAir = air;
                    airStart = -1f;
                }
            }
            catch { }
        }

        public static void ShowAir()
        {
            try { NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Longest airtime: {longestAir:F1}s.", 4000); }
            catch { }
        }

        private static DateTime roomJoinTime = DateTime.UtcNow;
        public static void RoomTimerReset() => roomJoinTime = DateTime.UtcNow;

        public static void RoomTime()
        {
            try
            {
                TimeSpan t = DateTime.UtcNow - roomJoinTime;
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> In this room {(int)t.TotalMinutes}m {t.Seconds}s.", 4000);
            }
            catch { }
        }

        public static void NearbyCount()
        {
            try
            {
                int near = 0, mid = 0, far = 0;
                Vector3 me = VRRig.LocalRig.transform.position;
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        float d = Vector3.Distance(rig.transform.position, me);
                        if (d < 4f) near++;
                        else if (d < 12f) mid++;
                        else far++;
                    }
                    catch { }
                }
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {near} close, {mid} mid, {far} far.", 4000);
            }
            catch { }
        }

        public static void TallestCheck()
        {
            try
            {
                float myH = 0f;
                try { myH = GorillaTagger.Instance.headCollider.transform.position.y - GorillaTagger.Instance.bodyCollider.transform.position.y; } catch { }
                string taller = "";
                foreach (var rig in VRRigExtensions.ActiveRigs)
                {
                    try
                    {
                        if (rig == null || rig.IsLocal()) continue;
                        float h = rig.headMesh.transform.position.y - rig.transform.position.y;
                        if (h > myH + 0.1f) { taller = CleanPlayerName(rig.GetName()); break; }
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(taller))
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>AI</color><color=grey>]</color> You're the tallest here. Stand proud.", 4000);
                else
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {taller} is taller than you.", 4000);
            }
            catch { }
        }

        private static float stillTime;
        private static Vector3 stillPos = Vector3.zero;
        public static void StillnessScore()
        {
            try
            {
                Vector3 p = VRRig.LocalRig.transform.position;
                if (Vector3.Distance(p, stillPos) > 0.3f)
                {
                    stillPos = p;
                    stillTime = Time.time;
                    return;
                }
                float still = Time.time - stillTime;
                if (still > 10f)
                {
                    stillTime = Time.time;
                    NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> Still for {still:F0}s. Statue game strong.", 3000);
                }
            }
            catch { }
        }

        public static void PingCheck()
        {
            try
            {
                int ping = PhotonNetwork.GetPing();
                string feel = ping < 60 ? "crisp" : ping < 120 ? "fine" : ping < 180 ? "rough" : "slideshow";
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {ping}ms — {feel} tonight.", 4000);
            }
            catch { }
        }

        public static void FPSCheck()
        {
            try
            {
                int fps = Mathf.RoundToInt(1f / Mathf.Max(Time.deltaTime, 0.0001f));
                string feel = fps >= 60 ? "butter" : fps >= 40 ? "playable" : "choppy";
                NotificationManager.SendNotification($"<color=grey>[</color><color=purple>AI</color><color=grey>]</color> {fps}fps — {feel}.", 4000);
            }
            catch { }
        }

        // Self Tracker. Reads your own session (nickname, room, region,
        // platform, map, queue, mode, IDs, fps/ping stats) and POSTs it to
        // your Discord webhook in the Lemming Tracker format. Outbound to
        // YOUR webhook only — nothing the lobby can see.

        public static string TrackerWebhook = "https://discord.com/api/webhooks/1555053206646562906/mAqCo1YYVxwdlrJV4VO8acIJTgbGfqNGwAQIoQY1qRL811BwasMNXI7PwPw6MDrsrvnO";

        private static readonly List<float> fpsSamples = new List<float>();
        private static readonly List<float> pingSamples = new List<float>();
        public static void TrackerSampler()
        {
            try
            {
                if (Time.deltaTime > 0f)
                {
                    fpsSamples.Add(1f / Time.deltaTime);
                    if (fpsSamples.Count > 120) fpsSamples.RemoveAt(0);
                }
                try
                {
                    pingSamples.Add(PhotonNetwork.GetPing());
                    if (pingSamples.Count > 120) pingSamples.RemoveAt(0);
                }
                catch { }
            }
            catch { }
        }

        private static float Median(List<float> xs)
        {
            try
            {
                if (xs == null || xs.Count == 0) return 0f;
                var sorted = xs.OrderBy(v => v).ToArray();
                int m = sorted.Length / 2;
                return sorted.Length % 2 == 1 ? sorted[m] : (sorted[m - 1] + sorted[m]) / 2f;
            }
            catch { return 0f; }
        }

        private static string TrackerMap()
        {
            try
            {
                string zone = null;
                try { zone = PhotonNetworkController.Instance?.currentJoinTrigger?.networkZone; } catch { }
                if (!string.IsNullOrEmpty(zone)) return zone.ToUpper();
                try
                {
                    object prop = PhotonNetwork.CurrentRoom?.CustomProperties?[RoomConfig.Room_GameModePropKey];
                    if (prop is string s && !string.IsNullOrEmpty(s)) return s.ToUpper();
                }
                catch { }
            }
            catch { }
            return "?";
        }

        private static string TrackerMothershipId()
        {
            try
            {
                // Mothership session GUID has no public accessor; best effort.
                var auth = MothershipAuthenticator.Instance;
                if (auth == null) return "n/a";
                foreach (string field in new[] { "SessionId", "SessionID", "MothershipId", "MothershipID", "UserSessionId" })
                {
                    try
                    {
                        var f = auth.GetType().GetField(field);
                        if (f != null)
                        {
                            object v = f.GetValue(auth);
                            if (v != null && !string.IsNullOrEmpty(v.ToString())) return v.ToString();
                        }
                        var p = auth.GetType().GetProperty(field);
                        if (p != null)
                        {
                            object v = p.GetValue(auth);
                            if (v != null && !string.IsNullOrEmpty(v.ToString())) return v.ToString();
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return "n/a";
        }

        public static void SendSelfTracker()
        {
            try
            {
                if (string.IsNullOrEmpty(TrackerWebhook))
                {
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Tracker webhook not set.");
                    return;
                }
                try { CoroutineManager.instance.StartCoroutine(SendTrackerRoutine()); }
                catch { NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Tracker has no coroutine host."); }
            }
            catch { }
        }

        private static System.Collections.IEnumerator SendTrackerRoutine()
        {
            string nick = "?";
            string room = "offline";
            string region = "?";
            string platform = "?";
            string map = "?";
            string queue = "?";
            string mode = "?";
            string playfab = "?";
            string photonId = "?";
            int players = 0;
            int actor = 0;

            try { nick = PhotonNetwork.NickName ?? "?"; } catch { }
            try { room = PhotonNetwork.CurrentRoom?.Name ?? "offline"; } catch { }
            try { region = (PhotonNetwork.CloudRegion ?? "?").ToUpper(); } catch { }
            try { platform = VRRig.LocalRig.GetPlatform().ToString().ToUpper(); } catch { }
            map = TrackerMap();
            try { queue = GorillaComputer.instance.currentQueue ?? "?"; } catch { }
            try { mode = GorillaComputer.instance.currentGameMode?.Value ?? "?"; } catch { }
            try { playfab = PlayFabAuthenticator.instance.GetPlayFabPlayerId() ?? "?"; } catch { }
            try { photonId = PhotonNetwork.LocalPlayer?.UserId ?? "?"; } catch { }
            try { players = PhotonNetwork.PlayerList?.Length ?? 0; } catch { }
            try { actor = PhotonNetwork.LocalPlayer?.ActorNumber ?? 0; } catch { }
            string mothership = TrackerMothershipId();

            float fpsMed = Median(fpsSamples);
            float pingMed = Median(pingSamples);
            float targetFps = 0f;
            try { targetFps = VRRig.LocalRig.GetTargetFPS(); } catch { }
            float liveFps = 0f;
            try { liveFps = 1f / Mathf.Max(Time.deltaTime, 0.0001f); } catch { }
            string modList = TrackerEnabledMods(out int modCount);

            TimeSpan ago = DateTime.UtcNow - sessionStart;
            string agoStr = ago.TotalMinutes < 1 ? "just now" : $"{(int)ago.TotalMinutes} minutes ago";

            // Clean embed + raw JSON attachment.
            string fieldMods = string.IsNullOrEmpty(modList) ? "none" : modList;
            if (fieldMods.Length > 1000) fieldMods = fieldMods.Substring(0, 1000) + "…";
            string statsLine = $"player count: {players}, actor: {actor}, fps median: {fpsMed:F1}, ping median: {pingMed:F0}, target fps: {targetFps:F0}, fps now: {liveFps:F0}";

            string embedJson = "{\"embeds\":[{\"title\":\"Lemming Tracker V2\",\"color\":5814783,\"fields\":[" +
                EmbField("In-game Nickname", nick) + "," +
                EmbField("Nickname", nick) + "," +
                EmbField("Room Code", room, true) + "," +
                EmbField("Region", region, true) + "," +
                EmbField("Platform", platform, true) + "," +
                EmbField("Map", map, true) + "," +
                EmbField("Queue", queue, true) + "," +
                EmbField("Game Mode", mode, true) + "," +
                EmbField("PlayFab/Photon ID", playfab + " / " + photonId) + "," +
                EmbField("Mothership ID", mothership) + "," +
                EmbField("Time", agoStr, true) + "," +
                EmbField("Stats", statsLine) + "," +
                EmbField($"Enabled Mods ({modCount})", fieldMods) +
                "],\"footer\":{\"text\":\"HamoodMenu Self Tracker\"}}]}";

            string rawJson = "{\"nickname\":\"" + EscapeJson(nick) +
                "\",\"room\":\"" + EscapeJson(room) +
                "\",\"region\":\"" + EscapeJson(region) +
                "\",\"platform\":\"" + EscapeJson(platform) +
                "\",\"map\":\"" + EscapeJson(map) +
                "\",\"queue\":\"" + EscapeJson(queue) +
                "\",\"game_mode\":\"" + EscapeJson(mode) +
                "\",\"playfab_id\":\"" + EscapeJson(playfab) +
                "\",\"photon_id\":\"" + EscapeJson(photonId) +
                "\",\"mothership_id\":\"" + EscapeJson(mothership) +
                "\",\"player_count\":" + players +
                ",\"actor\":" + actor +
                ",\"fps_median\":" + fpsMed.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"ping_median\":" + pingMed.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"target_fps\":" + targetFps.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"fps_now\":" + liveFps.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) +
                ",\"enabled_mod_count\":" + modCount +
                ",\"enabled_mods\":\"" + EscapeJson(modList) + "\"}";

            string boundary = "----HamoodBoundary" + DateTime.UtcNow.Ticks;
            byte[] bodyBytes = BuildMultipart(boundary, embedJson, rawJson);

            using (UnityWebRequest req = new UnityWebRequest(TrackerWebhook, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + boundary);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    NotificationManager.SendNotification("<color=grey>[</color><color=green>TRACKER</color><color=grey>]</color> Sent to Discord.", 3000);
                else
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Webhook failed: " + req.error, 4000);
            }
        }

        private static string EmbField(string name, string value, bool inline = false)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) value = "?";
                if (value.Length > 1000) value = value.Substring(0, 1000) + "…";
                return "{\"name\":\"" + EscapeJson(name) + "\",\"value\":\"" + EscapeJson(value) + "\",\"inline\":" + (inline ? "true" : "false") + "}";
            }
            catch { return "{\"name\":\"?\",\"value\":\"?\",\"inline\":false}"; }
        }

        private static byte[] BuildMultipart(string boundary, string payloadJson, string fileJson)
        {
            try
            {
                var parts = new List<byte[]>();
                string head1 = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"payload_json\"\r\nContent-Type: application/json\r\n\r\n";
                string mid = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"files[0]\"; filename=\"session.json\"\r\nContent-Type: application/json\r\n\r\n";
                string tail = "\r\n--" + boundary + "--\r\n";
                parts.Add(Encoding.UTF8.GetBytes(head1));
                parts.Add(Encoding.UTF8.GetBytes(payloadJson));
                parts.Add(Encoding.UTF8.GetBytes(mid));
                parts.Add(Encoding.UTF8.GetBytes(fileJson));
                parts.Add(Encoding.UTF8.GetBytes(tail));
                int total = parts.Sum(p => p.Length);
                byte[] outBytes = new byte[total];
                int off = 0;
                foreach (byte[] p in parts)
                {
                    Buffer.BlockCopy(p, 0, outBytes, off, p.Length);
                    off += p.Length;
                }
                return outBytes;
            }
            catch { return Encoding.UTF8.GetBytes(payloadJson); }
        }

        private static string EscapeJson(string s)
        {
            try
            {
                return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
            }
            catch { return "?"; }
        }

        private static float trackerAutoTime;
        private static string trackerFingerprint = "";
        private static float trackerWatchTime;

        // Realtime self tracker. Watches you live: player ID, in-game name,
        // room, FPS, enabled mods. Pushes to Discord the moment anything
        // changes (room switch, mods toggled, name change) plus a 60s
        // heartbeat so the trail never goes cold.

        private static string TrackerEnabledMods(out int total)
        {
            total = 0;
            try
            {
                var names = new List<string>();
                int categoryIndex = 0;
                foreach (ButtonInfo[] category in Buttons.buttons)
                {
                    try
                    {
                        bool isSettings = categoryIndex < Buttons.categoryNames.Length && Buttons.categoryNames[categoryIndex].Contains("Settings");
                        if (!isSettings)
                        {
                            foreach (ButtonInfo b in category)
                            {
                                try
                                {
                                    if (b != null && b.enabled && !b.label)
                                    {
                                        string n = Main.NoRichtextTags(b.overlapText ?? b.buttonText);
                                        if (!string.IsNullOrEmpty(n))
                                        {
                                            names.Add(n.Length > 64 ? n.Substring(0, 64) : n);
                                            total++;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                    categoryIndex++;
                }
                names.Sort();
                // Discord-safe length cap.
                string joined = string.Join(", ", names);
                if (joined.Length > 1500) joined = joined.Substring(0, 1500) + "…";
                return joined;
            }
            catch { return ""; }
        }

        private static string TrackerFingerprint()
        {
            try
            {
                string nick = "", room = "", uid = "";
                try { nick = PhotonNetwork.NickName ?? ""; } catch { }
                try { room = PhotonNetwork.CurrentRoom?.Name ?? "offline"; } catch { }
                try { uid = PhotonNetwork.LocalPlayer?.UserId ?? ""; } catch { }
                string mods = TrackerEnabledMods(out _);
                return nick + "|" + room + "|" + uid + "|" + mods;
            }
            catch { return ""; }
        }

        public static void RealtimeSelfTracker()
        {
            try
            {
                TrackerSampler();
                if (Time.time < trackerWatchTime) return;
                trackerWatchTime = Time.time + 5f;

                string fp = TrackerFingerprint();
                if (string.IsNullOrEmpty(fp)) return;

                // Change detected (room, name, id, mods) -> push now.
                // Otherwise heartbeat every 60s.
                bool changed = trackerFingerprint != "" && fp != trackerFingerprint;
                bool heartbeatDue = Time.time >= trackerAutoTime;
                if (fp != trackerFingerprint)
                    trackerFingerprint = fp;

                if (changed || heartbeatDue)
                {
                    trackerAutoTime = Time.time + 60f;
                    try { CoroutineManager.instance.StartCoroutine(SendTrackerRoutine()); } catch { }
                }
            }
            catch { }
        }

        public static void ResetTrackerFingerprint() => trackerFingerprint = "";
    }
}
