using UnityEngine;

namespace Popayork.Missions
{
    public enum ArbolTipo
    {
        Araucaria = 0,
        Guayacan = 1,
        Corcho = 2,
        FlorDeMayo = 3,
        Mango = 4,
        Seto = 5
    }

    [System.Serializable]
    public struct ArbolPuesto
    {
        public ArbolTipo tipo;
        public Vector3 posicion;
        public float rotacionY;
        public float escala;
    }

    [CreateAssetMenu(fileName = "ParqueVegetacion", menuName = "Popayork/ParqueVegetacion")]
    public class ParqueVegetacion : ScriptableObject
    {
        [Header("Posiciones explícitas de vegetación (ESTIMADO salvo indicación)")]
        public ArbolPuesto[] arboles = new ArbolPuesto[0];
    }
}
