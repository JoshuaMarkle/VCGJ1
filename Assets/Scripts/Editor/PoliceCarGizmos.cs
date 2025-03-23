using UnityEngine;
using UnityEngine.AI;

public class PoliceCarGizmos : MonoBehaviour
{
    public Transform player;

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        NavMeshPath path = new NavMeshPath();
        // Calculate the path from this object's position to the player's position
        if (NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, path))
        {
            Gizmos.color = Color.red;
            // Draw lines between each corner of the path
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}
