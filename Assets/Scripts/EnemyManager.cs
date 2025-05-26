using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    public List<Character> Enemies { get; private set; } = new();
    public Dictionary<int, List<Character>> Squads { get; private set; } = new();
    public void AddToSquad(Character c, int squadID)
    {
        if (!Squads.ContainsKey(squadID)) {
            Squads.Add(squadID, new List<Character>());
        }
        if (!Squads[squadID].Contains(c))
        {
            // Default to offense. This will get fixed later.
            Squads[squadID].Add(c);
        }
        BalanceSquad(squadID);
    }
    public void RemoveFromSquad(Character c, int squadID)
    {
        if (!Squads.ContainsKey(squadID))
        {
            Debug.LogError("Tried to remove a character from an invalid Squad ID");
            return;
        }
        if (Squads[squadID].Contains(c))
        {
            Squads[squadID].Remove(c);
        }
        BalanceSquad(squadID);
    }
    /*
     * Balances a squad such that there are only one or two attackers, and other roles are fulfilled.
     */
    public void BalanceSquad(int squadID)
    {
        var squad = Squads[squadID];
        if (squad == null)
            return;
        if (squad.Count == 0)
            return;

        int squadSize = squad.Count;

        // Determine the number of Offense roles based on squad size
        int offenseCount = squadSize < 6 ? 1 : 2;
        var offenseCandidates = squad.Where(c => c.SquadRole == AI_SquadRole.OFFENSE).ToList();
        if (offenseCandidates.Count > offenseCount)
        {
            // To many in offense. Sort by closest and cull.
            offenseCandidates = offenseCandidates
                .Where(c => c.Target != null)
                .OrderBy(c => Vector3.Distance(c.transform.position, c.Target.position))
                .Take(offenseCount).ToList();
        }
        else if (offenseCandidates.Count < offenseCount)
        {
            // Not enough in offense. Add the closest non-offense candidates
            var additionalOffense = squad.Except(offenseCandidates)
                .Where(c => c.Target != null)
                .OrderBy(c => Vector3.Distance(c.transform.position, c.Target.position))
                .Take(offenseCount - offenseCandidates.Count);
            offenseCandidates.AddRange(additionalOffense);
        }
        // Assign Offense
        foreach (var character in offenseCandidates)
        {
            character.SquadRole = AI_SquadRole.OFFENSE;
        }

        // Start with at least three coverExitCandidates.
        var coverExitCandidates = squad.Except(offenseCandidates).Take(3).ToList();
        foreach (var character in coverExitCandidates)
        {
            character.SquadRole = AI_SquadRole.COVER_EXIT;
        }

        // Get the rest of the candidates
        var remainingCandidates = squad.Except(offenseCandidates).Except(coverExitCandidates).ToList();

        // 2/3 will cover far exits, 1/3 will cover close exits
        int coverExitFarCount = (int)(remainingCandidates.Count * 2f / 3f);
        int coverExitCount = remainingCandidates.Count - coverExitFarCount;

        foreach (var character in remainingCandidates.Take(coverExitFarCount))
        {
            character.SquadRole = AI_SquadRole.COVER_EXIT_FAR;
        }

        foreach (var character in remainingCandidates.Skip(coverExitFarCount))
        {
            character.SquadRole = AI_SquadRole.COVER_EXIT;
        }

        PrintSquad(squadID);
    }

    private void PrintSquad(int squadID)
    {
        var squad = Squads[squadID];
        if (squad == null)
            return;
        if (squad.Count == 0)
        {
            Debug.Log("SquadManager: Squad is Empty.");
            return;
        }
        string output = "SquadManager:\n";
        int i = 0;
        foreach (var character in squad)
        {
            output += "\t" + i + ") " + character.name + " assigned role " + character.SquadRole + "\n";
            i++;
        }
        Debug.Log(output);
    }
}
