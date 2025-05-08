using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float leniencyDistance = 2f;

    private void Update()
    {
        if (transform.position.x + leniencyDistance < target.position.x)
        {
            // Move to right
            transform.position = new(target.position.x - leniencyDistance, transform.position.y, transform.position.z);
        }
        else if (transform.position.x - leniencyDistance > target.position.x)
        {
            // Move to left
            transform.position = new(target.position.x + leniencyDistance, transform.position.y, transform.position.z);
        }
    }
}
