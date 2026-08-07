using HarmonyLib;
using GameNetcodeStuff;

namespace DeathTunes
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class DeathPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void CheckDeath(
            PlayerControllerB __instance)
        {
            if (__instance == null)
                return;

            ulong steamID =
                __instance.playerSteamId;

            bool dead =
                __instance.isPlayerDead;

            bool previous =
                false;

            DeathTunesPlugin.LastDeathState.TryGetValue(
                steamID,
                out previous
            );

            if (dead && !previous)
            {
                DeathTunesPlugin.Log.LogInfo(
                    "DEATH DETECTED: Player "
                    + steamID
                );

                PlayerDeathSoundManager.PlayPlayerDeathSound(
                    __instance
                );
            }

            DeathTunesPlugin.LastDeathState[steamID] =
                dead;
        }
    }
}