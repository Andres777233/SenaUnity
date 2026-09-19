using UnityEngine;
using Popayork.Enemies;

namespace Popayork.Missions
{
    public enum CheckpointKind
    {
        Ride = 0,
        Fight = 1,
        Arrival = 2
    }

    [System.Serializable]
    public struct CheckpointDef
    {
        public string nombre;
        public CheckpointKind kind;
        public Vector3 position;
        public float radius;
        public WaveData fightWave;
    }

    [CreateAssetMenu(fileName = "Mission2Config", menuName = "Popayork/Mission2Config")]
    public class Mission2Config : ScriptableObject
    {
        [Header("Misión 2, primera mitad: ruta al Morro")]
        public Vector3[] routePoints = new Vector3[0];
        public CheckpointDef[] checkpoints = new CheckpointDef[0];
        public float corridorHalfWidth = 12f;
        public Vector3 morroTop;
    }
}
