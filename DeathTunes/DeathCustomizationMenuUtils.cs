using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeathTunes
{
    public static class DeathCustomizationMenuUtils
    {
        public static void InjectMenu(
            Transform mainButtonsTransform,
            GameObject quitButton)
        {
            DeathTunesPlugin.Log.LogInfo(
                "Injecting DeathTunes button..."
            );

            DeathCustomizationManager.OpenMenu();
            DeathCustomizationManager.CloseMenu();

            GameObject deathButton =
                Object.Instantiate(
                    quitButton,
                    mainButtonsTransform
                );

            deathButton.name =
                "DeathTunesButton";

            Button button =
                deathButton.GetComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick =
                new Button.ButtonClickedEvent();

            button.onClick.AddListener(() =>
            {
                DeathCustomizationManager.OpenMenu();
            });

            TextMeshProUGUI text =
                deathButton.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                text.text =
                    "> Death Sounds";
            }

            List<GameObject> buttons =
                mainButtonsTransform
                .GetComponentsInChildren<Button>()
                .Select(b => b.gameObject)
                .ToList();

            List<float> positions =
                buttons
                .Where(b => b != deathButton)
                .Select(b =>
                    b.GetComponent<RectTransform>()
                     .anchoredPosition.y)
                .ToList();

            float spacing = 55f;

            if (positions.Count >= 2)
            {
                spacing =
                    positions
                    .Zip(
                        positions.Skip(1),
                        (a, b) => Mathf.Abs(b - a)
                    )
                    .Min();
            }

            foreach (GameObject obj in buttons.Where(g => g != quitButton))
            {
                RectTransform rect =
                    obj.GetComponent<RectTransform>();

                rect.anchoredPosition +=
                    new Vector2(
                        0,
                        spacing
                    );
            }

            RectTransform quitRect =
                quitButton.GetComponent<RectTransform>();

            RectTransform deathRect =
                deathButton.GetComponent<RectTransform>();

            deathRect.anchoredPosition =
                quitRect.anchoredPosition +
                new Vector2(
                    0,
                    spacing
                );

            DeathTunesPlugin.Log.LogInfo(
                "DeathTunes button positioned successfully."
            );
        }
    }
}