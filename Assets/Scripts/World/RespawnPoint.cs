using UnityEngine;

namespace Popayork.World
{
    public class RespawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.0f, 2.0f, 1.0f));
        }
    }
}
