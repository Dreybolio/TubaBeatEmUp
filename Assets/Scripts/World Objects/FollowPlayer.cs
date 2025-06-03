using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
[RequireComponent(typeof(Camera))]
public class FollowPlayer : MonoBehaviour
{
    private Transform[] targets;
    private Camera cam;
    [Header("Camera Properties")]
    [SerializeField] private float leniencyDistance = 2f;
    [SerializeField] private float minAllowedFOV = 60f;
    [SerializeField] private float maxAllowedFOV = 85f;
    [SerializeField] private float fovAcceleration = 5f;
    [SerializeField] private float fovEdgeDistance = 7.5f;
    [SerializeField] private float edgeCollidersHeight = 20f;

    [Header("Wall Creation Variables")]
    [SerializeField] private float nearZSample = 2.5f;
    [SerializeField] private float farZSample = 6.5f;
    [SerializeField, Range(0f, 1f)] private float nearYSample = 0.5f;
    [SerializeField, Range(0f, 1f)] private float farYSample = 0.5f;
    [SerializeField] private float xOffset = 1.5f;

    [Header("Pointers")]
    [SerializeField] private BoxCollider leftWall;
    [SerializeField] private BoxCollider rightWall;

    public delegate void CameraEvent();
    public event CameraEvent OnCameraMoved;

    private float _fov = 60f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        OnCameraMoved += AlignBoxCollidersDelegate;
    }

    private void Update()
    {
        if (targets.Length > 0)
        {
            // We have targets, track them

            // Logic: Average the positions of each player, and move left/right according to where that average is.
            // Try to stay at 60 FOV, but allow for FOV up to 85 if the players are far apart.
            // The players will be physically constrained to always be within this FOV range 
            bool change = false;

            Vector3 avgPos = Vector3.zero;
            for (int i = 0; i < targets.Length; i++)
            {
                avgPos += targets[i].position;
            }
            avgPos /= targets.Length;

            if (transform.position.x + leniencyDistance < avgPos.x)
            {
                // Move to right
                transform.position = new(avgPos.x - leniencyDistance, transform.position.y, transform.position.z);
                change = true;
            }
            else if (transform.position.x - leniencyDistance > avgPos.x)
            {
                // Move to left
                transform.position = new(avgPos.x + leniencyDistance, transform.position.y, transform.position.z);
                change = true;
            }

            // Do FOV Adjusting
            Vector3 furthestPlayer = targets.OrderByDescending(t => Mathf.Abs(cam.WorldToViewportPoint(t.position).x)).FirstOrDefault().position;

            // Find required FOV
            Vector3 localPos = cam.transform.InverseTransformPoint(furthestPlayer);
            float tanVertFOVHalf = (Mathf.Abs(localPos.x) / localPos.z) / cam.aspect;
            float requiredVertFOV = Mathf.Rad2Deg * 2f * Mathf.Atan(tanVertFOVHalf);
            // Add FOV Edge Distance so the camera starts widening a little before the player reaches the exact edge of the screen
            float targetFOV = Mathf.Clamp(requiredVertFOV + fovEdgeDistance, minAllowedFOV, maxAllowedFOV);
            // Lerp the FOV
            if (Mathf.Abs(cam.fieldOfView - targetFOV) > 0.1f)
            {
                _fov = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovAcceleration);
                _fov = Mathf.Round(_fov * 1000f) / 1000f;
            }
            else
            {
                _fov = targetFOV;
            }
            // Apply FOV
            if (cam.fieldOfView != _fov)
            {
                cam.fieldOfView = _fov;
                change = true;
            }

            if (change) OnCameraMoved?.Invoke();
        }
    }
    public void SearchForTargets()
    {
        targets = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Select(pc => pc.transform).ToArray();
        if (targets.Length == 0)
        {
            Debug.LogWarning("FollowPlayer could not find any targets!");
        }
    }

    // To be called by event. We don't want to do this every frame if not necessary cause... That's a lot. If this only
    // happens when the camera moves about then that will reduce the load
    private void AlignBoxCollidersDelegate()
    {
        // -- LEFT WALL -- //
        Vector3 a = cam.ViewportToWorldPoint(new Vector3(0, nearYSample, nearZSample)); a.Set(a.x - xOffset, transform.position.y -edgeCollidersHeight / 2f, a.z);
        Vector3 b = cam.ViewportToWorldPoint(new Vector3(0, farYSample, farZSample)); b.Set(b.x - xOffset, transform.position.y - edgeCollidersHeight / 2f, b.z);
        Vector3 c = new Vector3(b.x, transform.position.y + edgeCollidersHeight / 2f, b.z);
        Vector3 d = new Vector3(a.x, transform.position.y + edgeCollidersHeight / 2f, a.z);

        //Debug.DrawLine(a, b, Color.red, 0.1f);
        //Debug.DrawLine(b, c, Color.red, 0.1f);
        //Debug.DrawLine(c, d, Color.red, 0.1f);
        //Debug.DrawLine(d, a, Color.red, 0.1f);
        //Debug.DrawLine(a, c, Color.red, 0.1f);
        //Debug.DrawLine(d, b, Color.red, 0.1f);

        AlignBoxCollider(leftWall, a, b, c, d, 0.5f);

        // -- RIGHT WALL -- //
        a = cam.ViewportToWorldPoint(new Vector3(1, nearYSample, nearZSample)); a.Set(a.x + xOffset, transform.position.y - edgeCollidersHeight / 2f, a.z);
        b = cam.ViewportToWorldPoint(new Vector3(1, farYSample, farZSample)); b.Set(b.x + xOffset, transform.position.y - edgeCollidersHeight / 2f, b.z);
        c = new Vector3(b.x, transform.position.y + edgeCollidersHeight / 2f, b.z);
        d = new Vector3(a.x, transform.position.y + edgeCollidersHeight / 2f, a.z);

        //Debug.DrawLine(a, b, Color.red, 0.1f);
        //Debug.DrawLine(b, c, Color.red, 0.1f);
        //Debug.DrawLine(c, d, Color.red, 0.1f);
        //Debug.DrawLine(d, a, Color.red, 0.1f);
        //Debug.DrawLine(a, c, Color.red, 0.1f);
        //Debug.DrawLine(d, b, Color.red, 0.1f);

        AlignBoxCollider(rightWall, a, b, c, d, 0.5f);
    }

    /*
     *  Assumption: The input vectors are as such:
     *      p3 ------- p2
     *      |           |
     *      |           |
     *      p0 ------- p1
     */
    private void AlignBoxCollider(BoxCollider col, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float depth)
    {
        Vector3 center = (p0 + p1 + p2 + p3) / 4f;

        Vector3 right = (p1 - p0 + p2 - p3) / 2f;
        Vector3 up = (p3 - p0 + p2 - p1) / 2f;

        float width = right.magnitude;
        float height = up.magnitude;

        Quaternion quat = Quaternion.LookRotation(Vector3.Cross(right, up).normalized, up.normalized);

        col.transform.position = center;
        col.transform.rotation = quat;
        col.size = new Vector3(width, height, depth);
    }
}
