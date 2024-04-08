using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx;
using Utilla;
using System;

namespace Jet_Engine
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private bool inRoom;
        private bool modDisabled = false;

        private GameObject gliderWindClone;
        private float movementSpeed = 10.0f; // Adjust this value to control the speed of movement
        private Vector3 offset = new Vector3(-19f, -10f, -18f); // Adjust these values as needed
        Vector3 targetPos;

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnDestroy()
        {
            Utilla.Events.GameInitialized -= OnGameInitialized;
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            Debug.Log("[Monke Thermals] Game Initialized");
            StartCoroutine(LoadAndCloneWindObject());
        }

        private IEnumerator LoadAndCloneWindObject()
        {
            Debug.Log("[Monke Thermals] Loading Skyjungle scene additively.");
            yield return SceneManager.LoadSceneAsync("Skyjungle", LoadSceneMode.Additive);
            Debug.Log("[Monke Thermals] Skyjungle scene loaded.");

            CloneGliderWindObject();
            Debug.Log("[Monke Thermals] Attempting to unload Skyjungle scene.");

            yield return SceneManager.UnloadSceneAsync("Skyjungle");
            Debug.Log("[Monke Thermals] Skyjungle scene unloaded.");
        }

        private void CloneGliderWindObject()
        {
            GameObject windObject = GameObject.Find("skyjungle/SmallMap_OffCenter_Prefab_2/ForceVols_Thermals_Border_MidMap");
            if (windObject != null)
            {
                gliderWindClone = Instantiate(windObject);

                if (Camera.main != null)
                {
                    // Set the position and rotation of the cloned object
                    gliderWindClone.transform.position = Camera.main.transform.position;
                    gliderWindClone.transform.localRotation = Camera.main.transform.localRotation;
                }
                else
                {
                    Debug.LogError("[Monke Thermals] Main Camera not found!");
                }

                gliderWindClone.tag = "Glider Wind";
                gliderWindClone.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // Adjust scale if necessary
                gliderWindClone.SetActive(false);
                Debug.Log("[Monke Thermals] Wind object cloned successfully.");
            }
            else
            {
                Debug.LogError("[Monke Thermals] Wind object not found in Skyjungle scene.");
            }
        }

        void Update()
        {
            UpdateTargetPosition();
            float V = ControllerInputPoller.TriggerFloat(UnityEngine.XR.XRNode.RightHand);

            if (inRoom && !modDisabled)
            {
                if (gliderWindClone == null)
                {
                    Debug.LogError("[Monke Thermals] Error: gliderWindClone is null!");
                    return;
                }

                if (V >= 0.05f)
                {
                    ActivateGliderWindClone();
                }
                else
                {
                    DeactivateGliderWindClone();
                }
            }
        }

        private void UpdateTargetPosition()
        {
            if (Camera.main != null)
            {
                if (gliderWindClone != null && gliderWindClone.activeSelf)
                {
                    // Calculate the target position with offset
                    Vector3 targetPosition = Camera.main.transform.position + offset;

                    // Calculate new position using Lerp for smooth transition
                    Vector3 newPosition = Vector3.Lerp(gliderWindClone.transform.position, targetPosition, Time.deltaTime * movementSpeed);

                    // Set the new position
                    gliderWindClone.transform.position = newPosition;
                }
            }
            else
            {
                Debug.LogError("[Monke Thermals] Error: Main Camera not found!");
            }
        }

        private void ActivateGliderWindClone()
        {
            if (!gliderWindClone.activeSelf)
            {
                gliderWindClone.SetActive(true);
            }
        }

        private void DeactivateGliderWindClone()
        {
            if (gliderWindClone.activeSelf)
            {
                gliderWindClone.SetActive(false);
            }
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            inRoom = false;
        }
    }
}
