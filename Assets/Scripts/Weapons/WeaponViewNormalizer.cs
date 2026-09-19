using UnityEngine;

namespace Popayork.Weapons
{
    public static class WeaponViewNormalizer
    {
        public struct Correction
        {
            public float appliedScale;
            public Vector3 appliedOffset;
            public float measuredLength;
        }

        // Corrige por código tamaño y pivote: centra el modelo en X/Z del holder
        // y lo escala a targetLengthMeters. Se llama una vez al construir/equipar.
        public static Correction Normalize(GameObject modelRoot, float targetLengthMeters)
        {
            var result = new Correction { appliedScale = 1.0f, appliedOffset = Vector3.zero };
            if (modelRoot == null)
            {
                return result;
            }
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return result;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            Vector3 size = bounds.size;
            float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            result.measuredLength = longest;
            if (longest > 0.0001f && targetLengthMeters > 0.0f)
            {
                result.appliedScale = targetLengthMeters / longest;
            }
            Vector3 localCenter = modelRoot.transform.InverseTransformPoint(bounds.center);
            result.appliedOffset = new Vector3(-localCenter.x, 0.0f, -localCenter.z);
            modelRoot.transform.localPosition += result.appliedOffset;
            modelRoot.transform.localScale = Vector3.one * result.appliedScale;
            return result;
        }
    }
}
