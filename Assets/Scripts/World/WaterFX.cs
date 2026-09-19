using UnityEngine;

namespace Popayork.World
{
    // Agua simple: plano semitransparente con pulso y espumas a la deriva.
    public class WaterFX : MonoBehaviour
    {
        [SerializeField] private Transform[] foam = new Transform[0];
        [SerializeField] private float flowSpeed = 2f;
        [SerializeField] private float span = 60f;

        private Material waterMat;
        private Color baseColor = new Color(0.15f, 0.45f, 0.75f, 0.8f);

        public void Bind(Transform[] foamPieces, Material water)
        {
            foam = foamPieces;
            waterMat = water;
        }

        private void Update()
        {
            if (waterMat != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.7f);
                Color c = baseColor;
                c.b += pulse * 0.1f;
                waterMat.color = c;
            }
            if (foam != null)
            {
                for (int i = 0; i < foam.Length; i++)
                {
                    if (foam[i] == null)
                    {
                        continue;
                    }
                    Vector3 p = foam[i].position;
                    p.z += flowSpeed * Time.deltaTime;
                    if (p.z > span)
                    {
                        p.z -= span * 2f;
                    }
                    foam[i].position = p;
                }
            }
        }
    }
}
