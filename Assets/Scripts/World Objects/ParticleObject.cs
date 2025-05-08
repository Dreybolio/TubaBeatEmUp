using UnityEngine;
[RequireComponent (typeof(AnimatorListener))]
public class ParticleObject : MonoBehaviour
{
    [SerializeField] protected AnimatorListener _animListener;
    protected void OnEnable()
    {
        _animListener.OnEvent01 += DestroySelf;
    }
    protected void OnDisable()
    {
        _animListener.OnEvent01 -= DestroySelf;
    }

    protected void DestroySelf()
    {
        Destroy(gameObject);
    }
}
