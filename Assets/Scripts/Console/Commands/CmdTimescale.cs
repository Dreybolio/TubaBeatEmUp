using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Timescale Command", menuName = "ScriptableObjects/Commands/TimescaleCommand")] 
public class CmdTimescale : ConsoleCommand
{
    public override bool Execute(string[] args)
    {
        if (args.Length != 1) return false;

        float timescale;
        if (float.TryParse(args[0], out timescale))
        {
            Time.timeScale = timescale;
        }

        return true;
    }
}
