using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Patches.Menu;
using UnityEngine;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.RigUtilities;

namespace Seralyth.Mods
{
    public static class HalloweenMods
    {
        private const float StealthMin = 0.09f;
        private const float StealthMax = 0.14f;

        private static HalloweenGhostChaser _lucy;
        public static HalloweenGhostChaser Lucy
        {
            get
            {
                if (_lucy == null)
                {
                    _lucy = GetObject("Environment Objects/05Maze_PersistentObjects/2025_Halloween1_PersistentObjects/Halloween Ghosts/Lucy/Halloween Ghost/FloatingChaseSkeleton")?.GetComponent<HalloweenGhostChaser>();
                    if (_lucy == null)
                    {
#pragma warning disable CS0618
                        _lucy = Object.FindObjectOfType<HalloweenGhostChaser>();
#pragma warning restore CS0618
                    }
                }
                return _lucy;
            }
            set => _lucy = value;
        }

        private static LurkerGhost _lurker;
        public static LurkerGhost Lurker
        {
            get
            {
                if (_lurker == null)
                {
                    _lurker = GetObject("Environment Objects/05Maze_PersistentObjects/2025_Halloween1_PersistentObjects/Halloween Ghosts/Lurker Ghost/GhostLurker_Prefab")?.GetComponent<LurkerGhost>();
                    if (_lurker == null)
                    {
#pragma warning disable CS0618
                        _lurker = Object.FindObjectOfType<LurkerGhost>();
#pragma warning restore CS0618
                    }
                }
                return _lurker;
            }
            set => _lurker = value;
        }

        private static float bringDelay;
        private static float floatDelay;
        private static float notifyDelay;
        private static float actionDelay;
        private static float lurkerDelay;
        private static float moveDelay;
        public static float lucyDelay;

        private static bool Throttle(ref float delay)
        {
            if (!(Time.time > delay))
                return false;
            delay = Time.time + Random.Range(StealthMin, StealthMax);
            return true;
        }

        private static bool CanUseLucy(out HalloweenGhostChaser hgc)
        {
            hgc = Lucy;
            if (hgc == null)
                return false;
            if (!NetworkSystem.Instance.InRoom)
                return false;
            if (!hgc.IsMine)
            {
                if (Time.time > notifyDelay)
                {
                    notifyDelay = Time.time + 5f;
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                }
                return false;
            }
            return true;
        }

        private static bool CanUseLurker(out LurkerGhost ghost)
        {
            ghost = Lurker;
            if (ghost == null)
                return false;
            if (!NetworkSystem.Instance.InRoom)
                return false;
            if (!ghost.IsMine)
            {
                if (Time.time > notifyDelay)
                {
                    notifyDelay = Time.time + 5f;
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                }
                return false;
            }
            return true;
        }

        private static NetPlayer GetRandomOthers()
        {
            var others = NetworkSystem.Instance.PlayerListOthers;
            if (others == null || others.Length == 0)
                return null;
            return others[Random.Range(0, others.Length)];
        }

        public static void BringLucyToPlayer()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref bringDelay))
                return;
            Vector3 localPos = GorillaTagger.Instance.bodyCollider.transform.position;
            hgc.transform.position = localPos + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
            hgc.targetPlayer = NetworkSystem.Instance.LocalPlayer;
            try { hgc.followTarget = GorillaTagger.Instance.offlineVRRig.transform; } catch { }
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void BringLucyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                {
                    if (!CanUseLucy(out HalloweenGhostChaser hgc))
                        return;
                    if (!Throttle(ref bringDelay))
                        return;
                    NetPlayer tp = lockTarget.GetPlayer();
                    if (tp == null || !tp.InRoom())
                        return;
                    hgc.transform.position = lockTarget.transform.position + Vector3.up;
                    hgc.currentSpeed = 0f;
                    hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                    hgc.targetPlayer = tp;
                    hgc.followTarget = lockTarget.transform;
                    SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { tp.ActorNumber } });
                    RPCProtection();
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        private static void SpamLucyOnPlayer(VRRig victim)
        {
            if (victim == null || victim.IsLocal())
                return;
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref floatDelay))
                return;
            NetPlayer victimPlayer = victim.GetPlayer();
            if (victimPlayer == null || !victimPlayer.InRoom())
                return;
            hgc.transform.position = victim.transform.position + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            hgc.grabTime = Time.time;
            hgc.targetPlayer = victimPlayer;
            hgc.followTarget = victim.transform;
            SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { victimPlayer.ActorNumber } });
            RPCProtection();
        }

        public static void FloatGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                    SpamLucyOnPlayer(lockTarget);
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        public static void FloatAll()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref floatDelay))
                return;
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
            {
                VRRig rig = GetVRRigFromPlayer(player);
                if (rig == null)
                    continue;
                hgc.transform.position = rig.transform.position + Vector3.up;
                hgc.currentSpeed = 0f;
                hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
                hgc.grabTime = Time.time;
                hgc.targetPlayer = player;
                hgc.followTarget = rig.transform;
                SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            }
            RPCProtection();
        }

        public static void FloatSelf()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref floatDelay))
                return;
            hgc.transform.position = GorillaTagger.Instance.bodyCollider.transform.position + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            hgc.grabTime = Time.time;
            hgc.targetPlayer = NetworkSystem.Instance.LocalPlayer;
            try { hgc.followTarget = GorillaTagger.Instance.offlineVRRig.transform; } catch { }
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void DisableFloat()
        {
            if (gunLocked)
                gunLocked = false;
        }

        public static void DisableGuns()
        {
            if (gunLocked)
                gunLocked = false;
            lockTarget = null;
        }

        public static void DisableLucyAttack()
        {
            DisableGuns();
            try { SerializePatch.OverrideSerialization = null; } catch { }
        }

        public static void DisableLurkerAttack()
        {
            DisableGuns();
            try { SerializePatch.OverrideSerialization = null; } catch { }
        }

        public static void DisableBecome()
        {
            DisableGuns();
            try { SerializePatch.OverrideSerialization = null; } catch { }
            try { Movement.EnableRig(); } catch { }
        }

        public static void SpawnRedLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            hgc.timeGongStarted = Time.time;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
            hgc.isSummoned = true;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void SpawnBlueLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            hgc.timeGongStarted = Time.time;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
            hgc.isSummoned = false;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void DespawnLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Dormant;
            hgc.isSummoned = false;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void LucyChaseSelf()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
            hgc.targetPlayer = NetworkSystem.Instance.LocalPlayer;
            try { hgc.followTarget = GorillaTagger.Instance.offlineVRRig.transform; } catch { }
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        private static void LucyChasePlayer(NetPlayer player)
        {
            if (player == null || !player.InRoom())
                return;
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            VRRig rig = GetVRRigFromPlayer(player);
            hgc.transform.position = (rig != null ? rig.transform.position : GorillaTagger.Instance.bodyCollider.transform.position) + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
            hgc.targetPlayer = player;
            if (rig != null)
                hgc.followTarget = rig.transform;
            SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            RPCProtection();
        }

        public static void LucyChaseGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                {
                    VRRig target = lockTarget;
                    if (target != null && !target.IsLocal())
                        LucyChasePlayer(target.GetPlayer());
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        public static void LucyChaseAll()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
            {
                VRRig rig = GetVRRigFromPlayer(player);
                if (rig == null)
                    continue;
                hgc.transform.position = rig.transform.position + Vector3.up;
                hgc.currentSpeed = 0f;
                hgc.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                hgc.targetPlayer = player;
                hgc.followTarget = rig.transform;
                SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            }
            RPCProtection();
        }

        public static void LucyAttackSelf()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            hgc.transform.position = GorillaTagger.Instance.bodyCollider.transform.position + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            hgc.grabTime = Time.time;
            hgc.targetPlayer = NetworkSystem.Instance.LocalPlayer;
            try { hgc.followTarget = GorillaTagger.Instance.offlineVRRig.transform; } catch { }
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void LucyAttackPlayer(NetPlayer player)
        {
            if (player == null || !player.InRoom())
                return;
            VRRig rig = GetVRRigFromPlayer(player);
            if (rig == null || rig.IsLocal())
                return;
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            hgc.transform.position = rig.transform.position + Vector3.up;
            hgc.currentSpeed = 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
            hgc.grabTime = Time.time;
            hgc.targetPlayer = player;
            hgc.followTarget = rig.transform;
            SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            RPCProtection();
        }

        public static void LucyAttackGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                    LucyAttackPlayer(lockTarget.GetPlayer());
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        public static void LucyAttackAll()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref actionDelay))
                return;
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
            {
                VRRig rig = GetVRRigFromPlayer(player);
                if (rig == null)
                    continue;
                hgc.transform.position = rig.transform.position + Vector3.up;
                hgc.currentSpeed = 0f;
                hgc.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
                hgc.grabTime = Time.time;
                hgc.targetPlayer = player;
                hgc.followTarget = rig.transform;
                SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            }
            RPCProtection();
        }

        public static void LucyHarassGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                {
                    if (!CanUseLucy(out HalloweenGhostChaser hgc))
                        return;
                    if (!Throttle(ref lucyDelay))
                        return;
                    NetPlayer tp = lockTarget.GetPlayer();
                    if (tp == null || !tp.InRoom())
                        return;
                    hgc.transform.position = lockTarget.transform.position + Vector3.up;
                    hgc.currentSpeed = 0f;
                    hgc.currentState = hgc.currentState == HalloweenGhostChaser.ChaseState.Grabbing ? HalloweenGhostChaser.ChaseState.Chasing : HalloweenGhostChaser.ChaseState.Grabbing;
                    hgc.targetPlayer = tp;
                    hgc.followTarget = lockTarget.transform;
                    SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { tp.ActorNumber } });
                    RPCProtection();
                }
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        public static void MoveLucyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                UnityEngine.GameObject NewPointer = GunData.NewPointer;
                if (GetGunInput(true))
                {
                    if (!CanUseLucy(out HalloweenGhostChaser hgc))
                        return;
                    if (!Throttle(ref moveDelay))
                        return;
                    hgc.transform.position = NewPointer.transform.position + Vector3.up;
                    SendSerialize(hgc.GetView);
                    RPCProtection();
                }
            }
        }

        public static void SpazLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref lucyDelay))
                return;
            hgc.timeGongStarted = hgc.timeGongStarted == 0f ? Time.time : 0f;
            hgc.currentState = HalloweenGhostChaser.ChaseState.Gong;
            hgc.isSummoned = true;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void AnnoyingLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            if (!Throttle(ref lucyDelay))
                return;
            hgc.timeGongStarted = Time.time;
            hgc.grabTime = Time.time;
            hgc.currentState = hgc.currentState == HalloweenGhostChaser.ChaseState.Gong ? HalloweenGhostChaser.ChaseState.Grabbing : HalloweenGhostChaser.ChaseState.Gong;
            NetPlayer random = GetRandomOthers();
            if (random != null && random.InRoom())
            {
                hgc.targetPlayer = random;
                VRRig rig = GetVRRigFromPlayer(random);
                if (rig != null)
                    hgc.followTarget = rig.transform;
                SendSerialize(hgc.GetView, new RaiseEventOptions { TargetActors = new[] { random.ActorNumber } });
            }
            else
            {
                SendSerialize(hgc.GetView);
            }
            RPCProtection();
        }

        public static void BecomeLucy()
        {
            if (!NetworkSystem.Instance.InRoom)
                return;
            if (!NetworkSystem.Instance.IsMasterClient)
            {
                if (Time.time > notifyDelay)
                {
                    notifyDelay = Time.time + 5f;
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                }
                return;
            }
            if (Lucy != null)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GorillaTagger.Instance.bodyCollider.transform.position - Vector3.up * 99999f;
                Lucy.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                Lucy.transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation;
                Lucy.currentState = HalloweenGhostChaser.ChaseState.Chasing;
                Lucy.targetPlayer = null;
                SendSerialize(Lucy.GetView);
                RPCProtection();
            }
        }

        public static void FastLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            hgc.currentSpeed = 10f;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void SlowLucy()
        {
            if (!CanUseLucy(out HalloweenGhostChaser hgc))
                return;
            hgc.currentSpeed = 1f;
            SendSerialize(hgc.GetView);
            RPCProtection();
        }

        public static void SpawnLurker()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            ghost.currentState = LurkerGhost.ghostState.patrol;
            SendSerialize(ghost.GetView);
            RPCProtection();
        }

        public static void DespawnLurker()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            ghost.currentState = LurkerGhost.ghostState.patrol;
            SendSerialize(ghost.GetView);
            RPCProtection();
        }

        public static void MoveLurkerGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                UnityEngine.GameObject NewPointer = GunData.NewPointer;
                if (GetGunInput(true))
                {
                    if (!CanUseLurker(out LurkerGhost ghost))
                        return;
                    if (!Throttle(ref moveDelay))
                        return;
                    ghost.transform.position = NewPointer.transform.position + Vector3.up;
                    SendSerialize(ghost.GetView);
                    RPCProtection();
                }
            }
        }

        public static void LurkerAttackSelf()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            ghost.ChangeState(LurkerGhost.ghostState.patrol);
            ghost.currentState = LurkerGhost.ghostState.possess;
            ghost.targetPlayer = NetworkSystem.Instance.LocalPlayer;
            SendSerialize(ghost.GetView);
            RPCProtection();
        }

        public static void LurkerAttackPlayer(NetPlayer player)
        {
            if (player == null || !player.InRoom())
                return;
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            if (ghost.targetPlayer != player)
            {
                ghost.ChangeState(LurkerGhost.ghostState.patrol);
                SendSerialize(ghost.GetView);
            }
            ghost.currentState = LurkerGhost.ghostState.possess;
            ghost.targetPlayer = player;
            SendSerialize(ghost.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            RPCProtection();
        }

        public static void LurkerAttackGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;
                if (gunLocked && lockTarget != null)
                    LurkerAttackPlayer(lockTarget.GetPlayer());
                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider?.GetComponentInParent<VRRig>();
                    if (gunTarget != null && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                    gunLocked = false;
            }
        }

        public static void LurkerAttackAll()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            foreach (NetPlayer player in NetworkSystem.Instance.PlayerListOthers)
            {
                VRRig rig = GetVRRigFromPlayer(player);
                if (rig == null)
                    continue;
                ghost.currentState = LurkerGhost.ghostState.possess;
                ghost.targetPlayer = player;
                SendSerialize(ghost.GetView, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } });
            }
            RPCProtection();
        }

        public static void SpazLurker()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            ghost.currentState = ghost.currentState == LurkerGhost.ghostState.charge ? LurkerGhost.ghostState.seek : LurkerGhost.ghostState.charge;
            NetPlayer random = GetRandomOthers();
            if (random != null && random.InRoom())
            {
                ghost.targetPlayer = random;
                SendSerialize(ghost.GetView, new RaiseEventOptions { TargetActors = new[] { random.ActorNumber } });
            }
            else
            {
                SendSerialize(ghost.GetView);
            }
            RPCProtection();
        }

        public static void BreakLurker()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            ghost.currentState = ghost.currentState == LurkerGhost.ghostState.charge ? LurkerGhost.ghostState.possess : LurkerGhost.ghostState.charge;
            NetPlayer random = GetRandomOthers();
            if (random != null && random.InRoom())
                ghost.targetPlayer = random;
            SendSerialize(ghost.GetView);
            RPCProtection();
        }

        public static void AnnoyingLurker()
        {
            if (!CanUseLurker(out LurkerGhost ghost))
                return;
            if (!Throttle(ref lurkerDelay))
                return;
            ghost.currentState = ghost.currentState == LurkerGhost.ghostState.possess ? LurkerGhost.ghostState.charge : LurkerGhost.ghostState.possess;
            NetPlayer random = GetRandomOthers();
            if (random != null && random.InRoom())
            {
                ghost.targetPlayer = random;
                SendSerialize(ghost.GetView, new RaiseEventOptions { TargetActors = new[] { random.ActorNumber } });
            }
            else
            {
                SendSerialize(ghost.GetView);
            }
            RPCProtection();
        }

        public static void BecomeLurker()
        {
            if (!NetworkSystem.Instance.InRoom)
                return;
            if (!NetworkSystem.Instance.IsMasterClient)
            {
                if (Time.time > notifyDelay)
                {
                    notifyDelay = Time.time + 5f;
                    NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not master client.");
                }
                return;
            }
            if (Lurker != null)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GorillaTagger.Instance.bodyCollider.transform.position - Vector3.up * 99999f;
                Lurker.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                Lurker.transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation;
                Lurker.currentState = LurkerGhost.ghostState.seek;
                SendSerialize(Lurker.GetView);
                RPCProtection();
            }
        }
    }
}
