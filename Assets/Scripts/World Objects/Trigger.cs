using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Trigger : MonoBehaviour
{
    [Header("Generic")]
    [SerializeField] protected LayerMask triggerableLayers;
    [SerializeField] protected bool triggerOnce = false;

    public UnityEvent OnTrigger;

    private bool _triggered = false;

    protected void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        int thisLayer = 1 << other.gameObject.layer;
        if ((triggerableLayers & thisLayer) != 0)
        {
            // A player has collided with this
            OnTrigger?.Invoke();
            if (triggerOnce)
            {
                _triggered = true;
                Destroy(gameObject, 0.25f);
            }
        }
    }
}
