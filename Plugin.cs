﻿using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;
using Vilar.Entity;

namespace archi_mods
{
    [BepInPlugin("husko.archipelagates.cheats", "Archipelagates Cheats", MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private enum Tab
        {
            MainCheats,
            SpecialCheats
        }

        private Tab _currentTab = Tab.MainCheats;
        private bool _showMenu;
        private Rect _menuRect = new(20, 20, 430, 200); // Initial position and size of the menu

        // Define separate arrays to store activation status for each tab
        private readonly bool[] _mainCheatsActivated = new bool[8];
        private readonly bool[] _specialCheatsActivated = new bool[2]; // Adjust the size as per your requirement

        // Default values

        private const string VersionLabel = MyPluginInfo.PLUGIN_VERSION;
        private List<EntityCharacter.Faction> _availableFactions = new List<EntityCharacter.Faction>();
        private int _selectedFactionIndex;

        // List to store button labels and corresponding actions for the current cheats tab
        private readonly List<(string label, Action action)> _mainCheatsButtonActions = new()
        {
            ("Invincibility", ToggleInvincibility),
            // Add more buttons and actions here
        };

        // Modify the ghostModeButtonActions list to include a button for Special Cheats
        private readonly List<(string label, Action action)> _specialCheatsButtonActions = new()
        {
            ("Clothed Skeleton Mode", ToggleSkeletonModeClothed),
            ("Naked Skeleton Mode", ToggleSkeletonModeNaked),
            // Add more buttons for Special Cheats here
        };

        /// <summary>
        /// Initializes the plugin on Awake event
        /// </summary>
        private void Awake()
        {
            // Log the plugin's version number and successful startup
            Logger.LogInfo($"Plugin Archipelagates Cheat v{VersionLabel} loaded!");
            
            // Fetch available factions
            FetchAvailableFactions();
        }

        /// <summary>
        /// Handles toggling the menu on and off with the Insert or F1 key.
        /// </summary>
        private void Update()
        {
            // Toggle menu visibility with Insert or F1 key
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.F1))
            {
                _showMenu = !_showMenu;
            }
        }

        /// <summary>
        /// Handles drawing the menu and all of its elements on the screen.
        /// </summary>
        private void OnGUI()
        {
            // Only draw the menu if it's supposed to be shown
            if (_showMenu)
            {
                // Apply dark mode GUI style
                GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

                // Draw the IMGUI window
                _menuRect = GUI.Window(0, _menuRect, MenuWindow, "----< Cheats Menu >----");

                // Calculate position for version label at bottom left corner
                float versionLabelX = _menuRect.xMin + 10; // 10 pixels from left edge
                float versionLabelY = _menuRect.yMax - 20; // 20 pixels from bottom edge

                // Draw version label at bottom left corner
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f); // Dark grey silver color
                GUI.Label(new Rect(versionLabelX, versionLabelY, 100, 20), "v" + VersionLabel);

                // Calculate the width of the author label
                float authorLabelWidth =
                    GUI.skin.label.CalcSize(new GUIContent("by Official-Husko")).x +
                    10; // Add some extra width for padding

                // Calculate position for author label at bottom right corner
                float authorLabelX = _menuRect.xMax - authorLabelWidth; // 10 pixels from right edge
                float authorLabelY = versionLabelY + 2; // Align with version label

                // Draw the author label as a clickable label
                if (GUI.Button(new Rect(authorLabelX, authorLabelY, authorLabelWidth, 20),
                        "<color=cyan>by</color> <color=yellow>Official-Husko</color>", GUIStyle.none))
                {
                    // Open a link in the user's browser when the label is clicked
                    Application.OpenURL("https://github.com/Official-Husko/gamer-struggles-cheats");
                }
            }
        }

        /// <summary>
        /// Handles the GUI for the main menu
        /// </summary>
        /// <param name="windowID">The ID of the window</param>
        private void MenuWindow(int windowID)
        {
            // Make the whole window draggable
            GUI.DragWindow(new Rect(0, 0, _menuRect.width, 20));

            // Begin a vertical group for menu elements
            GUILayout.BeginVertical();

            // Draw tabs
            GUILayout.BeginHorizontal();
            // Draw the Main Cheats tab button
            DrawTabButton(Tab.MainCheats, "Main Cheats");
            // Draw the Special Cheats tab button
            DrawTabButton(Tab.SpecialCheats, "Special Cheats");
            GUILayout.EndHorizontal();

            // Draw content based on the selected tab
            switch (_currentTab)
            {
                // Draw the Main Cheats tab
                case Tab.MainCheats:
                    DrawMainCheatsTab();
                    break;
                // Draw the Special Cheats tab
                case Tab.SpecialCheats:
                    DrawSpecialCheatsTab();
                    break;
            }

            // End the vertical group
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a tab button
        /// </summary>
        /// <param name="tab">The tab to draw</param>
        /// <param name="label">The label to display on the button</param>
        private void DrawTabButton(Tab tab, string label)
        {
            // Change background color based on the selected tab
            GUI.backgroundColor = _currentTab == tab ? Color.white : Color.grey;

            // If the button is clicked, set the current tab to the clicked tab
            if (GUILayout.Button(label))
            {
                _currentTab = tab;
            }
        }

        /// <summary>
        /// Gets the activation status array for the currently selected tab
        /// </summary>
        /// <returns>The activation status array for the current tab. If the tab is not recognized, null is returned.</returns>
        private bool[] GetCurrentTabActivationArray()
        {
            switch (_currentTab)
            {
                case Tab.MainCheats:
                    // Return the activation status array for the main cheats tab
                    return _mainCheatsActivated;
                case Tab.SpecialCheats:
                    // Return the activation status array for the special cheats tab
                    return _specialCheatsActivated;
                default:
                    // If the tab is not recognized, return null
                    return null;
            }
        }

        /// <summary>
        /// Toggles the activation state of the button at the given index on the currently selected tab.
        /// If the index is not within the range of the activation status array for the current tab, nothing is done.
        /// </summary>
        /// <param name="buttonIndex">The index of the button to toggle activation status for</param>
        private void ToggleButtonActivation(int buttonIndex)
        {
            // Get the activation status array for the current tab. If the tab is not recognized, return.
            bool[] currentTabActivationArray = GetCurrentTabActivationArray();
            if (currentTabActivationArray == null)
            {
                return;
            }

            // If the index is within the range of the activation status array, toggle the activation status
            if (buttonIndex >= 0 && buttonIndex < currentTabActivationArray.Length)
            {
                currentTabActivationArray[buttonIndex] = !currentTabActivationArray[buttonIndex];
            }
        }

        /// <summary>
        /// Method to draw content for the Main Cheats tab
        /// </summary>
        private void DrawMainCheatsTab()
        {
            GUILayout.BeginVertical();

            // Draw buttons from the list
            for (int i = 0; i < _mainCheatsButtonActions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                DrawActivationDot(_mainCheatsActivated[i]); // Draw activation dot based on activation status

                // Draws a button for each cheat with the label, 
                // activation status, and invokes the action associated 
                // with the button when pressed
                if (GUILayout.Button(_mainCheatsButtonActions[i].label))
                {
                    ToggleButtonActivation(i); // Toggle activation status
                    _mainCheatsButtonActions[i].action.Invoke(); // Invoke the action associated with the button
                }

                GUILayout.EndHorizontal();
            }

            // End the vertical layout
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the Special Cheats tab in the mod's UI
        /// </summary>
        private void DrawSpecialCheatsTab()
        {
            // Begin vertical layout for the tab
            GUILayout.BeginVertical();

            // Iterate through the list of special cheat buttons
            for (int i = 0; i < _specialCheatsButtonActions.Count; i++)
            {
                // Begin horizontal layout for the button row
                GUILayout.BeginHorizontal();

                // Draw an activation dot based on the activation status
                DrawActivationDot(_specialCheatsActivated[i]);

                // Draw a button for the special cheat
                if (GUILayout.Button(_specialCheatsButtonActions[i].label))
                {
                    // Toggle the activation status of the button
                    ToggleButtonActivation(i);

                    // Invoke the action associated with the button
                    _specialCheatsButtonActions[i].action.Invoke();
                }

                // End the horizontal layout for the button row
                GUILayout.EndHorizontal();
            }

            // Draw save game button
            DrawSaveGameButton();
            
            // Draw Teleport button
            DrawTeleportNpcsButton();
            
            // faction button
            DrawFactionsOption();
            
            // End the vertical layout for the tab
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a small dot with a green color if the activation status is true, and red if it's false.
        /// This method uses the current tab activation status array to determine the dot color.
        /// </summary>
        /// <param name="activated">The activation status to determine the dot color.</param>
        private void DrawActivationDot(bool activated)
        {
            GetCurrentTabActivationArray(); // Consider current tab activation status array
            GUILayout.Space(10); // Add some space to center the dot vertically
            Color dotColor = activated ? Color.green : Color.red; // Determine dot color based on activation status
            GUIStyle dotStyle = new GUIStyle(GUI.skin.label); // Create a new GUIStyle for the dot label
            dotStyle.normal.textColor = dotColor; // Set the color of the dot label
            GUILayout.Label("●", dotStyle, GUILayout.Width(20),
                GUILayout.Height(20)); // Draw dot with the specified style
        }

        private void DrawBlueDot()
        {
            GUILayout.Space(10); // Add some space to center the dot vertically
            Color blueDotColor = new Color(0.0f, 0.5f, 1.0f); // blue because nice
            GUIStyle dotStyle = new GUIStyle(GUI.skin.label); // Create a new GUIStyle for the dot label
            dotStyle.normal.textColor = blueDotColor; // Set the color of the dot label
            GUILayout.Label("●", dotStyle, GUILayout.Width(20),
                GUILayout.Height(20)); // Draw dot with the specified style
        }

        /*
         Below here are all the code related things for the cheats itself.
        */

        private static void ToggleInvincibility()
        {
            // Debug log the action being performed
            Debug.Log("Toggle Invincibility");

            // Find the "PlayerStuff(Clone)" GameObject
            GameObject playerStuff = GameObject.Find("Player(Clone)");
            if (playerStuff != null)
            {
                // find component EntityCharacter
                EntityCharacter entityCharacter = playerStuff.GetComponent<EntityCharacter>();
                if (entityCharacter != null)
                {
                    // Use reflection to access the private field _invulnerable
                    FieldInfo fieldInfo = typeof(EntityCharacter).GetField("_invulnerable",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        // Toggle invincibility
                        bool currentValue = (bool)fieldInfo.GetValue(entityCharacter);
                        fieldInfo.SetValue(entityCharacter, !currentValue);
                    }
                }
            }
        }

        private void FetchAvailableFactions()
        {
            Type entityCharacterType = typeof(EntityCharacter);
            Type factionType = entityCharacterType.GetNestedType("Faction");
            if (factionType != null && factionType.IsEnum)
            {
                Array factionValues = Enum.GetValues(factionType);
                foreach (var value in factionValues)
                {
                    _availableFactions.Add((EntityCharacter.Faction)value);
                }
            }
        }

        private void DrawFactionsOption()
        {
            GUILayout.BeginHorizontal();
            
            // Draw the dot
            DrawBlueDot();

            // Label for the selection
            GUILayout.Label("Change Faction:");

            // Scroll view for the selection
            _selectedFactionIndex = GUILayout.SelectionGrid(_selectedFactionIndex, _availableFactions.ConvertAll(x => x.ToString()).ToArray(), 1, GUILayout.Width(100));

            if (GUILayout.Button("Execute"))
            {
                // Check if a valid faction is selected
                if (_selectedFactionIndex >= 0 && _selectedFactionIndex < _availableFactions.Count)
                {
                    // Get the selected faction from availableFactions
                    EntityCharacter.Faction selectedFaction = _availableFactions[_selectedFactionIndex];

                    // Find the player character
                    GameObject playerObject = GameObject.Find("Player(Clone)");
                    if (playerObject != null)
                    {
                        // Get the EntityCharacter component
                        EntityCharacter entityCharacter = playerObject.GetComponent<EntityCharacter>();
                        if (entityCharacter != null && entityCharacter.data != null)
                        {
                            // Change the faction
                            entityCharacter.data.faction = selectedFaction;
                        }
                        else
                        {
                            Debug.LogError("EntityCharacter component or data not found.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Player character not found.");
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        
        private static void ToggleSkeletonModeClothed()
        {
            // Debug log the action being performed
            Debug.Log("Toggle Clothed Skeleton Mode");
            
            List<string> _validNames = new List<string>() { "Canine" };
            
            GameObject player = GameObject.Find("Player(Clone)");
            if (player != null)
            {
                Transform tumbler = player.transform.Find("Tumbler");
                if (tumbler != null)
                {
                    Transform charcreatechibi = tumbler.Find("charcreatechibi");
                    if (charcreatechibi != null)
                    {
                        foreach (string name in _validNames)
                        {
                            Transform body = charcreatechibi.Find("Body_" + name);
                            if (body != null)
                            {
                                body.gameObject.SetActive(!body.gameObject.activeSelf);
                            }
                        }
                        Transform skeleton = charcreatechibi.Find("Body_Skeleton");
                        if (skeleton != null)
                        {
                            skeleton.gameObject.SetActive(!skeleton.gameObject.activeSelf);
                        }
                    }
                }
            }
        }
        
        private static void ToggleSkeletonModeNaked()
        {
            // Debug log the action being performed
            Debug.Log("Toggle Naked Skeleton Mode");
            
            List<string> _validNames = new List<string>() { "Canine" };
            
            GameObject player = GameObject.Find("Player(Clone)");
            if (player != null)
            {
                Transform tumbler = player.transform.Find("Tumbler");
                if (tumbler != null)
                {
                    Transform charcreatechibi = tumbler.Find("charcreatechibi");
                    if (charcreatechibi != null)
                    {
                        foreach (string name in _validNames)
                        {
                            Transform body = charcreatechibi.Find("Body_" + name);
                            if (body != null)
                            {
                                body.gameObject.SetActive(!body.gameObject.activeSelf);
                            }
                        }
                        foreach (string name in _validNames)
                        {
                            Transform clothing = charcreatechibi.Find("Body_" + name + "_Clothes");
                            if (clothing != null)
                            {
                                clothing.gameObject.SetActive(!clothing.gameObject.activeSelf);
                            }
                        }
                        Transform skeleton = charcreatechibi.Find("Body_Skeleton");
                        if (skeleton != null)
                        {
                            skeleton.gameObject.SetActive(!skeleton.gameObject.activeSelf);
                        }
                    }
                }
            }
        }
        
        private void DrawSaveGameButton()
        {
            GUILayout.BeginHorizontal();

            // Draw the dot
            DrawBlueDot();
            
            if (GUILayout.Button("Save Game"))
            {
                Game.Save();
            }
            GUILayout.EndHorizontal();
        }
        
        private void DrawTeleportNpcsButton()
        {
            GUILayout.BeginHorizontal();

            // Draw the dot
            DrawBlueDot();
            
            if (GUILayout.Button("Teleport All NPCs"))
            {
                // Find the player GameObject
                GameObject player = GameObject.Find("Player(Clone)");
                if (player != null)
                {
                    // Get the player's position
                    Vector3 playerPosition = player.transform.position;

                    // Find all game objects with the name "Entity(Clone)"
                    Entity[] entities = GameObject.FindObjectsOfType<Entity>();
                    foreach (Entity entity in entities)
                    {
                        // Teleport the entity to the player's position
                        entity.transform.position = playerPosition;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}