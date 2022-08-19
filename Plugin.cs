using BepInEx;
using Eremite;
using Eremite.Controller;
using Eremite.Services;
using Eremite.View.Cameras;
using Eremite.View.Popups.GameMenu;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StormUITweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        private static Plugin Instance;
        public static void LogInfo(object obj) => Instance.Logger.LogInfo(obj);

        private void Awake()
        {
            Instance = this;

            harmony = new Harmony("bubblestorm");

            harmony.PatchAll(typeof(Plugin));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        }

        private static Slider zoomSlider = null;
        private static float maxZoomFactor = -20;
        private static CameraController currentGameCam = null;

        [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetValues))]
        [HarmonyPostfix]
        private static void OptionsPopup_SetValues(OptionsPopup __instance)
        {
            if (Serviceable.PrefsService.HasKey("BubblePrefs.maxZoomFactor"))
            {
                maxZoomFactor = Serviceable.PrefsService.GetFloat("BubblePrefs.maxZoomFactor");
                zoomSlider.value = -maxZoomFactor;
            }
        }

        [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.SetUpInputs))]
        [HarmonyPostfix]
        private static void OptionsPopup_SetUpInputs(OptionsPopup __instance)
        {
            zoomSlider.OnValueChangedAsObservable().Subscribe(newValue =>
            {
                maxZoomFactor = -newValue;
                if (currentGameCam != null)
                    currentGameCam.zoomLimit = new(maxZoomFactor, -8f);
                Serviceable.PrefsService.SetFloat("BubblePrefs.maxZoomFactor", maxZoomFactor);
            }).AddTo(__instance);
        }

        [HarmonyPatch(typeof(OptionsPopup), nameof(OptionsPopup.Initialize))]
        [HarmonyPrefix]
        private static void OptionsPopup_InitializePre(OptionsPopup __instance)
        {
            var prefab = __instance.cameraMouseSensitivitySlider.transform.parent.gameObject;

            var mySlider = GameObject.Instantiate(prefab);
            var rect = mySlider.transform as RectTransform;
            rect.SetParent(prefab.transform.parent, true);
            rect.localScale = Vector3.one;
            rect.localPosition = new(0, rect.localPosition.y, 0);
            rect.localRotation = Quaternion.identity;
            rect.SetSiblingIndex(prefab.transform.GetSiblingIndex());

            var slider = rect.GetComponentInChildren<Slider>();
            slider.minValue = 20;
            slider.maxValue = 50;
            slider.value = 20;

            zoomSlider = slider;

            mySlider.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = "Maximum zoom distance";
        }

        [HarmonyPatch(typeof(CameraController), nameof(CameraController.Start))]
        [HarmonyPostfix]
        private static void CameraController_Start(CameraController __instance)
        {
            __instance.zoomLimit = new(maxZoomFactor, -8f);
            currentGameCam = __instance;
        }

    }
}
