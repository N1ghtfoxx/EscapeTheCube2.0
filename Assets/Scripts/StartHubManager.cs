using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Runtime data for one player slot - populated automatically from the hierarchy.
/// </summary>
public class PlayerSlotUI
{
    public TMP_InputField nameInput;
    public Button[] characterButtons; // 4 buttons, one per character
    public Image readyIndicator;

    // runtime state
    public int selectedCharacterIndex = -1; // -1 = nothing chosen yet
    public bool isReady = false;

}

/// <summary>
/// Controls the StartScreen scene.
/// Finds all UI references automatically - no Inspector drag-and-drop needed.
/// </summary>
public class StartHubManager : MonoBehaviour
{
    #region Inspector

    [Header("Hierarchy Names")]
    [Tooltip("Exact name of the GameObject that contains all 4 player slot children")]
    [SerializeField] private string setupPanelName = "PlayerSetupPanel";

    [Tooltip("Exact name of the Start Game Button")]
    [SerializeField] private string startButtonName = "StartGameButton";

    [Tooltip("Exact name of the TMP_InputField inside each slot")]
    [SerializeField] private string nameInputName = "PlayerNameInput";

    [Tooltip("Exact name of the row that holds the 4 character buttons")]
    [SerializeField] private string charButtonRowName = "CharButtonRow";

    [Tooltip("Names of the 4 character buttons inside CharButtonRow (in order)")]
    [SerializeField] private string[] charButtonNames = { "Yellow", "Green", "Red", "Purple" };

    [Tooltip("Exact name of the ready indicator Image inside each slot")]
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

    private void FindReferences()
    {
        // Start button 
        // Search all buttons in the scene (including inactive)
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
        // Find the panel that holds all slot children
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

        // Slots = direct children of setupPanel 
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

            // Character buttons – find the row first, then get children by name
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
    /// Searches recursively for the first Transform with the given name under root (includes inactive).
    /// </summary>
    private Transform FindDeepTransform(Transform root, string targetName)
    {
        // GetComponentsInChildren includes inactive GameObjects
        Transform[] all = root.GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform t in all)
        {
            if (t.name == targetName) return t;
        }
        return null;
    }

    private T FindDeepComponent<T>(Transform root, string targetName) where T : Component
    {
        T[] all = root.GetComponentsInChildren<T>(includeInactive: true);
        foreach (T comp in all)
        {
            if (comp.gameObject.name == targetName) return comp;
        }
        return null;
    }

    #endregion

    #region Slot Initialization

    private void InitializeSlots()
    {
        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot == null) continue;

            int capturedSlot = s;

            if (slot.nameInput != null)
                slot.nameInput.onValueChanged.AddListener(_ => OnNameChanged(capturedSlot));

            if (slot.characterButtons != null)
            {
                for (int c = 0; c < slot.characterButtons.Length; c++)
                {
                    Button btn = slot.characterButtons[c];
                    if (btn == null) continue;

                    int capturedChar = c;
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

    private void OnCharacterSelected(int slotIndex, int charIndex)
    {
        PlayerSlotUI slot = playerSlots[slotIndex];

        // Toggle: click the same button again to deselect
        slot.selectedCharacterIndex = (slot.selectedCharacterIndex == charIndex) ? -1 : charIndex;

        EvaluateSlotReady(slotIndex);
        RefreshAllCharacterButtons();
        RefreshStartButton();
    }

    #endregion

    #region Ready State

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

    private void RefreshAllCharacterButtons()
    {
        // Which slot owns each character index? (-1 = free)
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
                // - selected by this slot   → full opacity (original look)
                // - not selected, same slot → faded (another char is chosen)
                // - taken by another slot   → faded
                // - nothing selected yet    → full opacity
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    Color col = img.color;
                    col.r = 1f; col.g = 1f; col.b = 1f; // always reset tint

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
    /// Called by StartGameButton -> OnClick() in the Inspector.
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

            // Read sprite directly from the chosen button's Image component
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
            Debug.Log($"[StartHubManager] Slot {s} - Player {savedCount + 1}: {playerName}, sprite: {charSprite?.name}");
            savedCount++;
        }

        Debug.Log($"[StartHubManager] {savedCount} player(s) saved. Loading '{mainSceneName}'...");
        SceneManager.LoadScene(mainSceneName);
    }

    #endregion
}