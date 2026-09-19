using UnityEngine;

namespace Popayork.Missions
{
    [CreateAssetMenu(fileName = "ParqueCaldasConfig", menuName = "Popayork/ParqueCaldasConfig")]
    public class ParqueCaldasConfig : ScriptableObject
    {
        [Header("Medidas (DOCUMENTADO / MEDIDO / ESTIMADO)")]
        public MedidaParque[] medidas = new MedidaParque[0];

        [Header("Puntos clave (origen = base de la estatua)")]
        public Vector3 estatuaPos;
        public Vector3 torrePos;
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
