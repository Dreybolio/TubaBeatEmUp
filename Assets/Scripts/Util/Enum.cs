using UnityEngine;

public enum Scene
{
    PERSISTENT,
    MAIN_MENU,
    GAME
}

public enum AI_SquadRole
{
    OFFENSE,
    COVER_EXIT,
    COVER_EXIT_FAR,
    FLANK,
}

public enum AI_Action
{
    IDLE,
    MOVE,
    ATTACK,
    JUMP,
    ROLL,
}

public enum AI_Action_Override
{
    NONE,
    CHANGEROLE_OFFENSE
}

public enum ActionType
{
    NONE,
    ATTACK_LIGHT,
    ATTACK_HEAVY,
    SPECIAL,
    DASH,
    ATTACK_DRILL,
    ATTACK_SPIN,
}

public enum CharacterType
{
    BARBARIAN,
}
public static class Enum
{
    public static string SceneToString(Scene scene)
    {
        return scene switch
        {
            Scene.PERSISTENT => "Persistent",
            Scene.MAIN_MENU =>  "Main Menu",
            Scene.GAME =>       "Game",
            _ => throw new System.Exception("Error: Scene Enum does not have an equivalent string!"),
        };
    }
}
