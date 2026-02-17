using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Forces the system keyboard to open when a TMP_InputField is selected.
/// NOTE: TouchScreenKeyboard only works on actual Quest device, NOT via Quest Link!
/// </summary>
public class ForceKeyboardOpen : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    [SerializeField] private TMP_InputField inputField;
    
    private TouchScreenKeyboard keyboard;
    private bool keyboardWasOpen = false;
    private bool keyboardSupported = true;

    void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    void Start()
    {
        // Check if TouchScreenKeyboard is supported
        keyboardSupported = TouchScreenKeyboard.isSupported;
        
        if (!keyboardSupported)
        {
            Debug.LogWarning("[ForceKeyboardOpen] TouchScreenKeyboard is NOT supported in this environment!");
            Debug.LogWarning("[ForceKeyboardOpen] Keyboard will only work when built and running directly on Quest device.");
            Debug.LogWarning("[ForceKeyboardOpen] Quest Link does NOT support native keyboards.");
        }
        else
        {
            Debug.Log("[ForceKeyboardOpen] TouchScreenKeyboard is supported!");
        }

        if (inputField != null)
        {
            inputField.onSelect.AddListener(OnInputFieldSelect);
            Debug.Log("[ForceKeyboardOpen] Initialized - waiting for input field selection");
        }
        else
        {
            Debug.LogError("[ForceKeyboardOpen] No TMP_InputField found!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[ForceKeyboardOpen] Click detected on input field");
        OpenKeyboard();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("[ForceKeyboardOpen] Input field selected");
        OpenKeyboard();
    }

    private void OnInputFieldSelect(string text)
    {
        Debug.Log("[ForceKeyboardOpen] InputField onSelect fired");
        OpenKeyboard();
    }

    private void OpenKeyboard()
    {
        if (inputField == null) return;

        // Activate the input field for caret/cursor
        inputField.ActivateInputField();
        
        if (!keyboardSupported)
        {
            Debug.LogWarning("[ForceKeyboardOpen] Cannot open keyboard - not supported via Quest Link!");
            Debug.LogWarning("[ForceKeyboardOpen] Build and run directly on Quest device to use keyboard.");
            return;
        }

        // Only try to open if we don't have an active keyboard
        if (keyboard != null)
        {
            return; // Already have a keyboard reference
        }

        Debug.Log("[ForceKeyboardOpen] Attempting to open TouchScreenKeyboard...");
        
        try
        {
            keyboard = TouchScreenKeyboard.Open(
                inputField.text,
                TouchScreenKeyboardType.Default,
                true,
                inputField.multiLine,
                false,
                false,
                "",
                0
            );
            
            if (keyboard != null)
            {
                Debug.Log("[ForceKeyboardOpen] Keyboard opened!");
                keyboardWasOpen = true;
            }
            else
            {
                Debug.LogWarning("[ForceKeyboardOpen] TouchScreenKeyboard.Open returned null");
                Debug.LogWarning("[ForceKeyboardOpen] This usually means you're running via Quest Link.");
                Debug.LogWarning("[ForceKeyboardOpen] Build to device to test keyboard functionality.");
                keyboardSupported = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceKeyboardOpen] Error opening keyboard: {e.Message}");
            keyboardSupported = false;
        }
    }

    void Update()
    {
        if (keyboard == null || !keyboardSupported) return;

        try
        {
            var status = keyboard.status;
            
            if (status == TouchScreenKeyboard.Status.Visible)
            {
                if (inputField.text != keyboard.text)
                {
                    inputField.text = keyboard.text;
                    inputField.caretPosition = inputField.text.Length;
                }
            }
            else if (keyboardWasOpen)
            {
                if (status == TouchScreenKeyboard.Status.Done)
                {
                    Debug.Log("[ForceKeyboardOpen] Keyboard closed (Done)");
                    inputField.text = keyboard.text;
                }
                else if (status == TouchScreenKeyboard.Status.Canceled)
                {
                    Debug.Log("[ForceKeyboardOpen] Keyboard closed (Canceled)");
                }
                keyboardWasOpen = false;
                keyboard = null;
            }
        }
        catch
        {
            // Keyboard object became invalid
            keyboard = null;
            keyboardWasOpen = false;
        }
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(OnInputFieldSelect);
        }
    }
}

