using LethalNetworkAPI;
using System.Collections.Generic;

namespace DeathTunes
{
    public class SoundSelectionData
    {
        public ulong SteamID;
        public string SoundName;
    }

    public static class DeathSoundNetworking
    {
        public static readonly Dictionary<ulong, string> PlayerSounds =
            new Dictionary<ulong, string>();

        public static LNetworkMessage<SoundSelectionData> SoundMessage;
        public static LNetworkMessage<string> RequestMessage;

        public static void Initialize()
        {
            SoundMessage =
                LNetworkMessage<SoundSelectionData>.Create(
                    "DeathSoundSelection",
                    OnServerReceived,
                    OnClientReceived
                );

            RequestMessage =
                LNetworkMessage<string>.Create(
                    "DeathSoundRequest",
                    OnRequestReceived,
                    null
                );

            DeathTunesPlugin.Log.LogInfo(
                "Death sound networking initialized."
            );
        }

        public static void SendSelection(
            ulong steamID,
            string sound)
        {
            SoundSelectionData data =
                new SoundSelectionData()
                {
                    SteamID = steamID,
                    SoundName = sound
                };

            PlayerSounds[steamID] = sound;

            DeathTunesPlugin.Log.LogInfo(
                $"Sending sound selection: {steamID} -> {sound}"
            );

            SoundMessage.SendServer(data);
        }

        private static void OnServerReceived(
            SoundSelectionData data,
            ulong sender)
        {
            PlayerSounds[data.SteamID] =
                data.SoundName;

            DeathTunesPlugin.Log.LogInfo(
                $"Host stored sound: {data.SteamID} -> {data.SoundName}"
            );

            SoundMessage.SendClients(data);
        }

        private static void OnClientReceived(
            SoundSelectionData data)
        {
            PlayerSounds[data.SteamID] =
                data.SoundName;

            DeathTunesPlugin.Log.LogInfo(
                $"Client stored sound: {data.SteamID} -> {data.SoundName}"
            );
        }

        public static void RequestAllSounds()
        {
            DeathTunesPlugin.Log.LogInfo(
                "Requesting all death sounds..."
            );

            RequestMessage.SendServer("");
        }

        private static void OnRequestReceived(
            string unused,
            ulong sender)
        {
            DeathTunesPlugin.Log.LogInfo(
                $"Sending all sounds to client {sender}"
            );

            Dictionary<ulong, string> soundsCopy =
                new Dictionary<ulong, string>(
                    PlayerSounds
                );

            foreach (
                KeyValuePair<ulong, string> entry
                in soundsCopy
            )
            {
                SoundSelectionData data =
                    new SoundSelectionData()
                    {
                        SteamID = entry.Key,
                        SoundName = entry.Value
                    };

                SoundMessage.SendClient(
                    data,
                    sender
                );
            }

            DeathTunesPlugin.Log.LogInfo(
                $"Finished sending {soundsCopy.Count} sounds to client {sender}"
            );
        }

        public static void PrintDatabase()
        {
            DeathTunesPlugin.Log.LogInfo(
                "===== DEATH SOUND DATABASE ====="
            );

            foreach (
                var entry in PlayerSounds
            )
            {
                DeathTunesPlugin.Log.LogInfo(
                    $"{entry.Key} -> {entry.Value}"
                );
            }
        }
    }
}