// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Runtime data for one player setup slot
/// Populated automatically by StartHubManager from the scene hierarchy
/// </summary>
public class PlayerSlotUI
{
    public TMP_InputField nameInput;

    public Button[] characterButtons;

    public Image readyIndicator;

    // Index of the chosen character button, or -1 if nothing is selected yet
    public int selectedCharacterIndex = -1;

    // True when the slot has a valid name and a character selected
    public bool isReady = false;
}

/// <summary>
/// Controls the CharacterSelection (player setup) scene
///
/// All UI references are discovered automatically at runtime by searching the hierarchy
/// using the configurable name strings in the Inspector — no drag-and-drop required
///
/// Flow:
///   1. FindReferences()   — locate panels, buttons, and inputs in the scene
///   2. InitializeSlots()  — wire up event listeners
///   3. Player interacts   — OnNameChanged / OnCharacterSelected update the ready state
///   4. OnStartButtonClicked() — saves selections to PlayerData and loads the main scene
/// </summary>
public class StartHubManager : MonoBehaviour
{
    #region Inspector

    [Header("Hierarchy Names")]
    [Tooltip("Exact name of the GameObject that holds all player slot children.")]
    [SerializeField] private string setupPanelName = "PlayerSetupPanel";

    [Tooltip("Exact name of the Start Game Button.")]
    [SerializeField] private string startButtonName = "StartGameButton";

    [Tooltip("Exact name of the TMP_InputField inside each slot.")]
    [SerializeField] private string nameInputName = "PlayerNameInput";

    [Tooltip("Exact name of the row that contains the four character buttons.")]
    [SerializeField] private string charButtonRowName = "CharButtonRow";

    [Tooltip("Names of the four character buttons inside CharButtonRow, in order.")]
    [SerializeField] private string[] charButtonNames = { "Yellow", "Green", "Red", "Purple" };

    [Tooltip("Exact name of the ready-indicator Image inside each slot.")]
    [SerializeField] private string readyImageName = "ReadyImage";

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    #endregion

    #region Private Fields

    private PlayerSlotUI[] playerSlots;
    private Button startButton;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        FindReferences();
        InitializeSlots();
        RefreshStartButton();
    }

    #endregion

    #region Reference Discovery

    /// <summary>
    /// Discovers all required UI references by searching the scene hierarchy
    /// using the name strings configured in the Inspector
    /// Logs descriptive warnings for any missing elements
    /// </summary>
    private void FindReferences()
    {
        // Start button
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in allButtons)
        {
            if (b.name == startButtonName)
            {
                startButton = b;
                break;
            }
        }

        if (startButton == null)
            Debug.LogError($"[StartHubManager] Button named '{startButtonName}' not found! " +
                           $"Check the name in the Inspector.");

        // Setup panel
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform setupPanel = null;

        foreach (GameObject go in allObjects)
        {
            if (go.name == setupPanelName)
            {
                setupPanel = go.transform;
                break;
            }
        }

        if (setupPanel == null)
        {
            Debug.LogError($"[StartHubManager] GameObject named '{setupPanelName}' not found! " +
                           $"Check the name in the Inspector.");
            playerSlots = new PlayerSlotUI[0];
            return;
        }

        // Player slots (direct children of the setup panel)
        int childCount = setupPanel.childCount;
        playerSlots = new PlayerSlotUI[childCount];

        Debug.Log($"[StartHubManager] Found '{setupPanelName}' with {childCount} child slot(s).");

        for (int i = 0; i < childCount; i++)
        {
            Transform slotRoot = setupPanel.GetChild(i);
            PlayerSlotUI slot = new PlayerSlotUI();

            // Name input
            TMP_InputField input = FindDeepComponent<TMP_InputField>(slotRoot, nameInputName);
            if (input != null)
                slot.nameInput = input;
            else
                Debug.LogWarning($"[StartHubManager] '{nameInputName}' (TMP_InputField) not found in slot {i} ('{slotRoot.name}')");

            // Character buttons — find the row, then each named button inside it
            Transform rowT = FindDeepTransform(slotRoot, charButtonRowName);
            slot.characterButtons = new Button[charButtonNames.Length];

            if (rowT != null)
            {
                for (int c = 0; c < charButtonNames.Length; c++)
                {
                    Transform btnT = rowT.Find(charButtonNames[c]);
                    if (btnT != null)
                        slot.characterButtons[c] = btnT.GetComponent<Button>();
                    else
                        Debug.LogWarning($"[StartHubManager] Button '{charButtonNames[c]}' not found in slot {i}");
                }
            }
            else
            {
                Debug.LogWarning($"[StartHubManager] '{charButtonRowName}' not found in slot {i} ('{slotRoot.name}')");
            }

            // Ready indicator
            Image readyImg = FindDeepComponent<Image>(slotRoot, readyImageName);
            if (readyImg != null)
                slot.readyIndicator = readyImg;
            else
                Debug.LogWarning($"[StartHubManager] '{readyImageName}' not found in slot {i} ('{slotRoot.name}')");

            playerSlots[i] = slot;
            Debug.Log($"[StartHubManager] Slot {i} built from '{slotRoot.name}'");
        }
    }

    /// <summary>
    /// Recursively searches <paramref name="root"/> (including inactive objects)
    /// for the first Transform whose name matches <paramref name="targetName"/>
    /// </summary>
    private Transform FindDeepTransform(Transform root, string targetName)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (t.name == targetName) return t;
        }
        return null;
    }

    /// <summary>
    /// Recursively searches <paramref name="root"/> (including inactive objects)
    /// for the first component of type <typeparamref name="T"/> on a GameObject
    /// whose name matches <paramref name="targetName"/>
    /// </summary>
    private T FindDeepComponent<T>(Transform root, string targetName) where T : Component
    {
        foreach (T comp in root.GetComponentsInChildren<T>(includeInactive: true))
        {
            if (comp.gameObject.name == targetName) return comp;
        }
        return null;
    }

    #endregion

    #region Slot Initialization

    /// <summary>
    /// Wires up name-change and character-button listeners for every slot,
    /// and hides all ready indicators to start in the default unready state
    /// </summary>
    private void InitializeSlots()
    {
        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot == null) continue;

            int capturedSlot = s; // Capture for lambda closures

            if (slot.nameInput != null)
                slot.nameInput.onValueChanged.AddListener(_ => OnNameChanged(capturedSlot));

            if (slot.characterButtons != null)
            {
                for (int c = 0; c < slot.characterButtons.Length; c++)
                {
                    Button btn = slot.characterButtons[c];
                    if (btn == null) continue;

                    int capturedChar = c; // Capture for lambda closures
                    btn.onClick.AddListener(() => OnCharacterSelected(capturedSlot, capturedChar));
                }
            }

            SetReadyIndicator(slot, false);
        }
    }

    #endregion

    #region Event Handlers

    private void OnNameChanged(int slotIndex)
    {
        EvaluateSlotReady(slotIndex);
        RefreshStartButton();
    }

    /// <summary>
    /// Handles a character button click
    /// Clicking the already-selected character deselects it (toggle behaviour)
    /// </summary>
    private void OnCharacterSelected(int slotIndex, int charIndex)
    {
        PlayerSlotUI slot = playerSlots[slotIndex];

        // Toggle: re-clicking the active character deselects it
        slot.selectedCharacterIndex = (slot.selectedCharacterIndex == charIndex) ? -1 : charIndex;

        EvaluateSlotReady(slotIndex);
        RefreshAllCharacterButtons();
        RefreshStartButton();
    }

    #endregion

    #region Ready State

    /// <summary>
    /// Recalculates the ready state for a single slot
    /// A slot is ready when it has both a non-empty input field and a selected character
    /// </summary>
    private void EvaluateSlotReady(int slotIndex)
    {
        PlayerSlotUI slot = playerSlots[slotIndex];
        string name = slot.nameInput != null ? slot.nameInput.text.Trim() : "";

        slot.isReady = !string.IsNullOrEmpty(name) && slot.selectedCharacterIndex >= 0;
        SetReadyIndicator(slot, slot.isReady);
    }

    private void SetReadyIndicator(PlayerSlotUI slot, bool ready)
    {
        if (slot.readyIndicator != null)
            slot.readyIndicator.gameObject.SetActive(ready);
    }

    #endregion

    #region Character Button Refresh

    /// <summary>
    /// Updates the interactability and visual alpha of every character button
    /// across all slots to reflect the current selection state
    ///
    /// Rules:
    ///   - A character chosen by another slot is greyed out and non-interactable
    ///   - The active character in the owning slot shows full opacity
    ///   - Unchosen characters in a slot that has already made a selection are faded
    ///   - Characters in a slot with no selection yet show full opacity
    /// </summary>
    private void RefreshAllCharacterButtons()
    {
        // Build a lookup: which slot owns each character index? (-1 = unclaimed)
        int[] takenBySlot = new int[charButtonNames.Length];
        for (int i = 0; i < takenBySlot.Length; i++) takenBySlot[i] = -1;

        for (int s = 0; s < playerSlots.Length; s++)
        {
            int ci = playerSlots[s].selectedCharacterIndex;
            if (ci >= 0 && ci < takenBySlot.Length)
                takenBySlot[ci] = s;
        }

        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot.characterButtons == null) continue;

            for (int c = 0; c < slot.characterButtons.Length; c++)
            {
                Button btn = slot.characterButtons[c];
                if (btn == null) continue;

                bool selectedByMe = slot.selectedCharacterIndex == c;
                bool takenByOther = takenBySlot[c] >= 0 && takenBySlot[c] != s;

                btn.interactable = !takenByOther;

                // Alpha logic:
                //   Selected by this slot       - full opacity
                //   Not selected, slot has pick - faded (another character is active)
                //   Taken by another slot       - faded
                //   Slot has no selection yet   - full opacity
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    Color col = img.color;
                    col.r = 1f; col.g = 1f; col.b = 1f; // Always reset any tint

                    bool slotHasSelection = slot.selectedCharacterIndex >= 0;
                    bool fadedInThisSlot = slotHasSelection && !selectedByMe;

                    col.a = (takenByOther || fadedInThisSlot) ? 0.35f : 1f;
                    img.color = col;
                }
            }
        }
    }

    #endregion

    #region Start Button

    /// <summary>
    /// Enables the Start button only when:
    ///   - At least one slot is fully ready, AND
    ///   - No slot is partially filled (has a name OR a character, but not both)
    /// Partial slots block the start to prevent unclear game states
    /// </summary>
    private void RefreshStartButton()
    {
        if (startButton == null) return;

        int readyCount = 0;
        bool anyPartial = false;

        foreach (PlayerSlotUI slot in playerSlots)
        {
            if (slot == null) continue;

            string name = slot.nameInput != null ? slot.nameInput.text.Trim() : "";
            bool hasName = !string.IsNullOrEmpty(name);
            bool hasChar = slot.selectedCharacterIndex >= 0;

            if (slot.isReady)
                readyCount++;
            else if (hasName || hasChar)
                anyPartial = true;
        }

        startButton.interactable = readyCount >= 1 && !anyPartial;
    }

    /// <summary>
    /// Called by the Start Game Button's OnClick() event in the Inspector
    /// Saves all ready slot selections to PlayerData, then loads the MainScene
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("[StartHubManager] PlayerData.Instance is null!");
            return;
        }

        PlayerData.Instance.ClearSelection();

        int savedCount = 0;
        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot == null || !slot.isReady) continue;

            string playerName = slot.nameInput.text.Trim();
            int charIndex = slot.selectedCharacterIndex;

            // Read the sprite directly from the chosen button's Image component
            Sprite charSprite = null;
            Button chosenBtn = slot.characterButtons[charIndex];
            if (chosenBtn != null)
            {
                Image btnImage = chosenBtn.GetComponent<Image>();
                if (btnImage != null)
                    charSprite = btnImage.sprite;
            }

            if (charSprite == null)
                Debug.LogWarning($"[StartHubManager] Slot {s}: no sprite found on button {charIndex}!");

            PlayerData.Instance.SetPlayerSelection(savedCount, playerName, charIndex, charSprite);
            Debug.Log($"[StartHubManager] Slot {s} → Player {savedCount + 1}: {playerName}, " +
                      $"sprite: {charSprite?.name}");
            savedCount++;
        }

        Debug.Log($"[StartHubManager] {savedCount} player(s) saved. Loading '{mainSceneName}'...");
        SceneManager.LoadScene(mainSceneName);
    }

    #endregion
}