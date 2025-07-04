using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class AIBehaviours : MonoBehaviour
{
    [SerializeField] private float _navCornerCutDistance = 0.5f;
    [SerializeField] private float _navTargetLeniencyThreshold = 0.15f;
    [SerializeField] private float _navAvoidEnemiesDistance = 0.35f;
    [NonSerialized] public EnemyController Enemy;

    // Constants
    private float ATTACK_CHANGEROLE_OFFENSE_CHANCE = 75.0f;
    private float CHANGE_TARGET_TIME = 3.0f;

    private float COVER_EXIT_MIN_DIST = 1.5f;
    private float COVER_EXIT_MAX_DIST = 3.5f;
    private float COVER_EXIT_FAR_MIN_DIST = 3.0f;
    private float COVER_EXIT_FAR_MAX_DIST = 4.5f;

    private float COVER_EXIT_MIN_TIMEOUT = 1.0f;
    private float COVER_EXIT_MAX_TIMEOUT = 3.0f;

    private float COVER_EXIT_TIMEOUT_OFFENSE_CHANCE = 50.0f;
    private float COVER_EXIT_PRESSURE_CHANCE = 50.0f;

    private float COVER_EXIT_TIMEOUT_OVERRIDE_TIME = 6.0f;

    public delegate void AIEvent();
    public AIEvent OnRoleChanged;

    // Vars
    private NavMeshPath _path;
    private Vector3 _targetPos;
    private Vector3 _targetPosAlt;
    private float _attackTimer = 0.5f;
    private float _pathTimer = 0.0f;
    private float _movePercent = 1.0f;

    private float _coverExitStartIdleTime;
    private float _coverExitIdleTimeout;
    private bool _coverExitIsIdling = false;

    private bool _favourAltPath = false;
    private bool _forceOffense = false;

    private float _changeTargetTimer;
    private float _cantMakePathStartTime = -1;
    private List<Transform> _playerTransforms;
    private Transform _closestPlayer;
    private AI_SquadRole _role;

    private void Start()
    {
        Enemy = GetComponent<EnemyController>();
        _playerTransforms = PlayerManager.Instance.Players.Select(p => p.Controller.transform).ToList();
        _path = new NavMeshPath();
        _changeTargetTimer = CHANGE_TARGET_TIME;
        StartCoroutine(C_CheckRoleChange());
    }

    private void OnEnable()
    {
        OnRoleChanged += ResetValues;
    }

    private void OnDisable()
    {
        OnRoleChanged -= ResetValues;
    }

    private void Update()
    {
        if (_pathTimer <= 0)
        {
            _pathTimer = 0.10f;
            RecalculatePath();
        }
        else
        {
            _pathTimer -= Time.deltaTime;
        }

        // See if we should try to change targets
        if (_changeTargetTimer < 0)
        {
            Enemy.RequestChangeTarget(_closestPlayer);
            _changeTargetTimer = CHANGE_TARGET_TIME;
        }
        else if (_closestPlayer != Enemy.Target)
        {
            _changeTargetTimer -= Time.deltaTime;
        }
        else
        {
            _changeTargetTimer = CHANGE_TARGET_TIME;
        }
    }

    private void FixedUpdate()
    {
        _closestPlayer = _playerTransforms.OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).FirstOrDefault();
    }

    private IEnumerator C_CheckRoleChange()
    {
        while (true)
        {
            AI_SquadRole prevRole = _role;
            yield return new WaitUntil(() => prevRole != _role);
            OnRoleChanged?.Invoke();
        }
    }

    private void ResetValues()
    {
        _movePercent = 1.0f;
    }

    public AI_Decision MakeDecision(AI_SquadRole role, int aiDifficulty)
    {
        if (Enemy.Target == null) return new AI_Decision { action = AI_Action.IDLE };

        _role = role;
        //**
        //**    Try for an attack
        //**
        float xDistFromTarget = Mathf.Abs(Enemy.Target.transform.position.x - transform.position.x);
        float zDistFromTarget = Mathf.Abs(Enemy.Target.transform.position.z - transform.position.z);
        if (xDistFromTarget < 1.5f && zDistFromTarget < 0.35f)
        {
            // If close enough to safely make an attack
            if (_attackTimer <= 0)
            {
                // Reset the timer
                float diffMod = aiDifficulty > 1 ? Mathf.Log(aiDifficulty, 3) : 0.5f; // Log base 3. Base case of .5 on AI Level 1
                _attackTimer = (2 / diffMod) + (Random.Range(0f, 1f) - 0.5f);
                // Attack!
                if (Random.Range(0f, 100f) <= ATTACK_CHANGEROLE_OFFENSE_CHANCE)
                {
                    return new AI_Decision { action = AI_Action.ATTACK, actionOverride = AI_Action_Override.CHANGEROLE_OFFENSE};
                }
                return new AI_Decision { action = AI_Action.ATTACK };
            }
            else
            {
                _attackTimer -= Time.deltaTime;
            }
        }

        //**
        //**    Perform Squad Role
        //**

        if (role == AI_SquadRole.OFFENSE || _forceOffense)
        {
            return DecideOffense();
        }
        else if (role == AI_SquadRole.COVER_EXIT)
        {
            return DecideCoverExit();
        }
        else if (role == AI_SquadRole.COVER_EXIT_FAR)
        {
            return DecideCoverExitFar();
        }
        else
        {
            return new AI_Decision { action = AI_Action.IDLE };
        }
    }
    #region Offense State
    private AI_Decision DecideOffense()
    {
        // Try to get to the player (right or left, whichever is closer)
        _targetPos = new(
            Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? 1.25f : -1.25f),
            Enemy.Target.transform.position.y, Enemy.Target.transform.position.z
        );
        // Set emergency alternate go-to point to be the other. Is case player is hugging a wall or something.
        _targetPosAlt = new(
            Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? -1.25f : 1.25f),
            Enemy.Target.transform.position.y, Enemy.Target.transform.position.z
        );
        if (Mathf.Abs(Vector3.Distance(transform.position, _targetPos)) < _navTargetLeniencyThreshold)
        {
            // Close enough.
            return new AI_Decision { action = AI_Action.IDLE };
        }

        if (_path != null && _path.corners.Length > 1)
        {
            DrawPath(_path);
            // Path is what we shall now follow! Go to first corner.
            Vector2 moveDir;
            if (_path.corners.Length > 2 && Mathf.Abs(Vector3.Distance(_path.corners[1], Enemy.Target.transform.position)) < _navCornerCutDistance)
            {
                // If this is a complicated path, see if we're close enough to cut a corner now
                moveDir = new(_path.corners[2].x - transform.position.x, _path.corners[2].z - transform.position.z);
            }
            else
            {
                moveDir = new(_path.corners[1].x - transform.position.x, _path.corners[1].z - transform.position.z);
            }
            return new AI_Decision { action = AI_Action.MOVE, data = moveDir.normalized, modifier = _movePercent };
        }
        return new AI_Decision { action = AI_Action.IDLE };
    }
    #endregion
    #region Cover Exit State
    private AI_Decision DecideCoverExit()
    {
        float targetDistance = Vector3.Distance(_targetPos, Enemy.Target.transform.position);
        if (targetDistance > COVER_EXIT_MAX_DIST || targetDistance < COVER_EXIT_MIN_DIST || _path == null || (_path != null && _path.corners.Length < 1))
        {
            // This targetPos is no longer any good, either cause it's no longer in range or cause it's inaccessible
            float randomAcceptableRange = COVER_EXIT_MIN_DIST + Random.Range(0f, COVER_EXIT_MAX_DIST);
            _targetPos = new(
                Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? randomAcceptableRange : -randomAcceptableRange),
                Enemy.Target.transform.position.y, Enemy.Target.transform.position.z + Random.Range(-2f, 2f)
            );
        }
        // Set emergency alternate go-to point to be the other side. In case player is hugging a wall or something.
        if (targetDistance > COVER_EXIT_MAX_DIST || targetDistance < COVER_EXIT_MIN_DIST || _path == null || (_path != null && _path.corners.Length < 1))
        {
            // This targetPos is no longer any good, either cause it's no longer in range or cause it's inaccessible
            float randomAcceptableRange = COVER_EXIT_MIN_DIST + Random.Range(0f, COVER_EXIT_MAX_DIST);
            _targetPosAlt = new(
                Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? -randomAcceptableRange : randomAcceptableRange),
                Enemy.Target.transform.position.y, Enemy.Target.transform.position.z + Random.Range(-2f, 2f)
            );
        }
        if (Mathf.Abs(Vector3.Distance(transform.position, !_favourAltPath ? _targetPos : _targetPosAlt)) < _navTargetLeniencyThreshold)
        {
            // Choosing to Idle
            if (!_coverExitIsIdling)
            {
                // Starting idle behaviour
                _coverExitIsIdling = true;
                _coverExitStartIdleTime = Time.time;
                _coverExitIdleTimeout = Random.Range(COVER_EXIT_MIN_TIMEOUT, COVER_EXIT_MAX_TIMEOUT);
                return new AI_Decision { action = AI_Action.IDLE };
            }
            else if (Time.time > _coverExitStartIdleTime + _coverExitIdleTimeout)
            {
                // We've been idling too long. Go do something else.
                // Hopefully, this leads to the AI crossing to the other side of the player.
                Debug.Log(name + " has idled for too long. Choosing another path...");
                if (Random.Range(0f, 100f) <= COVER_EXIT_TIMEOUT_OFFENSE_CHANCE)
                {
                    // Got bored, go crazy mode on the player
                    StartCoroutine(C_ForceOffenseForTime(COVER_EXIT_TIMEOUT_OVERRIDE_TIME));
                    if (Random.Range(0f, 100f) <= COVER_EXIT_PRESSURE_CHANCE)
                    {
                        _movePercent = 0.50f;   // Apply a 50% speed modifier
                    }
                }
                else
                {
                    // Got bored, go somewhere else
                    StartCoroutine(C_FavourAltPosForTime(COVER_EXIT_TIMEOUT_OVERRIDE_TIME));
                }
            }
            else
            {
                // Still waiting for timeout. Start pressuring players
                return new AI_Decision { action = AI_Action.IDLE };
            }
        }
        // We are not idling.
        _coverExitIsIdling = false;
        if (_path != null && _path.corners.Length > 1)
        {
            DrawPath(_path);
            // Path is what we shall now follow! Go to first corner.
            Vector2 moveDir;
            if (_path.corners.Length > 2 && Mathf.Abs(Vector3.Distance(_path.corners[1], Enemy.Target.transform.position)) < _navCornerCutDistance)
            {
                // If this is a complicated path, see if we're close enough to cut a corner now
                moveDir = new(_path.corners[2].x - transform.position.x, _path.corners[2].z - transform.position.z);
            }
            else
            {
                moveDir = new(_path.corners[1].x - transform.position.x, _path.corners[1].z - transform.position.z);
            }
            _movePercent = 1.0f;
            return new AI_Decision { action = AI_Action.MOVE, data = moveDir.normalized, modifier = _movePercent };
        }
        return new AI_Decision { action = AI_Action.IDLE };
    }
    #endregion

    #region Cover Exit Far State
    private AI_Decision DecideCoverExitFar()
    {
        // Try to stand on either side of the player
        float targetDistanceFar = Vector3.Distance(_targetPos, Enemy.Target.transform.position);
        if (targetDistanceFar > COVER_EXIT_FAR_MAX_DIST || targetDistanceFar < COVER_EXIT_FAR_MIN_DIST || _path == null || (_path != null && _path.corners.Length < 1))
        {
            // This targetPos is no longer any good, either cause it's no longer in range or cause it's inaccessible
            float randomAcceptableRange = COVER_EXIT_FAR_MIN_DIST + Random.Range(0f, COVER_EXIT_FAR_MAX_DIST);
            _targetPos = new(
                Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? randomAcceptableRange : -randomAcceptableRange),
                Enemy.Target.transform.position.y, Enemy.Target.transform.position.z + Random.Range(-2f, 2f)
            );
        }
        // Set emergency alternate go-to point to be the other side. In case player is hugging a wall or something.
        if (targetDistanceFar > COVER_EXIT_FAR_MAX_DIST || targetDistanceFar < COVER_EXIT_FAR_MIN_DIST || _path == null || (_path != null && _path.corners.Length < 1))
        {
            // This targetPos is no longer any good, either cause it's no longer in range or cause it's inaccessible
            float randomAcceptableRange = COVER_EXIT_FAR_MIN_DIST + Random.Range(0f, COVER_EXIT_FAR_MAX_DIST);
            _targetPosAlt = new(
                Enemy.Target.transform.position.x + (transform.position.x > Enemy.Target.transform.position.x ? -randomAcceptableRange : randomAcceptableRange),
                Enemy.Target.transform.position.y, Enemy.Target.transform.position.z + Random.Range(-2f, 2f)
            );
        }
        if (Mathf.Abs(Vector3.Distance(transform.position, !_favourAltPath ? _targetPos : _targetPosAlt)) < _navTargetLeniencyThreshold)
        {
            // Choosing to Idle
            if (!_coverExitIsIdling)
            {
                // Starting idle behaviour
                _coverExitIsIdling = true;
                _coverExitStartIdleTime = Time.time;
                _coverExitIdleTimeout = Random.Range(COVER_EXIT_MIN_TIMEOUT, COVER_EXIT_MAX_TIMEOUT);
                return new AI_Decision { action = AI_Action.IDLE };
            }
            else if (Time.time > _coverExitStartIdleTime + _coverExitIdleTimeout)
            {
                // We've been idling too long. Go do something else.
                // Hopefully, this leads to the AI crossing to the other side of the player.
                StartCoroutine(C_FavourAltPosForTime(5.0f));
            }
            else
            {
                // Still waiting for timeout.
                return new AI_Decision { action = AI_Action.IDLE };
            }
        }
        _coverExitIsIdling = false;
        if (_path != null && _path.corners.Length > 1)
        {
            DrawPath(_path);
            // Path is what we shall now follow! Go to first corner.
            Vector2 moveDir;
            if (_path.corners.Length > 2 && Mathf.Abs(Vector3.Distance(_path.corners[1], Enemy.Target.transform.position)) < _navCornerCutDistance)
            {
                // If this is a complicated path, see if we're close enough to cut a corner now
                moveDir = new(_path.corners[2].x - transform.position.x, _path.corners[2].z - transform.position.z);
            }
            else
            {
                moveDir = new(_path.corners[1].x - transform.position.x, _path.corners[1].z - transform.position.z);
            }
            return new AI_Decision { action = AI_Action.MOVE, data = moveDir.normalized, modifier = _movePercent };
        }
        return new AI_Decision { action = AI_Action.IDLE };
    }
#endregion

    private void RecalculatePath()
    {
        NavMeshPath oldPath = _path;
        NavMesh.CalculatePath(transform.position, (!_favourAltPath ? _targetPos : _targetPosAlt), NavMesh.AllAreas, _path);
        if (_path.corners.Length == 0)
        {
            // This path doesn't work. Try an alternative destination
            NavMesh.CalculatePath(transform.position, (_favourAltPath ? _targetPos : _targetPosAlt), NavMesh.AllAreas, _path);
        }

        if (_path.corners.Length == 0)
        {
            if (_cantMakePathStartTime == -1)
            {
                _cantMakePathStartTime = Time.time;
            }
            else if (Time.time > _cantMakePathStartTime + 3.0f)
            {
                // Has failed to make a path for three seconds. Throw an error that something's gone wrong.
                DeveloperConsole.Log($"Error: Enemy {name} cannot find path to target!");
                return;
            }
            // Just use the old path for now. If this persists, an error will be read
            _path = oldPath;
        }
        else
        {
            // A valid path has been made, reset the timer.
            _cantMakePathStartTime = -1;
        }
        // If the chosen path's last point intersects with another enemy, then try to adjust it to avoid stacking.

        Vector3 last = _path.corners[^1];
        Character closest = EnemyManager.Instance.Enemies
            .Where(e => e != Enemy)
            .OrderBy(e => Vector3.Distance(e.transform.position, last))
            .FirstOrDefault();
        if (closest != null && Vector3.Distance(closest.transform.position, last) < _navAvoidEnemiesDistance)
        {
            // This final path point is no good, it's too close to an existing enemy. Find the closest point that works
            Vector3 sampleDir = new(Random.Range(0f, 1f), 0, Random.Range(0f, 1f));
            sampleDir.Normalize();
            Vector3 sample = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                // Rotate the sampler 45 degrees
                sampleDir = Quaternion.Euler(0f, 45f, 0f) * sampleDir;
                sampleDir.Normalize();
                sample = last + (sampleDir * (_navAvoidEnemiesDistance + 0.1f));
                closest = EnemyManager.Instance.Enemies
                    .OrderBy(e => Vector3.Distance(e.transform.position, sample))
                    .FirstOrDefault();
                if (closest != null && Vector3.Distance(closest.transform.position, sample) >= _navAvoidEnemiesDistance)
                {
                    // This new point is far enough away, consider it valid.
                    break;
                }
                // If the statement failed, then there was another enemy at that sampled point. Try again.
            }
            // Set the found sample to the new last position. It's possible that no sample will work, in which case we just use the last sample as a guess.
            _path.corners[^1] = sample;
        }
    }

    private IEnumerator C_FavourAltPosForTime(float duration)
    {
        _favourAltPath = true;
        yield return new WaitForSeconds(duration);
        _favourAltPath = false;
    }

    private IEnumerator C_ForceOffenseForTime(float duration)
    {
        _forceOffense = true;
        yield return new WaitForSeconds(duration);
        _forceOffense = false;
    }

    public void DrawPath(NavMeshPath path)
    {
        if (path.corners.Length < 2) //if the path has 1 or no corners, there is no need
            return;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(
                new(path.corners[i].x, path.corners[i].y + 0.5f, path.corners[i].z),
                new(path.corners[i + 1].x, path.corners[i + 1].y + 0.5f, path.corners[i + 1].z),
                Color.green, 0.10f);
        }
    }
}


public struct AI_Decision
{
    public AI_Action action;
    public Vector2 data;
    public float modifier;
    public AI_Action_Override actionOverride;
}
