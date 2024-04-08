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

        private bool AButton; // A button variable
        private float RightTriggerV; // Right Trigger variable

        private GameObject gliderWindClone; // Wind Stream Object
        private float movementSpeed = 10.0f; // Adjust this value to control the speed in which the wind follows the player
        
        private Vector3 vertOffset = new Vector3(-19f, -10f, -18f); // Updraft position offset
        private Vector3 boostPosOffset = new Vector3(-19f, -10f, -18f); // Boost position offset
        private Quaternion boostRotOffset = new Quaternion(0f, 90f, 0f, 0f); // pseudocode
        
        private Vector3 vertTargetPos; // Target Position (for the updraft) based on the Main Camera + the offset
        private Vector3 boostTargetPos; // Target Position (for the boost) based on the Main Camera + the offset

        private bool isBoosting = false;

        private float defaultUpdraftStrength = gliderWind // need to find the variable for that strength

        private float updraftStrength = 0f;

        private float boostStrength = 0f;

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
            StartCoroutine(LoadAndCloneWindObjects()); // Begin the cloning sequence as soon as the game loads
        }

        private IEnumerator LoadAndCloneWindObjects()
        {
            Debug.Log("[Monke Thermals] Loading Skyjungle scene additively.");
            yield return SceneManager.LoadSceneAsync("Skyjungle", LoadSceneMode.Additive); // Load the scene on top of the preexisting scene 'GorillaTag'
            Debug.Log("[Monke Thermals] Skyjungle scene loaded.");

            CloneVerticalGliderWindObject(); // Clone the vertical object
            CloneHorizontalBoostWindObject(); // Clone the boost object
            
            Debug.Log("[Monke Thermals] Attempting to unload Skyjungle scene.");

            yield return SceneManager.UnloadSceneAsync("Skyjungle"); // Unload the `Skyjungle` scene
            Debug.Log("[Monke Thermals] Skyjungle scene unloaded.");
        }

        private void CloneVerticalGliderWindObject()
        {
            GameObject vertWindObject = GameObject.Find("skyjungle/SmallMap_OffCenter_Prefab_2/ForceVols_Thermals_Border_MidMap");
            
            if (vertWindObject != null)
            {
                vertGliderWindClone = Instantiate(vertWindObject);

                if (Camera.main != null)
                {
                    // Set the position and rotation of the cloned object
                    vertGliderWindClone.transform.position = Camera.main.transform.position;
                    vertGliderWindClone.transform.localRotation = Camera.main.transform.localRotation;
                }
                else
                {
                    Debug.LogError("[Monke Thermals] Main Camera not found!");
                }

                vertGliderWindClone.tag = "Vertical Wind";
                vertGliderWindClone.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // Shrink the air stream
                vertGliderWindClone.SetActive(false);
                Debug.Log("[Monke Thermals] Vertical object cloned successfully.");
            }
            else
            {
                Debug.LogError("[Monke Thermals] Wind object not found in Skyjungle scene.");
            }
        }

        private void CloneHorizontalBoostWindObject()
        {
            GameObject boostWindObject = GameObject.Find("skyjungle/SmallMap_OffCenter_Prefab_2/ForceVols_Thermals_Border_MidMap");
            
            if (boostWindObject != null)
            {
                boostWindClone = Instantiate(boostWindObject);

                if (Camera.main != null)
                {
                    // Set the position and rotation of the cloned object
                    boostWindClone.transform.position = Camera.main.transform.position;
                    boostWindClone.transform.localRotation = Camera.main.rotation; // pseudocode
                }
                else
                {
                    Debug.LogError("[Monke Thermals] Main Camera not found!");
                }

                boostWindClone.tag = "Boost Wind";
                boostWindClone.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // Shrink the air stream
                boostWindClone.SetActive(false);
                Debug.Log("[Monke Thermals] Boost object cloned successfully.");
            }
            else
            {
                Debug.LogError("[Monke Thermals] Wind object not found in Skyjungle scene.");
            }
        }

        void Update()
        {
            RightTriggerV = ControllerInputPoller.TriggerFloat(UnityEngine.XR.XRNode.RightHand);
            AButton = ControllerInputPoller.A(UnityEngine.XR.XRNode.RightHand);

            if (inRoom && !modDisabled)
            {
                if (gliderWindClone == null)
                {
                    Debug.LogError("[Monke Thermals] Error: gliderWindClone is null!");
                    return;
                }

                //A Button Detection (Boost)
                if (AButton = true && !isBoosting)
                {
                    StartCoroutine(BoostPlayer()); // boost the monkey lmao
                }

                // Right Trigger Detection
                if (RightTriggerV >= 0.05f)
                {
                    UpdateVerticalTargetPosition(); // Move the updraft to the player pos + an offset
                    UpdateUpdraftStrength(); // Update the strength of the updraft
                    ActivateVerticalGliderWindClone(); // Activate the object
                }
                else
                {
                    ResetUpdraftStrength();
                    DeactivateVerticalGliderWindClone();
                }
            }
        }

        private void UpdateVerticalTargetPosition()
        {
            if (Camera.main != null)
            {
                if (gliderWindClone != null && gliderWindClone.activeSelf)
                {
                    // Calculate the target position with offset
                    vertTargetPos = Camera.main.transform.position + vertOffset;

                    // Calculate new position using Lerp for smooth transition
                    Vector3 newPosition = Vector3.Lerp(vertGliderWindClone.transform.position, vertTargetPos, Time.deltaTime * movementSpeed);

                    // Set the new position
                    gliderWindClone.transform.position = newPosition;
                }
            }
            else
            {
                Debug.LogError("[Monke Thermals] Error: Main Camera not found!");
            }
        }

        private void ActivateVerticalGliderWindClone()
        {
            if (!vertGliderWindClone.activeSelf)
            {
                vertGliderWindClone.SetActive(true);
            }
        }

        private void DeactivateVerticalGliderWindClone()
        {
            if (vertGliderWindClone.activeSelf)
            {
                vertGliderWindClone.SetActive(false);
            }
        }

        private void UpdateBoostTargetPosition()
        {
            if (Camera.main != null)
            {
                if (boostWindClone != null && boostWindClone.activeSelf)
                {
                    // Calculate the target position with offset
                    boostTargetPos = Camera.main.transform.position + boostPosOffset;
                    boostTargetRot = Camera.main.rotation + boostRotOffset

                    // Calculate new position using Lerp for smooth transition
                    Vector3 newPosition = Vector3.Lerp(boostWindClone.transform.position, boostTargetPos, Time.deltaTime * movementSpeed);

                    // Set the new position
                    boostWindClone.transform.position = newPosition;
                }
            }
            else
            {
                Debug.LogError("[Monke Thermals] Error: Main Camera not found!");
            }
        }

        private void ActivateBoostGliderWindClone()
        {
            if (!boostWindClone.activeSelf)
            {
                boostWindClone.SetActive(true);
            }
        }

        private void DeactivateBoostGliderWindClone()
        {
            if (boostWindClone.activeSelf)
            {
                boostWindClone.SetActive(false);
            }
        }

        private IEnumerator BoostPlayer()
        {
            isBoosting = true;
            ActivateBoostGliderWindClone();
    
            // Increase strength parameters gradually over time.
            // This is just a pseudocode example.
            for (float timer = 0; timer < 1.0f; timer += Time.deltaTime)
            {
                // Update strength based on timer, e.g., linearly increasing.
                UpdateBoostStrength(timer);
                yield return null;
            }
    
            // Keep the boost active for 2 seconds.
            yield return new WaitForSeconds(2);
    
            // Deactivate the boost and reset parameters.
            DeactivateBoostGliderWindClone();
            ResetBoostStrength(); // Implement this method to reset the strength to default.
    
            isBoosting = false;
        }

        private void UpdateBoostStrength(float timer)
        {
            // Placeholder logic for updating boost strength.
            // Replace this with actual implementation later.
            boostStrength = timer; // Example: linearly increase the boost strength over time.
        }

        private void ResetUpdraftStrength()
        {
            // Placeholder logic for resetting updraft strength.
            // Replace this with actual implementation later.
            updraftStrength = 0; // Reset the updraft strength to zero or to some default value.
        }
        
        private void UpdateUpdraftStrength()
        {
            // Placeholder logic for updating updraft strength.
            // Replace this with actual implementation later.
            updraftStrength = RightTriggerV * defaultUpdraftStrength; // Example calculation.
        }

        private void ResetBoostStrength()
        {
            boostStrength = 0;
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
