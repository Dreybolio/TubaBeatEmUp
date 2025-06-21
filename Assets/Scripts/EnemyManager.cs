using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

#pragma warning disable CS0162 // Unreachable code detected
public class EnemyManager : Singleton<EnemyManager>
{
    public List<EnemyController> Enemies { get; private set; } = new();
    public Dictionary<int, List<EnemyController>> Squads { get; private set; } = new();

    // Override Sets: When an override is in place, this is forced to be in the final squad set.
    // Typically forced for 5 secs then allowed to be auto-placed again
    private Dictionary<EnemyController, Transform> targetOverrides = new();
    private Dictionary<EnemyController, AI_SquadRole> roleOverrides = new();
    const float TIME_RESET_OVERRIDE = 5.0f;
    const bool PRINT = false;

    public void AddToSquad(EnemyController c, int squadID)
    {
        if (!Squads.ContainsKey(squadID)) {
            Squads.Add(squadID, new List<EnemyController>());
        }
        if (!Squads[squadID].Contains(c))
        {
            // Default to offense. This will get fixed later.
            Squads[squadID].Add(c);
        }
        BalanceSquad(squadID);
    }
    public void RemoveFromSquad(EnemyController c, int squadID)
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

        var targets = PlayerManager.Instance.Players
            .Where(p => p.Controller.Health > 0)
            .Select(p => p.Controller)
            .ToList();

        if (targets.Count == 0)
            return;

        // Before assigning roles, try to balance player targets amongst this squad.
        // Assume that unless a target is overrepresented in this squad, we don't need to rearrange
        bool hasNull = squad
            .Where(x => x.Target == null || !targets.Contains(x.Target))
            .Any();
        var grouped = squad
            .GroupBy(x => x.Target)
            .Select(g => g.Count());
        int mostCommonTarget = grouped.Max();
        int leastCommonTarget = grouped.Min();

        if (hasNull || (Mathf.Abs(mostCommonTarget - leastCommonTarget) > 1))
        {
            // Make a dictionary with each character assigned to their current targets
            var targetGroups = squad
                .Where(c => c.Target != null && targets.Contains(c.Target) && !targetOverrides.ContainsKey(c))    // <-- Don't allow overrides into this initial list
                .GroupBy(c => c.Target)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var player in targets)
            {
                // Players who are not already being targeted need their entries manually created
                if (!targetGroups.ContainsKey(player))
                {
                    targetGroups[player] = new();
                }
            }

            int maxPerTarget = squad.Count / targets.Count;
            maxPerTarget += (squad.Count % targets.Count) > 0 ? 1 : 0; // If there are any extra assignments to be made, raise cap by one

            var unassigned = squad
                .Where(c => c.Target == null || !targetGroups.ContainsKey(c.Target))
                .ToList();

            // First, everyone may keep their min + 1 closest targets
            foreach (var target in targets)
            {
                // Identify overrides that will occur
                var overrides = targetOverrides
                    .Where(g => g.Value.Equals(target))
                    .Select(g => g.Key);
                // Order by distance, closest to furthest
                var ordered = targetGroups[target]
                    .OrderBy(c => Vector3.Distance(c.transform.position, target.transform.position))
                    .Except(overrides);
                // This list contains all the overrides first, then the ordered list.
                var priorityList = overrides
                    .Concat(ordered)
                    .ToList();

                unassigned = unassigned
                    .Union(priorityList
                    .Skip(maxPerTarget))
                    .ToList();
                targetGroups[target].Clear();
                targetGroups[target].AddRange(priorityList.Take(maxPerTarget));
            }

            // Everyone in unassigned should now be assigned to their closest target, given they don't go over-limit
            foreach (var enemy in unassigned)
            {
                var validTargets = targetGroups
                    .Where(g => g.Value.Count < maxPerTarget)
                    .Select(g => g.Key)
                    .ToList();
                var closest = validTargets
                    .OrderBy(t => Vector3.Distance(t.transform.position, enemy.transform.position))
                    .FirstOrDefault();
                if (closest != null)
                {
                    enemy.Target = closest;
                    targetGroups[closest].Add(enemy);
                }
                else
                {
                    Debug.LogError($"No valid targets remained in enemy assignment when trying to assign {enemy.name}");
                }
            }
        }

        // Determine the number of Offense roles based on squad size
        int offenseCount = (squad.Count / targets.Count) < 6 ? 1 : 2;
        foreach (var target in targets)
        {
            // Get all the AI targeting me. Assign them roles accordingly.
            var subSquad = squad
                .Where(c => c.Target == target)
                .ToList();
            var overrideCandidates = subSquad
                .Where(c => roleOverrides.ContainsKey(c))
                .ToList();
            foreach (var character in overrideCandidates)
            {
                character.SquadRole = roleOverrides[character];
            }

            subSquad = subSquad
                .Except(overrideCandidates)
                .ToList();
            var offenseCandidates = subSquad
                .Where(c => c.SquadRole == AI_SquadRole.OFFENSE)
                .ToList();
            if (offenseCandidates.Count > offenseCount)
            {
                // To many in offense. Sort by closest and cull.
                offenseCandidates = offenseCandidates
                    .Where(c => c.Target != null)
                    .OrderBy(c => Vector3.Distance(c.transform.position, c.Target.transform.position))
                    .Take(offenseCount).ToList();
            }
            else if (offenseCandidates.Count < offenseCount)
            {
                // Not enough in offense. Add the closest non-offense candidates
                var additionalOffense = subSquad.Except(offenseCandidates)
                    .Where(c => c.Target != null)
                    .OrderBy(c => Vector3.Distance(c.transform.position, c.Target.transform.position))
                    .Take(offenseCount - offenseCandidates.Count);
                offenseCandidates.AddRange(additionalOffense);
            }
            // Assign Offense
            foreach (var character in offenseCandidates)
            {
                character.SquadRole = AI_SquadRole.OFFENSE;
            }

            // Start with at least three coverExitCandidates.
            subSquad = subSquad
                .Except(offenseCandidates)
                .ToList();
            var coverExitCandidates = subSquad
                .Take(3)
                .ToList();
            foreach (var character in coverExitCandidates)
            {
                character.SquadRole = AI_SquadRole.COVER_EXIT;
            }

            // Get the rest of the candidates
            subSquad = subSquad
                .Except(coverExitCandidates)
                .ToList();
            var remainingCandidates = subSquad.ToList();

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

        }
        if (PRINT) PrintSquad(squadID);
    }

    public void AddTargetOverride(EnemyController c, Transform target)
    {
        // Here, a character can request to change their target. Will requre the targets to be rebalanced afterwards.
        if (PRINT) Debug.Log($"Enemy {c.name} is overriding target to {target.name}");
        C_AddTargetOverride(c, target);
        BalanceSquad(c.SquadID);
    }

    public void AddRoleOverride(EnemyController c, AI_SquadRole role)
    {
        // Here, a character can request to change their target. Will requre the targets to be rebalanced afterwards.
        if (PRINT) Debug.Log($"Enemy {c.name} is overriding role to {role}");
        C_AddRoleOverride(c, role);
        BalanceSquad(c.SquadID);
    }

    private IEnumerator C_AddTargetOverride(EnemyController c, Transform target)
    {
        if (targetOverrides.ContainsKey(c))
        {
            targetOverrides.Remove(c);
        }
        targetOverrides.Add(c, target);
        yield return new WaitForSeconds(TIME_RESET_OVERRIDE);
        if (targetOverrides.ContainsKey(c))
        {
            targetOverrides.Remove(c);
        }
    }
    private IEnumerator C_AddRoleOverride(EnemyController c, AI_SquadRole role)
    {
        if (roleOverrides.ContainsKey(c))
        {
            roleOverrides.Remove(c);
        }
        roleOverrides.Add(c, role);
        yield return new WaitForSeconds(TIME_RESET_OVERRIDE);
        if (roleOverrides.ContainsKey(c))
        {
            roleOverrides.Remove(c);
        }
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
            output += "\t" + i + ") " + character.name + " assigned role " + character.SquadRole + ", targeting " + character.Target.name + "\n";
            i++;
        }
        Debug.Log(output);
    }
}

#pragma warning restore CS0162 // Unreachable code detected
