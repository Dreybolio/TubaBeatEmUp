using UnityEngine;
public abstract class ConsoleCommand : ScriptableObject, IConsoleCommand
{
    [SerializeField] private string _commandWord = string.Empty;
    public string CommandWord => _commandWord;

    [SerializeField] private string _argFormat = string.Empty;
    public string ArgFormat => _argFormat;


    public abstract bool Execute(string[] args);
}

public interface IConsoleCommand
{
    string CommandWord { get; }
    string ArgFormat { get; }
    bool Execute(string[] args);
}
