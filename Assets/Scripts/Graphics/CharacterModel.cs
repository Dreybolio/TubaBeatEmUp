using UnityEngine;

public class CharacterModel : MonoBehaviour
{
    public Animator Animator
    {
        get { return _animator; }
    }
    [SerializeField] private Animator _animator;

    public CharacterAnimListener AnimListener
    {
        get { return _animListener; } 
    }
    [SerializeField] private CharacterAnimListener _animListener;

    [SerializeField] private GameObject _modelRoot;

    public void TurnAround()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    public void SetVisible(bool b)
    {
        _modelRoot.SetActive(b);
    }
}
