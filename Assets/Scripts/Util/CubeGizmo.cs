using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeGizmo : MonoBehaviour
{
    [SerializeField] private Vector3 size;
    [SerializeField] private Color color;
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
