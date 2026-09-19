using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    [CreateAssetMenu(fileName = "Mission3Config", menuName = "Popayork/Mission3Config")]
    public class Mission3Config : ScriptableObject
    {
        [Header("Misión 3: descenso en cartón hasta el río")]
        public Vector3[] routePoints = new Vector3[0];
        public CheckpointDef[] gates = new CheckpointDef[0];
        public Vector3 riverCenter;
        public float riverRadius = 18f;
    }
}
