using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using GameNetcodeStuff;

namespace DeathTunes
{
    [BepInPlugin("ryang.deathtunes", "DeathTunes", "0.1.0")]
    public class DeathTunesPlugin : BaseUnityPlugin
    {
        public static DeathTunesPlugin Instance;
        public static ManualLogSource Log;
        public static AudioSource AudioSource;
        public static BepInEx.Configuration.ConfigEntry<string> SavedDeathSound;

        public static Dictionary<ulong, bool> LastDeathState =
            new Dictionary<ulong, bool>();

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            SavedDeathSound =
                Config.Bind(
                    "DeathTunes",
                    "SelectedDeathSound",
                    "",
                    "The selected death sound for this player."
                );

            Log.LogInfo("DeathTunes starting...");

            Log.LogInfo(
                "DLL location: " +
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location
                )
            );

            DeathCustomizationManager.LoadSounds();
            DeathSoundNetworking.Initialize();

            Harmony harmony =
                new Harmony("ryang.deathtunes");

            harmony.PatchAll();

            StartCoroutine(CreateAudioSource());

            Log.LogInfo("DeathTunes loaded!");
        }

        private void Update()
        {
            if (DeathCustomizationManager.PendingSoundSelection == null)
                return;

            Log.LogInfo(
                "DeathTunes pending sound: " +
                DeathCustomizationManager.PendingSoundSelection
            );

            if (StartOfRound.Instance == null)
            {
                Log.LogInfo("Waiting for StartOfRound...");
                return;
            }

            Log.LogInfo("StartOfRound exists");

            PlayerControllerB player =
                StartOfRound.Instance.localPlayerController;

            if (player == null)
            {
                Log.LogInfo("Waiting for local player...");
                return;
            }

            Log.LogInfo("Local player found!");

            ulong steamID = player.playerSteamId;

            DeathSoundNetworking.SendSelection(
                steamID,
                DeathCustomizationManager.PendingSoundSelection
            );

            Log.LogInfo(
                "Sent sound " +
                DeathCustomizationManager.PendingSoundSelection +
                " for SteamID " +
                steamID
            );

            DeathCustomizationManager.PendingSoundSelection = null;
        }

        private IEnumerator CreateAudioSource()
        {
            yield return new WaitForSeconds(3f);

            CreateAudioObject();

            Log.LogInfo("DeathTunes AudioSource created");
        }

        public static void CreateAudioObject()
        {
            if (AudioSource != null)
                return;

            GameObject audioObject =
                new GameObject("DeathTunesAudio");

            DontDestroyOnLoad(audioObject);

            AudioSource =
                audioObject.AddComponent<AudioSource>();

            AudioSource.playOnAwake = false;
            AudioSource.loop = false;
            AudioSource.spatialBlend = 0f;
            AudioSource.volume = 1f;
            AudioSource.ignoreListenerVolume = true;
            AudioSource.priority = 0;
        }

        public static AudioClip LoadWavPublic(string path)
        {
            return Instance.LoadWav(path);
        }

        public AudioClip LoadWav(string path)
        {
            try
            {
                UnityWebRequest request =
                    UnityWebRequestMultimedia.GetAudioClip(
                        "file:///" + path,
                        AudioType.WAV
                    );

                UnityWebRequestAsyncOperation operation =
                    request.SendWebRequest();

                while (!operation.isDone)
                {
                    Thread.Sleep(10);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Log.LogError(request.error);
                    return null;
                }

                AudioClip clip =
                    DownloadHandlerAudioClip.GetContent(request);

                clip.name =
                    Path.GetFileNameWithoutExtension(path);

                return clip;
            }
            catch (Exception e)
            {
                Log.LogError(e.Message);
                return null;
            }
        }

        public static void PlayClip(AudioClip clip)
        {
            if (clip == null)
            {
                Log.LogError("Tried to play NULL clip");
                return;
            }

            if (AudioSource == null)
                CreateAudioObject();

            AudioSource.Stop();
            AudioSource.clip = clip;
            AudioSource.volume = 1f;
            AudioSource.Play();

            Log.LogInfo(
                "Playing death sound: " + clip.name
            );
        }
    }
}