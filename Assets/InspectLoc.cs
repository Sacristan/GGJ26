using System;
using UnityEngine;

public class InspectLoc : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1f);
    }
}
