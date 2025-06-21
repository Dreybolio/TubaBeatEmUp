using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Hurt Command", menuName = "ScriptableObjects/Commands/HurtCommand")] 
public class CmdHurt : ConsoleCommand
{
    public override bool Execute(string[] args)
    {
        if (args == null || args.Length < 2)
        {
            DeveloperConsole.Log("Error: Arguments must contain a GameObject name and a integer.");
            return false;
        }

        string last = args[args.Length - 1];
        if (!int.TryParse(last, out int number))
        {
            DeveloperConsole.Log("Error: Last argument must be an integer");
            return false;
        }

        string[] withoutLast = args.Take(args.Length - 1).ToArray();

        string joined = string.Join(' ', withoutLast);
        GameObject g = GameObject.Find(joined);
        if (g == null)
        {
            DeveloperConsole.Log($"Error: Count not find GameObject {joined}");
            return false;
        }
        Character c;
        if (g.TryGetComponent(out c))
        {
            c.Damage(number);
            return true;
        }
        return false;

    }
}
