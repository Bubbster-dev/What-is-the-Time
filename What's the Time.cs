using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using System;

namespace TimeDisplayMod
{
    // Client-side only: shows your computer's local time above your left hand.
    // Nothing is sent over the network.
    [BepInPlugin("com.example.timedisplaymod", "Time Display", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // ---- Tweak these ----
        private const bool HoldToShow = true;            // true = visible while X is held, false = X toggles on/off
        private const string TimeFormat = "h:mm tt";     // e.g. "3:45 PM". Use "HH:mm" for 24-hour.
        private const float HeightAboveHand = 0.15f;     // meters above your left hand
        private const float TextScale = 0.012f;
        // ---------------------

        private GameObject textObj;
        private TextMesh textMesh;
        private bool toggled;
        private bool wasPressed;

        private void Update()
        {
            var tagger = GorillaTagger.Instance;
            if (tagger == null) { LogOnce("GorillaTagger.Instance is null (waiting for game to load)"); return; }
            if (tagger.leftHandTransform == null) { LogOnce("leftHandTransform is null"); return; }

            EnsureText();

            // X button = primary button on the left controller.
            // Method 1: the game's own input reader. Method 2: Unity XR as a fallback.
            bool pressed = false;
            if (ControllerInputPoller.instance != null)
                pressed = ControllerInputPoller.instance.leftControllerPrimaryButton;
            if (!pressed)
            {
                InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                if (left.isValid) left.TryGetFeatureValue(CommonUsages.primaryButton, out pressed);
            }
            if (pressed && !wasPressed) Logger.LogInfo("X button press detected");

            if (!HoldToShow && pressed && !wasPressed) toggled = !toggled;
            wasPressed = pressed;

            bool visible = HoldToShow ? pressed : toggled;
            textObj.SetActive(visible);
            if (!visible) return;

            textMesh.text = DateTime.Now.ToString(TimeFormat);
            textObj.transform.position = tagger.leftHandTransform.position + Vector3.up * HeightAboveHand;
        }

        // Runs after all movement so the text never lags behind your view
        private void LateUpdate()
        {
            if (textObj == null || !textObj.activeSelf) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            // Always face the player's camera
            textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - cam.transform.position);
        }

        private string lastLog;
        private void LogOnce(string msg)
        {
            if (msg == lastLog) return;
            lastLog = msg;
            Logger.LogInfo(msg);
        }

        private void Awake()
        {
            Logger.LogInfo("Time Display loaded");
        }

        private void EnsureText()
        {
            if (textObj != null) return;

            textObj = new GameObject("TimeDisplayText");
            textObj.transform.localScale = Vector3.one * TextScale;

            textMesh = textObj.AddComponent<TextMesh>();
            textMesh.fontSize = 64;
            textMesh.characterSize = 1f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            textObj.SetActive(false);
        }

        private void OnDestroy()
        {
            if (textObj != null) Destroy(textObj);
        }
    }
}
