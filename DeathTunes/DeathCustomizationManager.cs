using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LethalNetworkAPI;

namespace DeathTunes
{
    public static class DeathCustomizationManager
    {

        public static Dictionary<string, AudioClip> Sounds =
            new Dictionary<string, AudioClip>();

        public static List<string> SoundIDs =
            new List<string>();

        public static string SelectedSound = "";

        private static int soundIndex = 0;

        public static string PendingSoundSelection = null;

        public static int CurrentSoundIndex
        {
            get
            {
                return soundIndex;
            }
        }

        private static GameObject menu;

        private static void SendPlayerSound(PlayerControllerB player)
        {
            ulong steamID =
                player.playerSteamId;

            DeathTunesPlugin.Log.LogInfo(
                "Sending sound '" +
                SelectedSound +
                "' for SteamID " +
                steamID
            );

            DeathSoundNetworking.SendSelection(
                steamID,
                SelectedSound
            );
        }

        public static void LoadSounds()
        {

            string folder =
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location
                );

            List<string> files =
                new List<string>();

            DeathTunesPlugin.Log.LogInfo(
                "Searching for death sounds..."
            );

            string soundsFolder =
                Path.Combine(
                    folder,
                    "Sounds"
                );

            if (Directory.Exists(soundsFolder))
            {

                files.AddRange(
                    Directory.GetFiles(
                        soundsFolder,
                        "*.wav",
                        SearchOption.AllDirectories
                    )
                );

            }

            files.AddRange(
                Directory.GetFiles(
                    folder,
                    "*.wav",
                    SearchOption.TopDirectoryOnly
                )
            );

            files =
                files
                .Distinct()
                .ToList();

            foreach (string file in files)
            {
                AudioClip clip =
                    DeathTunesPlugin.Instance.LoadWav(file);

                if (clip != null)
                {

                    string id =
                        Path.GetFileNameWithoutExtension(file);

                    if (!Sounds.ContainsKey(id))
                    {
                        Sounds.Add(
                            id,
                            clip
                        );

                        SoundIDs.Add(
                            id
                        );

                        DeathTunesPlugin.Log.LogInfo(
                            "Loaded death sound: "
                            + id
                        );

                    }

                }

            }

            if (SoundIDs.Count > 0)
            {
                string saved =
                    DeathTunesPlugin.SavedDeathSound.Value;

                if (
                    !string.IsNullOrEmpty(saved)
                    &&
                    Sounds.ContainsKey(saved)
                )
                {
                    SelectedSound = saved;
                }
                else
                {
                    SelectedSound = SoundIDs[0];
                }
            }

            DeathTunesPlugin.Log.LogInfo(
                "Total death sounds loaded: "
                + Sounds.Count
            );
        }

        public static void OpenMenu()
        {
            if (menu != null)
            {
                menu.SetActive(true);
                return;
            }

            menu =
                new GameObject(
                    "DeathTunesMenu"
                );

            Canvas canvas =
                menu.AddComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                menu.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            menu.AddComponent<GraphicRaycaster>();

            GameObject background =
                CreatePanel(
                    menu.transform,
                    new Vector2(500, 300),
                    new Vector2(0, 0)
                );

            Image bgImage =
                background.GetComponent<Image>();

            bgImage.color =
                new Color(
                    0,
                    0,
                    0,
                    0.85f
                );

            GameObject title =
                CreateText(
                    background.transform,
                    "Choose Death Sound",
                    32,
                    new Vector2(0, 100)
                );

            GameObject soundText =
                CreateText(
                    background.transform,
                    "",
                    24,
                    new Vector2(0, 20)
                );

            TextMeshProUGUI soundLabel =
                soundText.GetComponent<TextMeshProUGUI>();

            UpdateSoundText(soundLabel);

            GameObject previous =
                CreateButton(
                    background.transform,
                    "<",
                    new Vector2(-150, -50)
                );

            previous
            .GetComponent<Button>()
            .onClick
            .AddListener(
                () =>
                {
                    PreviousSound();
                    UpdateSoundText(soundLabel);
                }
            );

            GameObject next =
                CreateButton(
                    background.transform,
                    ">",
                    new Vector2(150, -50)
                );

            next
            .GetComponent<Button>()
            .onClick
            .AddListener(
                () =>
                {
                    NextSound();
                    UpdateSoundText(soundLabel);
                }
            );

            GameObject preview =
                CreateButton(
                    background.transform,
                    "Preview",
                    new Vector2(0, -100)
                );

            preview
            .GetComponent<Button>()
            .onClick
            .AddListener(
                () =>
                {
                    PlaySelectedSound();
                }
            );

            GameObject close =
                CreateButton(
                    background.transform,
                    "Save & Close",
                    new Vector2(0, -150)
                );

            close
                .GetComponent<Button>()
                .onClick
                .AddListener(
                SaveAndClose
            );
        }


        private static void SaveAndClose()
        {
            DeathTunesPlugin.Log.LogInfo(
                "===== SAVING DEATH SOUND ====="
            );

            DeathTunesPlugin.Log.LogInfo(
                "Selected sound: " + SelectedSound
            );

            DeathTunesPlugin.SavedDeathSound.Value =
                SelectedSound;

            if (GameNetworkManager.Instance == null)
            {
                DeathTunesPlugin.Log.LogWarning(
                    "GameNetworkManager not initialized yet."
                );

                PendingSoundSelection = SelectedSound;

                menu.SetActive(false);
                return;
            }

            PlayerControllerB localPlayer =
                GameNetworkManager.Instance.localPlayerController;

            if (localPlayer == null)
            {
                DeathTunesPlugin.Log.LogWarning(
                    "Local player controller not available. Saving locally for later."
                );

                PendingSoundSelection = SelectedSound;

                menu.SetActive(false);
                return;
            }

            SendPlayerSound(localPlayer);

            menu.SetActive(false);
        }
        public static void CloseMenu()
        {
            if (menu != null)
            {
                menu.SetActive(false);
            }
        }

        private static void UpdateSoundText(
            TextMeshProUGUI text)
        {
            if (SoundIDs.Count == 0)
            {
                text.text =
                    "No sounds found";
                return;
            }

            text.text =
                SoundIDs[soundIndex];
        }

        public static void NextSound()
        {
            soundIndex++;

            if (soundIndex >= SoundIDs.Count)
                soundIndex = 0;

            SelectedSound =
                SoundIDs[soundIndex];

            DeathTunesPlugin.Log.LogInfo(
                "Changed selected sound to: "
                + SelectedSound
            );
        }

        public static void PreviousSound()
        {
            soundIndex--;

            if (soundIndex < 0)
                soundIndex =
                    SoundIDs.Count - 1;

            SelectedSound =
                SoundIDs[soundIndex];

            DeathTunesPlugin.Log.LogInfo(
                "Changed selected sound to: "
                + SelectedSound
            );
        }

        private static void PlaySelectedSound()
        {
            if (
                Sounds.ContainsKey(
                    SelectedSound
                ))
            {
                DeathTunesPlugin.PlayClip(
                    Sounds[SelectedSound]
                );
            }
        }

        private static GameObject CreatePanel(
            Transform parent,
            Vector2 size,
            Vector2 position)
        {
            GameObject obj =
                new GameObject(
                    "Panel"
                );

            obj.transform.SetParent(
                parent
            );

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.sizeDelta =
                size;

            rect.anchoredPosition =
                position;

            obj.AddComponent<Image>();

            return obj;
        }

        private static GameObject CreateText(
            Transform parent,
            string value,
            int size,
            Vector2 position)
        {
            GameObject obj =
                new GameObject(
                    "Text"
                );

            obj.transform.SetParent(
                parent
            );

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.sizeDelta =
                new Vector2(
                    400,
                    50
                );

            rect.anchoredPosition =
                position;

            TextMeshProUGUI text =
                obj.AddComponent<TextMeshProUGUI>();

            text.text =
                value;

            text.fontSize =
                size;

            text.alignment =
                TextAlignmentOptions.Center;

            return obj;
        }

        private static GameObject CreateButton(
            Transform parent,
            string value,
            Vector2 position)
        {
            GameObject obj =
                new GameObject(
                    value
                );

            obj.transform.SetParent(
                parent
            );

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.sizeDelta =
                new Vector2(
                    120,
                    40
                );

            rect.anchoredPosition =
                position;

            Image image =
                obj.AddComponent<Image>();

            image.color =
                Color.gray;

            Button button =
                obj.AddComponent<Button>();

            GameObject textObj =
                CreateText(
                    obj.transform,
                    value,
                    18,
                    Vector2.zero
                );

            return obj;
        }

    }

}