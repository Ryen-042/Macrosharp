namespace Macrosharp.Devices.Keyboard.HotkeyBindings;

// Manages the mapping and execution of hotkey actions.
public class HotkeyActionService
{
    // Stores the mapping from action names to their corresponding delegates.
    private readonly Dictionary<string, Action<IReadOnlyDictionary<string, string>>> _actionMap = new();

    /// <summary>
    /// Registers an action with a given name and a delegate to execute.
    /// </summary>
    /// <param name="actionName">The unique name of the action.</param>
    /// <param name="actionDelegate">The delegate to execute when the action is triggered.
    /// It takes a dictionary of string arguments.</param>
    public void RegisterAction(string actionName, Action<IReadOnlyDictionary<string, string>> actionDelegate)
    {
        if (_actionMap.ContainsKey(actionName))
        {
            Console.WriteLine($"Warning: Action '{actionName}' is already registered and will be overwritten.");
        }
        _actionMap[actionName] = actionDelegate;
    }

    /// <summary>
    /// Executes a registered action by its name, passing the provided arguments.
    /// </summary>
    /// <param name="actionName">The name of the action to execute.</param>
    /// <param name="arguments">A dictionary of arguments for the action.</param>
    public void ExecuteAction(string actionName, IReadOnlyDictionary<string, string> arguments)
    {
        if (_actionMap.TryGetValue(actionName, out var actionDelegate))
        {
            actionDelegate.Invoke(arguments);
        }
        else
        {
            Console.WriteLine($"Error: Action '{actionName}' not found in the action service.");
        }
    }

    /// <summary>
    /// Helper method to parse an integer argument from the arguments dictionary.
    /// </summary>
    /// <param name="args">The arguments dictionary.</param>
    /// <param name="key">The key of the argument to parse.</param>
    /// <param name="defaultValue">The default value to return if parsing fails or key is not found.</param>
    /// <returns>The parsed integer value or the default value.</returns>
    public static int ParseIntArgument(IReadOnlyDictionary<string, string> args, string key, int defaultValue = 0)
    {
        if (args.TryGetValue(key, out string? valueString) && int.TryParse(valueString, out int result))
        {
            return result;
        }
        return defaultValue;
    }
}
