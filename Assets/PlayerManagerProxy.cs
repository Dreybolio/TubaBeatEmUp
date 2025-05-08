using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManagerProxy : MonoBehaviour
{
    private PlayerManager _pm;
    private PlayerManager pm
    {
        #region
        get
        {
            if (_pm == null)
            {
                PlayerManager[] objs = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
                if (objs.Length > 0)
                {
                    _pm = objs[0];
                }
                if (_pm == null)
                {
                    Debug.LogError("Error: Could not find a PlayerManager to be a proxy of");
                }
            }
            return _pm;
        }
        #endregion
    }
    private PlayerInputManager _pim;
    private PlayerInputManager pim
    {
    #region
        get
        {
            if (_pim == null)
            {
                PlayerInputManager[] objs = FindObjectsByType<PlayerInputManager>(FindObjectsSortMode.None);
                if (objs.Length > 0)
                {
                    _pim = objs[0];
                }
                if (_pim == null)
                {
                    Debug.LogError("Error: Could not find a PlayerManager to be a proxy of");
                }
            }
            return _pim;
        }
    #endregion
    }
    public void AllowJoining(bool b)
    {
        if (b) pim.EnableJoining();
        else pim.DisableJoining();
    }
    public void SetActionMap(string map)
    {
        pm.SetAllPlayersActionMap(map);
    }
}
