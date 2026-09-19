using UnityEngine;

namespace Popayork.Missions
{
    public enum ValorFuente
    {
        Medido = 0,
        Estimado = 1
    }

    [System.Serializable]
    public struct MedidaParque
    {
        public string nombre;
        public string valor;
        public ValorFuente fuente;
        public string detalleFuente;
    }

    [CreateAssetMenu(fileName = "ParqueConfig", menuName = "Popayork/ParqueConfig")]
    public class ParqueConfig : ScriptableObject
    {
        [Header("Medidas del parque (MEDIDO = del modelo, ESTIMADO = diseño)")]
        public MedidaParque[] medidas = new MedidaParque[0];

        [Header("Puntos clave de Mision1")]
        public Vector3 centroPlaza;
        public float ladoPlaza;
        public float alturaSuelo;
        public Vector3 posicionTorre;
        public Vector3 spawnJugador;
        public Vector3[] spawnAliados = new Vector3[0];
        public Vector3[] spawnOleadas = new Vector3[0];
        public Vector3 objetivoTorre;

        public int Contar(ValorFuente fuente)
        {
            int n = 0;
            if (medidas != null)
            {
                for (int i = 0; i < medidas.Length; i++)
                {
                    if (medidas[i].fuente == fuente)
                    {
                        n++;
                    }
                }
            }
            return n;
        }
    }
}
