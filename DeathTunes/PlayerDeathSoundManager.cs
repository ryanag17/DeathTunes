using GameNetcodeStuff;
using UnityEngine;

namespace DeathTunes
{
    public static class PlayerDeathSoundManager
    {
        public static void PlayPlayerDeathSound(
            PlayerControllerB player)
        {
            if (player == null)
                return;

            ulong steamID =
                player.playerSteamId;

            DeathTunesPlugin.Log.LogInfo(
                $"Looking up death sound for SteamID: {steamID}"
            );

            AudioClip clip = null;

            if (
                DeathSoundNetworking.PlayerSounds.TryGetValue(
                    steamID,
                    out string soundID
                )
            )
            {
                DeathTunesPlugin.Log.LogInfo(
                    $"Found sound entry: {steamID} -> {soundID}"
                );

                if (
                    DeathCustomizationManager.Sounds.ContainsKey(
                        soundID
                    )
                )
                {
                    clip =
                        DeathCustomizationManager.Sounds[soundID];
                }
                else
                {
                    DeathTunesPlugin.Log.LogWarning(
                        $"Sound file does not exist: {soundID}"
                    );
                }
            }
            else
            {
                DeathTunesPlugin.Log.LogWarning(
                    $"No sound registered for SteamID: {steamID}"
                );
            }

            if (
                clip == null &&
                player ==
                StartOfRound.Instance.localPlayerController
            )
            {
                string saved =
                    DeathTunesPlugin.SavedDeathSound.Value;

                if (
                    !string.IsNullOrEmpty(saved) &&
                    DeathCustomizationManager.Sounds.ContainsKey(saved)
                )
                {
                    DeathTunesPlugin.Log.LogInfo(
                        $"Using local saved fallback sound: {saved}"
                    );

                    clip =
                        DeathCustomizationManager.Sounds[saved];
                }
            }

            if (clip == null)
            {
                DeathTunesPlugin.Log.LogWarning(
                    $"Could not find death sound for {steamID}"
                );

                return;
            }

            DeathTunesPlugin.Log.LogInfo(
                $"Playing death sound for {steamID}: {clip.name}"
            );

            DeathTunesPlugin.PlayClip(
                clip
            );
        }
    }
}