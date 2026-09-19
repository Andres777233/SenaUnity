using UnityEngine;

namespace Popayork.World
{
    // Paloma ambiental: vuela en círculo sobre la plaza. Sin asignaciones.
    public class PalomaVuelo : MonoBehaviour
    {
        [SerializeField] private Vector3 centro = Vector3.zero;
        [SerializeField] private float radio = 12f;
        [SerializeField] private float altura = 8f;
        [SerializeField] private float velocidadAngular = 0.5f;
        [SerializeField] private float fase;

        private float angulo;

        public void Configurar(Vector3 centroVuelo, float radioVuelo, float alturaVuelo, float velocidad, float faseInicial)
        {
            centro = centroVuelo;
            radio = Mathf.Max(2f, radioVuelo);
            altura = alturaVuelo;
            velocidadAngular = velocidad;
            fase = faseInicial;
            angulo = faseInicial;
        }

        private void Update()
        {
            angulo += velocidadAngular * Time.deltaTime;
            float c = Mathf.Cos(angulo);
            float s = Mathf.Sin(angulo);
            Vector3 pos = centro + new Vector3(c * radio, altura + Mathf.Sin(angulo * 2f) * 0.5f, s * radio);
            Vector3 siguiente = centro + new Vector3(Mathf.Cos(angulo + 0.1f) * radio, altura, Mathf.Sin(angulo + 0.1f) * radio);
            transform.position = pos;
            Vector3 frente = siguiente - pos;
            if (frente.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(frente.normalized);
            }
        }
    }
}
