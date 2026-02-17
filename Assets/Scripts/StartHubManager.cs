using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;

/// <summary>
/// Holds all UI references for a single player slot.
/// Fill these in the Inspector for each of the 4 slots.
/// </summary>
[System.Serializable]
public class PlayerSlotUI
{
    [Tooltip("Name input field for this player")]
    public TMP_InputField nameInput;

    [Tooltip("4 character buttons - must match characterDatas order")]
    public Button[] characterButtons;

    [Tooltip("Ready indicator image (shown when slot is ready)")]
    public Image readyIndicator;

    // -- runtime state ------------------------------------------------------
    [HideInInspector] public int selectedCharacterIndex = -1;
    [HideInInspector] public bool isReady = false;
}

/// <summary>
/// Controls the StartScreen scene.
/// Manages player slot setup: name input, character selection, ready state.
/// Saves selections into PlayerData and loads the MainScene on start.
/// </summary>
public class StartHubManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Character Data (assign all4, same order as buttons")]
    [SerializeField] private CharacterData[] characterDatas;

    [Header("Player Slots")]
    [SerializeField] private PlayerSlotUI[] playerSlots;

    [Header("Start Button")]
    [SerializeField] private Button startButton;

    [Header("Scene to load")]
    [SerializeField] private string mainSceneName = "MainScene";

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateSetup();
        InitializeSlots();
        RefreshStartButton();
    }

    #endregion

    #region Setup & Validation

    /// <summary>
    /// Logs warnings for common Inspector mistakes
    /// </summary>
    private void ValidateSetup()
    {
        if (characterDatas == null || characterDatas.Length != 4)
            Debug.LogWarning("[StartHubManager] characterDatas should have exactly 4 entries.");

        if (playerSlots == null || playerSlots.Length != 4)
            Debug.LogWarning("[StartHubManager] playerSlots should have exactly 4 entries.");

        if (startButton == null)
            Debug.LogError("[StartHubManager] startButton is not assigned!");
    }

    /// <summary>
    /// Wires up all listeners and sets initial visual states 
    /// </summary>
    private void InitializeSlots()
    {
        for (int slotIndex = 0; slotIndex <playerSlots.Length; slotIndex++)
        {
            PlayerSlotUI slot = playerSlots[slotIndex];
            if (slot == null) continue;

            // name input
            if (slot.nameInput != null)
            {
                // capture for closure
                int capturedSlot = slotIndex;
                slot.nameInput.onValueChanged.AddListener(_ => OnNameChanged(capturedSlot));
            }

            // character buttons
            if (slot.characterButtons != null)
            {
                for (int charIndex = 0; charIndex < slot.characterButtons.Length; charIndex++)
                {
                    Button btn = slot.characterButtons[charIndex];
                    if (btn == null) continue;

                    int capturedSlot = slotIndex;
                    int capturedChar = charIndex;
                    btn.onClick.AddListener(() => OnCharacterSelected(capturedSlot, capturedChar));
                }
            }

            // ready indicator
            SetReadyIndicator(slot, false);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called whenever a name field changes. Reevaluates ready state for that slot.
    /// </summary>
    private void OnNameChanged(int slotIndex)
    {
        EvaluateSlotReady(slotIndex);
        RefreshStartButton();
    }

    /// <summary>
    /// Called when a character button is clicked.
    /// Enforces unique character selection across all slots.
    /// </summary>
    private void OnCharacterSelected(int slotIndex, int charIndex)
    {
        PlayerSlotUI slot = playerSlots[slotIndex];

        // if already selected this character - deselect (toggle)
        if (slot.selectedCharacterIndex == charIndex)
        {
            slot.selectedCharacterIndex = -1;
        }
        else
        {
            slot.selectedCharacterIndex = charIndex;
        }

        // Re-evaluate ready for this slot
        EvaluateSlotReady(slotIndex);

        // refresh ALL buttons across ALL slots (global lock for taken characters)
        RefreshAllCharacterButtons();
        RefreshStartButton();
    }

    #endregion

    #region Ready State Logic

    /// <summary>
    /// A slot is "ready" when it has both a non-empty name AND a selected character
    /// </summary>
    private void EvaluateSlotReady(int slotIndex)
    {
        PlayerSlotUI slot = playerSlots[slotIndex];

        string name = slot.nameInput != null ? slot.nameInput.text.Trim() : "";
        bool hasName = !string.IsNullOrEmpty(name);
        bool hasCharacter = slot.selectedCharacterIndex >= 0;

        slot.isReady = hasName && hasCharacter;
        SetReadyIndicator(slot, slot.isReady);
    }

    /// <summary>
    /// Shows or hides the ready indicator image for a slot
    /// </summary>
    private void SetReadyIndicator(PlayerSlotUI slot, bool ready)
    {
        if (slot.readyIndicator != null)
            slot.readyIndicator.gameObject.SetActive(ready);
    }

    #endregion

    #region Character Button Refresh

    /// <summary>
    /// Refresh the visual state of every character button across all slots
    /// </summary>
    private void RefreshAllCharacterButtons()
    {
        // Build a set of wich characters are currently taken and by whom
        int[] takenBySlot = new int[characterDatas.Length];
        for (int i = 0; i < takenBySlot.Length; i++) takenBySlot[i] = -1;

        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot == null) continue;
            if (slot.selectedCharacterIndex >= 0 && slot.selectedCharacterIndex < takenBySlot.Length)
            {
                takenBySlot[slot.selectedCharacterIndex] = s;
            }
        }

        // now update every button
        for (int s = 0; s < playerSlots.Length; s++)
        {
            PlayerSlotUI slot = playerSlots[s];
            if (slot == null || slot.characterButtons == null) continue;

            for (int c = 0; c < slot.characterButtons.Length; c++)
            {
                Button btn = slot.characterButtons[c];
                if (btn == null) continue;

                bool isSelectedByThisSlot = (slot.selectedCharacterIndex == c);
                bool isTakenByOtherSlot = (takenBySlot[c] >= 0 && takenBySlot[c] != s);

                // Interactable: not taken by someone else
                btn.interactable = !isTakenByOtherSlot;

                // Visual highlight for selected state
                // button image will be tinted: full white = available/selected, gray = taken
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    if (isSelectedByThisSlot)
                    {
                        // selected: bright tint
                        btnImage.color = new Color(1f, 0.85f, 0.3f);
                    }
                    else if (isTakenByOtherSlot)
                    {
                        // aken by another player: dark gray
                        btnImage.color = new Color(0.4f, 0.4f, 0.4f);
                    }
                    else
                    {
                        // available: original colour
                        btnImage.color = Color.white;
                    }
                }
            }
        }
    }

    #endregion

    #region Start Button

    /// <summary>
    /// Start button is interactable only when at least 1 slot is ready.
    /// All non-empty slots must also be ready (no partial selections allowed).
    /// </summary>
    private void RefreshStartButton()
    {
        if (startButton == null) return;
    }

    #endregion

}
