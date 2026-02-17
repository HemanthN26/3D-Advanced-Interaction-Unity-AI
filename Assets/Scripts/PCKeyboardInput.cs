using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Allows PC keyboard input for TMP_InputField when testing via Quest Link.
/// This is a development tool - on device, the native keyboard will be used instead.
/// 
/// Usage: Add this component to your TMP_InputField GameObject.
/// When the input field is selected, you can type using your PC keyboard.
/// </summary>
public class PCKeyboardInput : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_InputField inputField;
    
    [Header("Settings")]
    [Tooltip("Enable PC keyboard input (disable on device builds)")]
    [SerializeField] private bool enablePCInput = true;
    
    [Tooltip("Show visual indicator when input field is focused")]
    [SerializeField] private bool showFocusIndicator = true;
    
    private bool isFocused = false;
    private string lastInputString = "";

    void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    void Start()
    {
        if (inputField == null)
        {
            Debug.LogError("[PCKeyboardInput] No TMP_InputField found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Auto-disable on Android device builds (not Editor)
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (TouchScreenKeyboard.isSupported)
        {
            Debug.Log("[PCKeyboardInput] Running on device - native keyboard will be used. Disabling PC input.");
            enablePCInput = false;
        }
        #endif

        if (enablePCInput)
        {
            Debug.Log("[PCKeyboardInput] PC Keyboard input enabled for: " + gameObject.name);
            Debug.Log("[PCKeyboardInput] Click on the input field, then type with your keyboard!");
        }

        // Subscribe to input field events
        inputField.onSelect.AddListener(OnInputFieldSelect);
        inputField.onDeselect.AddListener(OnInputFieldDeselect);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetFocus(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetFocus(false);
    }

    private void OnInputFieldSelect(string text)
    {
        SetFocus(true);
    }

    private void OnInputFieldDeselect(string text)
    {
        SetFocus(false);
    }

    private void SetFocus(bool focused)
    {
        if (isFocused == focused) return;
        
        isFocused = focused;
        
        if (focused)
        {
            Debug.Log("[PCKeyboardInput] Input field FOCUSED - Type with your PC keyboard!");
            // Keep the input field active for caret display
            inputField.ActivateInputField();
        }
        else
        {
            Debug.Log("[PCKeyboardInput] Input field lost focus");
        }
    }

    void Update()
    {
        if (!enablePCInput || !isFocused || inputField == null) return;

        // Keep input field active
        if (!inputField.isFocused)
        {
            inputField.ActivateInputField();
        }

        // Process keyboard input
        ProcessKeyboardInput();
    }

    private void ProcessKeyboardInput()
    {
        // Get all input characters this frame
        string inputString = Input.inputString;
        
        if (!string.IsNullOrEmpty(inputString))
        {
            foreach (char c in inputString)
            {
                ProcessCharacter(c);
            }
        }

        // Handle special keys
        HandleSpecialKeys();
    }

    private void ProcessCharacter(char c)
    {
        // Backspace
        if (c == '\b')
        {
            if (inputField.text.Length > 0)
            {
                // Handle selection
                if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
                {
                    // Delete selected text
                    int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    inputField.text = inputField.text.Remove(start, end - start);
                    inputField.caretPosition = start;
                }
                else if (inputField.caretPosition > 0)
                {
                    // Delete character before caret
                    int pos = inputField.caretPosition;
                    inputField.text = inputField.text.Remove(pos - 1, 1);
                    inputField.caretPosition = pos - 1;
                }
            }
        }
        // Enter/Return
        else if (c == '\n' || c == '\r')
        {
            if (inputField.multiLine)
            {
                InsertTextAtCaret("\n");
            }
            else
            {
                // Submit on single-line input
                Debug.Log("[PCKeyboardInput] Enter pressed - submitting");
                inputField.onEndEdit?.Invoke(inputField.text);
                inputField.onSubmit?.Invoke(inputField.text);
            }
        }
        // Tab - skip
        else if (c == '\t')
        {
            // Could implement tab to next field here
        }
        // Regular character
        else
        {
            InsertTextAtCaret(c.ToString());
        }
    }

    private void InsertTextAtCaret(string text)
    {
        // Handle selection replacement
        if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
        {
            int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            inputField.text = inputField.text.Remove(start, end - start);
            inputField.caretPosition = start;
        }

        // Insert text at caret position
        int caretPos = inputField.caretPosition;
        inputField.text = inputField.text.Insert(caretPos, text);
        inputField.caretPosition = caretPos + text.Length;
    }

    private void HandleSpecialKeys()
    {
        // Delete key
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
            {
                // Delete selected text
                int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                inputField.text = inputField.text.Remove(start, end - start);
                inputField.caretPosition = start;
            }
            else if (inputField.caretPosition < inputField.text.Length)
            {
                // Delete character after caret
                inputField.text = inputField.text.Remove(inputField.caretPosition, 1);
            }
        }

        // Select All (Ctrl+A)
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                inputField.selectionAnchorPosition = 0;
                inputField.selectionFocusPosition = inputField.text.Length;
            }
            // Copy (Ctrl+C)
            else if (Input.GetKeyDown(KeyCode.C))
            {
                if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
                {
                    int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    GUIUtility.systemCopyBuffer = inputField.text.Substring(start, end - start);
                    Debug.Log("[PCKeyboardInput] Copied to clipboard");
                }
            }
            // Paste (Ctrl+V)
            else if (Input.GetKeyDown(KeyCode.V))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboard))
                {
                    InsertTextAtCaret(clipboard);
                    Debug.Log("[PCKeyboardInput] Pasted from clipboard");
                }
            }
            // Cut (Ctrl+X)
            else if (Input.GetKeyDown(KeyCode.X))
            {
                if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
                {
                    int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    GUIUtility.systemCopyBuffer = inputField.text.Substring(start, end - start);
                    inputField.text = inputField.text.Remove(start, end - start);
                    inputField.caretPosition = start;
                    Debug.Log("[PCKeyboardInput] Cut to clipboard");
                }
            }
        }

        // Arrow keys for caret movement
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (inputField.caretPosition > 0)
            {
                inputField.caretPosition--;
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (inputField.caretPosition < inputField.text.Length)
            {
                inputField.caretPosition++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Home))
        {
            inputField.caretPosition = 0;
        }
        else if (Input.GetKeyDown(KeyCode.End))
        {
            inputField.caretPosition = inputField.text.Length;
        }

        // Escape to deselect
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            inputField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);
            SetFocus(false);
        }
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(OnInputFieldSelect);
            inputField.onDeselect.RemoveListener(OnInputFieldDeselect);
        }
    }
}

