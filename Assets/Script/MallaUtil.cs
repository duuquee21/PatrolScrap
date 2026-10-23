using UnityEngine;

public static class MallaUtil
{
    // Instancia el modelo como hijo de "padre", lo escala (uniforme) a un tamaño y lo centra.
    // Devuelve la instancia. El padre debería tener rotación (0,0,0) al llamarlo.
    public static GameObject Colocar(GameObject prefab, Transform padre, float tamanoObjetivo)
    {
        GameObject instancia = Object.Instantiate(prefab, padre);
        instancia.name = prefab.name;
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.identity;
        // La escala del prefab se respeta: así vale la escala de importación del asset

        // El modelo es solo visual: fuera colliders y rigidbodies
        foreach (Collider coll in instancia.GetComponentsInChildren<Collider>())
        {
            coll.enabled = false;
            Object.Destroy(coll);
        }
        foreach (Rigidbody cuerpo in instancia.GetComponentsInChildren<Rigidbody>())
        {
            Object.Destroy(cuerpo);
        }

        if (!TryObtenerLimites(instancia, out Bounds limites)) return instancia;   // sin mesh: nada que ajustar

        // Escala uniforme: la dimensión mayor pasa a medir "tamanoObjetivo"
        float mayor = Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
        if (tamanoObjetivo > 0f && mayor > 0.0001f)
        {
            instancia.transform.localScale *= tamanoObjetivo / mayor;
        }

        // Centramos el modelo sobre el padre (con la escala ya aplicada)
        if (TryObtenerLimites(instancia, out limites))
        {
            instancia.transform.position += padre.position - limites.center;
        }

        return instancia;
    }

    private static bool TryObtenerLimites(GameObject objeto, out Bounds limites)
    {
        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
        limites = default;
        if (renderers.Length == 0) return false;

        limites = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) limites.Encapsulate(renderers[i].bounds);
        return true;
    }
}