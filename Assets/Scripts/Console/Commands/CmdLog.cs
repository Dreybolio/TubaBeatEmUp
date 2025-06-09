using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Log Command", menuName = "ScriptableObjects/Commands/LogCommand")] 
public class CmdLog : ConsoleCommand
{
    public override bool Execute(string[] args)
    {
        string joined = string.Join(' ', args);

        DeveloperConsole.Log(joined);

        return true;
    }
}
