using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Kill Command", menuName = "ScriptableObjects/Commands/KillCommand")] 
public class CmdKill : ConsoleCommand
{
    public override bool Execute(string[] args)
    {
        string joined = string.Join(' ', args);
        GameObject g = GameObject.Find(joined);
        if (g == null)
        {
            DeveloperConsole.Log($"Error: Count not find GameObject {joined}");
            return false;
        }
        Character c;
        if (g.TryGetComponent(out c))
        {
            c.Damage(9999);
            return true;
        }
        return false;

    }
}
