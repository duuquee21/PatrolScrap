using UnityEngine;

public class CentrarRueda : MonoBehaviour
{
    [ContextMenu("Medir y centrar")]
    private void MedirYCentrar()
    {
        Renderer render = GetComponentInChildren<Renderer>();
        Bounds b = render.bounds;   // ya incluye la escala: son metros reales

        // Mueve el modelo para que el centro de su caja coincida con el del padre (el Pivote)
        transform.position += transform.parent.position - b.center;

        float diametro = Mathf.Max(b.size.x, b.size.y, b.size.z);
        Debug.Log($"Tamaño: {b.size}  ->  RADIO = {diametro / 2f}");
    }
}