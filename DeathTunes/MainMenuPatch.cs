using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace DeathTunes
{
    [HarmonyPatch(typeof(MenuManager))]
    public class MainMenuPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start(MenuManager __instance)
        {
            if (__instance.isInitScene)
                return;

            DeathTunesPlugin.Log.LogInfo(
                "DeathTunes injecting menu..."
            );

            __instance.StartCoroutine(
                DelayedInjection()
            );
        }

        private static IEnumerator DelayedInjection()
        {
            yield return new WaitForSeconds(1f);

            InjectMenu();
        }

        private static void InjectMenu()
        {
            GameObject menuContainer =
                GameObject.Find(
                    "MenuContainer"
                );

            if (menuContainer == null)
                return;

            Transform mainButtons =
                menuContainer.transform.Find(
                    "MainButtons"
                );

            if (mainButtons == null)
                return;

            Transform quitButton =
                mainButtons.Find(
                    "QuitButton"
                );

            if (quitButton == null)
            {
                DeathTunesPlugin.Log.LogError(
                    "QuitButton missing!"
                );

                return;
            }

            DeathCustomizationMenuUtils.InjectMenu(
                mainButtons,
                quitButton.gameObject
            );
        }
    }
}