using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.IO;
public class UIManager : MonoBehaviour
{
    public bool m_isOpen { get; private set; } = false;
    private UIDocument m_uiDocument;
    public bool isRebinding = false;
    private VisualElement m_root;
    private VisualElement m_rebindOverlay;
    private Label m_rebindMessage;
    [SerializeField] private VisualTreeAsset m_controlTemplate;
    [SerializeField] private InputActionAsset m_asset;
    public static UIManager Instance;
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOverrides();
    }

    private void OnEnable()
    {
        m_uiDocument = GetComponent<UIDocument>();
        m_root = m_uiDocument.rootVisualElement.Q<VisualElement>("Panel");
        m_rebindOverlay = m_root.Q<VisualElement>("RebindOverlay");
        m_rebindMessage = m_root.Q<Label>("RebindMessage");
    }

    private void ShowRebindMessage(string message)
    {
        isRebinding = true;
        m_rebindMessage.text = message;
        m_rebindOverlay.RemoveFromClassList("hidden");
    }

    private void HideRebindMessage()
    {
        isRebinding = false;
        m_rebindOverlay.AddToClassList("hidden");
    }

    public void OnMenuButton()
    {
        if (m_isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
        m_isOpen = !m_isOpen;
    }

    public void OpenMenu()
    {
        print("Opening menu");
        m_root.RemoveFromClassList("hidden");
        DisplayControls();
    }

    public void CloseMenu()
    {
        if (isRebinding)
        {
            return;
        }
        print("Closing menu");
        m_root.AddToClassList("hidden");
    }

    void DisplayControls()
    {
        ScrollView scrollView = m_root.Q<ScrollView>("ControlsArea");
        scrollView.Clear();

        InputActionMap actionMap = m_asset.actionMaps[0];
        foreach (InputAction action in actionMap.actions)
        {
            TemplateContainer entry = m_controlTemplate.Instantiate();
            entry.dataSource = action;
            scrollView.Add(entry);

            Button remapButton = entry.Q<Button>("RemapButton");
            remapButton.clicked += () =>
            {
                OnRemapButtonPressed(action);
            };
        }
    }

    private InputActionRebindingExtensions.RebindingOperation rebindingOperationKeyboard;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperationGamepad;
    private string m_overridesBackup;

    private void OnRemapButtonPressed(InputAction action)
    {
        action.Disable();
        CleanUpRebinds();
        ShowRebindMessage("Press any key or button to rebind (if you are changing the MOVE binding press the UP key you want to change)");
        m_overridesBackup = action.SaveBindingOverridesAsJson();

        int kbIndex = CustomInputDisplayBinding.GetBindingIndexForDevice(
            CustomInputDisplayBinding.Device.Keyboard, action);
        int gpIndex = CustomInputDisplayBinding.GetBindingIndexForDevice(
            CustomInputDisplayBinding.Device.Gamepad, action);

        if (kbIndex >= 0)
        {
            bool isComposite = action.bindings[kbIndex].isComposite;
            int firstPart = isComposite ? kbIndex + 1 : kbIndex;
            int lastPart = isComposite ? GetCompositeEnd(action, kbIndex) : kbIndex;

            rebindingOperationKeyboard = action.PerformInteractiveRebinding(firstPart)
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    rebindingOperationGamepad?.Dispose();
                    rebindingOperationGamepad = null;
                    op.Dispose();
                    rebindingOperationKeyboard = null;

                    RebindKeyboardPart(action, firstPart + 1, lastPart);
                })
                .OnCancel(op => CancelRebind(action))
                .Start();
        }

        if (gpIndex >= 0 && !action.bindings[gpIndex].isComposite)
        {
            rebindingOperationGamepad = action.PerformInteractiveRebinding(gpIndex)
                .WithControlsHavingToMatchPath("<Gamepad>")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    rebindingOperationKeyboard?.Dispose();
                    rebindingOperationKeyboard = null;
                    op.Dispose();
                    rebindingOperationGamepad = null;

                    FinishRebind(action);
                })
                .OnCancel(op => CancelRebind(action))
                .Start();
        }
    }

    private void RebindKeyboardPart(InputAction action, int partIndex, int lastIndex)
    {
        if (partIndex > lastIndex)
        {
            FinishRebind(action);
            return;
        }

        string partName = action.bindings[partIndex].name;   // up, down, left, right

        ShowRebindMessage($"Press a key for: {partName.ToUpper()}\n(Esc to cancel)");

        rebindingOperationKeyboard = action.PerformInteractiveRebinding(partIndex)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                op.Dispose();
                rebindingOperationKeyboard = null;
                RebindKeyboardPart(action, partIndex + 1, lastIndex);
            })
            .OnCancel(op => CancelRebind(action))
            .Start();
    }

    private static int GetCompositeEnd(InputAction action, int compositeIndex)
    {
        int i = compositeIndex + 1;
        while (i < action.bindings.Count && action.bindings[i].isPartOfComposite)
            i++;
        return i - 1;
    }

    private void FinishRebind(InputAction action)
    {
        HideRebindMessage();
        CleanUpRebinds();
        action.Enable();
        SaveBindings();
        DisplayControls();
    }

    private void CancelRebind(InputAction action)
    {
        HideRebindMessage();
        CleanUpRebinds();
        action.LoadBindingOverridesFromJson(m_overridesBackup);
        action.Enable();
        DisplayControls();
    }
    private void CleanUpRebinds()
    {
        rebindingOperationKeyboard?.Dispose();
        rebindingOperationGamepad?.Dispose();
        rebindingOperationKeyboard = null;
        rebindingOperationGamepad = null;
    }

    private void LoadOverrides()
    {
        string path = string.Concat(Application.persistentDataPath, "/Controls/controls.json");
        if (File.Exists(path))
        {
            string overrides = File.ReadAllText(path);
            m_asset.LoadBindingOverridesFromJson(overrides);
        }
    }

    private void SaveBindings()
    {
        string overrides = m_asset.SaveBindingOverridesAsJson();
        string path = string.Concat(Application.persistentDataPath, "/Controls");

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        File.WriteAllText(string.Concat(path, "/controls.json"), overrides);
    }

    
}
