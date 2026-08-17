using GameNetcodeStuff;
using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathTunes
{
    public static class DeathCustomizationManager
    {
        public static Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();
        public static List<string> SoundIDs = new List<string>();
        public static string SelectedSound = "";
        private static int soundIndex = 0;
        public static string PendingSoundSelection = null;

        public static int CurrentSoundIndex
        {
            get { return soundIndex; }
        }

        private static GameObject menu;
        private static TextMeshProUGUI soundNameText;
        private static TextMeshProUGUI soundCounterText;
        private static Button previousButton;
        private static Button nextButton;
        private static Button previewButton;
        private static Button saveButton;
        private static Button closeButton;
        private static bool menuCreated = false;
        private static TMP_FontAsset gameFont;

        private static readonly Color BackgroundColor = new Color32(0, 0, 0, 235);
        private static readonly Color PanelColor = new Color32(0, 0, 0, 255);
        private static readonly Color RedColor = new Color32(247, 0, 7, 255);
        private static readonly Color SecondaryRedColor = new Color32(240, 0, 7, 250);
        private static readonly Color BlackColor = new Color32(0, 0, 0, 255);
        private static readonly Color WhiteColor = new Color32(255, 255, 255, 255);
        private static readonly Color DisabledColor = new Color32(45, 45, 45, 180);

        private static void LoadGameFont()
        {
            if (gameFont != null)
                return;

            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

            foreach (TMP_FontAsset font in fonts)
            {
                if (font == null)
                    continue;

                if (font.name.Equals("3270-Regular", StringComparison.OrdinalIgnoreCase))
                {
                    gameFont = font;
                    break;
                }
            }

            if (gameFont == null)
            {
                foreach (TMP_FontAsset font in fonts)
                {
                    if (font == null)
                        continue;

                    if (font.name.IndexOf("3270", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gameFont = font;
                        break;
                    }
                }
            }

            if (gameFont != null)
            {
                DeathTunesPlugin.Log.LogInfo(
                    "DeathTunes using game font: " + gameFont.name
                );
            }
            else
            {
                DeathTunesPlugin.Log.LogWarning(
                    "3270-Regular font could not be found. Using default TMP font."
                );
            }
        }

        private static void SendPlayerSound(PlayerControllerB player)
        {
            if (player == null)
                return;

            ulong steamID = player.playerSteamId;

            DeathTunesPlugin.Log.LogInfo(
                "Sending sound '" + SelectedSound + "' for SteamID " + steamID
            );

            DeathSoundNetworking.SendSelection(steamID, SelectedSound);
        }

        public static void LoadSounds()
        {
            LoadGameFont();

            string folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            );

            List<string> files = new List<string>();

            DeathTunesPlugin.Log.LogInfo("Searching for death sounds...");

            string soundsFolder = Path.Combine(folder, "Sounds");

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

            files = files.Distinct().ToList();

            foreach (string file in files)
            {
                AudioClip clip = DeathTunesPlugin.Instance.LoadWav(file);

                if (clip != null)
                {
                    string id = Path.GetFileNameWithoutExtension(file);

                    if (!Sounds.ContainsKey(id))
                    {
                        Sounds.Add(id, clip);
                        SoundIDs.Add(id);

                        DeathTunesPlugin.Log.LogInfo(
                            "Loaded death sound: " + id
                        );
                    }
                }
            }

            if (SoundIDs.Count > 0)
            {
                string saved = DeathTunesPlugin.SavedDeathSound.Value;

                if (!string.IsNullOrEmpty(saved) && Sounds.ContainsKey(saved))
                {
                    SelectedSound = saved;
                    soundIndex = SoundIDs.IndexOf(saved);

                    if (soundIndex < 0)
                        soundIndex = 0;
                }
                else
                {
                    SelectedSound = SoundIDs[0];
                    soundIndex = 0;
                }
            }

            DeathTunesPlugin.Log.LogInfo(
                "Total death sounds loaded: " + Sounds.Count
            );
        }

        public static void OpenMenu()
        {
            if (menu != null)
            {
                menu.SetActive(true);
                UpdateMenuDisplay();
                return;
            }

            CreateMenu();
            menu.SetActive(true);
            UpdateMenuDisplay();
        }

        private static void CreateMenu()
        {
            if (menuCreated)
                return;

            menuCreated = true;
            LoadGameFont();

            menu = new GameObject("DeathTunesMenu");

            Canvas canvas = menu.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = menu.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            menu.AddComponent<GraphicRaycaster>();
            UnityEngine.Object.DontDestroyOnLoad(menu);

            GameObject overlay = CreatePanel(
                menu.transform,
                Vector2.zero,
                Vector2.zero,
                BackgroundColor
            );

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject panel = CreatePanel(
                menu.transform,
                new Vector2(760, 570),
                Vector2.zero,
                PanelColor
            );

            AddBorder(panel, RedColor, 2f);

            GameObject title = CreateText(
                panel.transform,
                "DEATHTUNES",
                44,
                new Vector2(0, 215),
                RedColor
            );

            TextMeshProUGUI titleText =
                title.GetComponent<TextMeshProUGUI>();

            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 3f;

            GameObject subtitle = CreateText(
                panel.transform,
                "DEATH SOUND CUSTOMIZATION",
                20,
                new Vector2(0, 175),
                RedColor
            );

            subtitle.GetComponent<TextMeshProUGUI>().fontStyle =
                FontStyles.Bold;

            CreateDivider(
                panel.transform,
                new Vector2(0, 145)
            );

            GameObject selectText = CreateText(
                panel.transform,
                "SELECT SOUND",
                21,
                new Vector2(0, 112),
                RedColor
            );

            selectText.GetComponent<TextMeshProUGUI>().fontStyle =
                FontStyles.Bold;

            GameObject soundDisplay = CreatePanel(
                panel.transform,
                new Vector2(500, 100),
                new Vector2(0, 48),
                BlackColor
            );

            AddBorder(soundDisplay, RedColor, 1f);

            GameObject soundText = CreateText(
                soundDisplay.transform,
                "",
                30,
                Vector2.zero,
                RedColor
            );

            soundNameText =
                soundText.GetComponent<TextMeshProUGUI>();

            soundNameText.fontStyle = FontStyles.Bold;
            soundNameText.enableWordWrapping = false;
            soundNameText.overflowMode = TextOverflowModes.Ellipsis;

            previousButton = CreateStyledButton(
                panel.transform,
                "‹",
                new Vector2(-315, 48),
                new Vector2(70, 70)
            );

            previousButton.onClick.AddListener(() =>
            {
                PreviousSound();
                UpdateMenuDisplay();
            });

            nextButton = CreateStyledButton(
                panel.transform,
                "›",
                new Vector2(315, 48),
                new Vector2(70, 70)
            );

            nextButton.onClick.AddListener(() =>
            {
                NextSound();
                UpdateMenuDisplay();
            });

            GameObject counter = CreateText(
                panel.transform,
                "",
                17,
                new Vector2(0, -18),
                SecondaryRedColor
            );

            soundCounterText =
                counter.GetComponent<TextMeshProUGUI>();

            soundCounterText.fontStyle = FontStyles.Bold;

            previewButton = CreateStyledButton(
                panel.transform,
                ">  PREVIEW SOUND",
                new Vector2(0, -72),
                new Vector2(300, 48)
            );

            previewButton.onClick.AddListener(PlaySelectedSound);

            CreateDivider(
                panel.transform,
                new Vector2(0, -115)
            );

            saveButton = CreateStyledButton(
                panel.transform,
                ">  SAVE",
                new Vector2(-125, -165),
                new Vector2(190, 50)
            );

            saveButton.onClick.AddListener(SaveSelection);

            closeButton = CreateStyledButton(
                panel.transform,
                ">  CLOSE",
                new Vector2(125, -165),
                new Vector2(190, 50)
            );

            closeButton.onClick.AddListener(CloseMenu);

            menu.SetActive(false);
        }

        private static void UpdateMenuDisplay()
        {
            if (soundNameText == null)
                return;

            if (SoundIDs.Count == 0)
            {
                soundNameText.text = "NO SOUNDS FOUND";

                if (soundCounterText != null)
                    soundCounterText.text = "0 / 0";

                SetButtonInteractable(previousButton, false);
                SetButtonInteractable(nextButton, false);
                SetButtonInteractable(previewButton, false);
                SetButtonInteractable(saveButton, false);

                return;
            }

            if (soundIndex < 0)
                soundIndex = 0;

            if (soundIndex >= SoundIDs.Count)
                soundIndex = 0;

            SelectedSound = SoundIDs[soundIndex];

            soundNameText.text =
                FormatSoundName(SelectedSound);

            if (soundCounterText != null)
            {
                soundCounterText.text =
                    "SOUND " +
                    (soundIndex + 1) +
                    " / " +
                    SoundIDs.Count;
            }

            SetButtonInteractable(
                previousButton,
                SoundIDs.Count > 1
            );

            SetButtonInteractable(
                nextButton,
                SoundIDs.Count > 1
            );

            SetButtonInteractable(
                previewButton,
                true
            );

            SetButtonInteractable(
                saveButton,
                true
            );
        }

        private static void SaveSelection()
        {
            DeathTunesPlugin.Log.LogInfo("Saving death sound...");

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
                return;
            }

            SendPlayerSound(localPlayer);
        }

        public static void CloseMenu()
        {
            DeathTunesPlugin.Log.LogInfo(
                "Closing DeathTunes customization menu."
            );

            if (menu != null)
                menu.SetActive(false);
        }

        public static void NextSound()
        {
            if (SoundIDs.Count == 0)
                return;

            soundIndex++;

            if (soundIndex >= SoundIDs.Count)
                soundIndex = 0;

            SelectedSound = SoundIDs[soundIndex];

            DeathTunesPlugin.Log.LogInfo(
                "Changed selected sound to: " + SelectedSound
            );
        }

        public static void PreviousSound()
        {
            if (SoundIDs.Count == 0)
                return;

            soundIndex--;

            if (soundIndex < 0)
                soundIndex = SoundIDs.Count - 1;

            SelectedSound = SoundIDs[soundIndex];

            DeathTunesPlugin.Log.LogInfo(
                "Changed selected sound to: " + SelectedSound
            );
        }

        private static void PlaySelectedSound()
        {
            DeathTunesPlugin.Log.LogInfo(
                "Previewing death sound: " + SelectedSound
            );

            if (Sounds.ContainsKey(SelectedSound))
            {
                DeathTunesPlugin.PlayClip(
                    Sounds[SelectedSound]
                );
            }
            else
            {
                DeathTunesPlugin.Log.LogWarning(
                    "Cannot preview sound. Sound not found: " +
                    SelectedSound
                );
            }
        }

        private static GameObject CreatePanel(
            Transform parent,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            GameObject obj = new GameObject("Panel");

            obj.transform.SetParent(parent, false);

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = obj.AddComponent<Image>();
            image.color = color;

            return obj;
        }

        private static GameObject CreateText(
            Transform parent,
            string value,
            int size,
            Vector2 position,
            Color color)
        {
            GameObject obj = new GameObject("Text");

            obj.transform.SetParent(parent, false);

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 50);
            rect.anchoredPosition = position;

            TextMeshProUGUI text =
                obj.AddComponent<TextMeshProUGUI>();

            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;

            if (gameFont != null)
                text.font = gameFont;

            return obj;
        }

        private static Button CreateStyledButton(
            Transform parent,
            string value,
            Vector2 position,
            Vector2 size)
        {
            GameObject obj = new GameObject("Button");

            obj.transform.SetParent(parent, false);

            RectTransform rect =
                obj.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            GameObject border = new GameObject("Border");
            border.transform.SetParent(obj.transform, false);

            RectTransform borderRect =
                border.AddComponent<RectTransform>();

            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            Image borderImage =
                border.AddComponent<Image>();

            borderImage.color = RedColor;
            borderImage.raycastTarget = false;

            GameObject background =
                new GameObject("Background");

            background.transform.SetParent(obj.transform, false);

            RectTransform backgroundRect =
                background.AddComponent<RectTransform>();

            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = new Vector2(2f, 2f);
            backgroundRect.offsetMax = new Vector2(-2f, -2f);

            Image backgroundImage =
                background.AddComponent<Image>();

            backgroundImage.color = BlackColor;

            Button button =
                obj.AddComponent<Button>();

            button.transition = Selectable.Transition.None;
            button.targetGraphic = backgroundImage;

            bool isArrow =
                value == "‹" ||
                value == "›";

            Color textColor =
                isArrow
                    ? WhiteColor
                    : RedColor;

            GameObject textObject =
                CreateText(
                    background.transform,
                    value,
                    isArrow ? 38 : 18,
                    Vector2.zero,
                    textColor
                );

            TextMeshProUGUI text =
                textObject.GetComponent<TextMeshProUGUI>();

            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;

            ButtonHoverHandler hoverHandler =
                obj.AddComponent<ButtonHoverHandler>();

            hoverHandler.Background = backgroundImage;
            hoverHandler.Text = text;
            hoverHandler.IsArrow = isArrow;
            hoverHandler.NormalBackgroundColor = BlackColor;
            hoverHandler.HoverBackgroundColor = RedColor;
            hoverHandler.NormalTextColor =
                isArrow ? WhiteColor : RedColor;
            hoverHandler.HoverTextColor =
                isArrow ? WhiteColor : BlackColor;

            return button;
        }

        private class ButtonHoverHandler :
            MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler
        {
            public Image Background;
            public TextMeshProUGUI Text;
            public bool IsArrow;
            public Color NormalBackgroundColor;
            public Color HoverBackgroundColor;
            public Color NormalTextColor;
            public Color HoverTextColor;

            public void OnPointerEnter(
                PointerEventData eventData)
            {
                if (IsArrow)
                    return;

                if (Background != null)
                    Background.color = HoverBackgroundColor;

                if (Text != null)
                    Text.color = HoverTextColor;
            }

            public void OnPointerExit(
                PointerEventData eventData)
            {
                if (Background != null)
                    Background.color = NormalBackgroundColor;

                if (Text != null)
                    Text.color = NormalTextColor;
            }
        }

        private static void AddBorder(
            GameObject target,
            Color color,
            float thickness)
        {
            Outline outline =
                target.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance =
                new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;
        }

        private static void CreateDivider(
            Transform parent,
            Vector2 position)
        {
            GameObject divider =
                new GameObject("Divider");

            divider.transform.SetParent(parent, false);

            RectTransform rect =
                divider.AddComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 1);
            rect.anchoredPosition = position;

            Image image =
                divider.AddComponent<Image>();

            image.color =
                new Color32(247, 0, 7, 180);
        }

        private static void SetButtonInteractable(
            Button button,
            bool interactable)
        {
            if (button == null)
                return;

            button.interactable = interactable;
        }

        private static string FormatSoundName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "UNKNOWN SOUND";

            string formatted =
                id.Replace("_", " ")
                  .Replace("-", " ");

            string[] words =
                formatted.Split(
                    new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                );

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0)
                    continue;

                words[i] =
                    char.ToUpper(words[i][0]) +
                    (
                        words[i].Length > 1
                            ? words[i].Substring(1).ToLower()
                            : ""
                    );
            }

            return string.Join(" ", words).ToUpper();
        }
    }
}