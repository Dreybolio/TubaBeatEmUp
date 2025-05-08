using UnityEngine;

public class CharacterHitbox : MonoBehaviour
{
    public Character Character {
        get { return _character; }
    }
    [SerializeField] private Character _character;
}
