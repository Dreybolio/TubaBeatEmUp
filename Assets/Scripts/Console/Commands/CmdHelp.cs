using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Help Command", menuName = "ScriptableObjects/Commands/HelpCommand")] 
public class CmdHelp : ConsoleCommand
{
    public override bool Execute(string[] args)
    {
        DeveloperConsole.Log("-- COMMANDS --");
        foreach (var command in DeveloperConsole.Instance.Commands)
        {
            DeveloperConsole.Log($"{command.CommandWord} {command.ArgFormat}");
        }

        return true;
    }
}
