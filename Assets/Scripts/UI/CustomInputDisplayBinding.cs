using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlObject]
public partial class CustomInputDisplayBinding : CustomBinding
{
    public enum Device
    {
        Keyboard,
        Gamepad
    }

    [UxmlAttribute]
    public Device device;

    public CustomInputDisplayBinding() 
    {
        updateTrigger = BindingUpdateTrigger.OnSourceChanged;
    }
    protected override void OnDataSourceChanged(in DataSourceContextChanged context)
    {
        VisualElement element = context.targetElement;

        object data = element.dataSource;
        if (data == null)
            data = element.GetHierarchicalDataSourceContext().dataSource;

        if (data is InputAction inputAction)
        {
            int index = GetBindingIndexForDevice(device, inputAction);
            string value = index >= 0 ? inputAction.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontUseShortDisplayNames) : "";

            Debug.Log($"[{device}] {inputAction.name} -> index {index}, '{value}'");

            ConverterGroups.TrySetValueGlobal(ref element, context.bindingId, value, out var errorCode);
        }
    }

    public static int GetBindingIndexForDevice(Device device, InputAction action)
    {
        string group = device == Device.Keyboard ? "Keyboard" : "Gamepad";
        var bindings = action.bindings;

        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding b = bindings[i];
            if (b.isPartOfComposite) continue;

            if (b.isComposite)
            {
                // El composite no tiene grupo propio: miramos su primera parte
                if (i + 1 < bindings.Count && MatchesDevice(bindings[i + 1], group))
                    return i;
                continue;
            }

            if (MatchesDevice(b, group))
                return i;
        }
        return -1;
    }

    private static bool MatchesDevice(InputBinding b, string group)
    {
        // Por grupo, o por path si no hay grupos definidos
        return (!string.IsNullOrEmpty(b.groups) && b.groups.Contains(group))
            || (!string.IsNullOrEmpty(b.path) && b.path.Contains($"<{group}>"));
    }
}
