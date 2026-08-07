using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;

namespace DeathTunes
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class PlayerConnectionPatch
    {
        private static bool hasSynced = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void OnGameStart(StartOfRound __instance)
        {
            if (hasSynced)
                return;

            hasSynced = true;

            DeathTunesPlugin.Log.LogInfo(
                "DeathTunes player connection sync started."
            );

            __instance.StartCoroutine(
                DelayedSync()
            );
        }


        private static System.Collections.IEnumerator DelayedSync()
        {
            yield return new WaitForSeconds(3f);

            PlayerControllerB player =
                StartOfRound.Instance.localPlayerController;

            if (player == null)
            {
                DeathTunesPlugin.Log.LogWarning(
                    "Cannot sync death sound. Local player missing."
                );

                yield break;
            }

            ulong steamID =
                player.playerSteamId;

            DeathTunesPlugin.Log.LogInfo(
                $"Local player found. SteamID: {steamID}"
            );

            if (
                !string.IsNullOrEmpty(
                    DeathTunesPlugin.SavedDeathSound.Value
                )
            )
            {
                DeathSoundNetworking.SendSelection(
                    steamID,
                    DeathTunesPlugin.SavedDeathSound.Value
                );

                DeathTunesPlugin.Log.LogInfo(
                    "Sent saved death sound."
                );
            }
            else
            {
                DeathTunesPlugin.Log.LogInfo(
                    "No saved death sound found."
                );
            }

            DeathSoundNetworking.RequestAllSounds();

            DeathTunesPlugin.Log.LogInfo(
                "Requested all player death sounds."
            );
        }


        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        public static void OnDestroy()
        {
            hasSynced = false;

            DeathTunesPlugin.Log.LogInfo(
                "DeathTunes player sync reset."
            );
        }
    }
}