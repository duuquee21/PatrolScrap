using UnityEngine;

[RequireComponent(typeof(InventarioCoche))]
public class RecolectorCoche : MonoBehaviour
{
    [Header("Zona de recogida")]
    [SerializeField] private Vector3 _centro = new Vector3(0f, -0.5f, 0f);   // relativo a Patrol
    [SerializeField] private float _radio = 2.5f;
    [SerializeField] private LayerMask _capaRecolectable;

    private readonly Collider[] _buffer = new Collider[32];   // se reutiliza: no genera basura cada paso
    private InventarioCoche _inventario;

    private void Awake()
    {
        _inventario = GetComponent<InventarioCoche>();
    }

    private void FixedUpdate()
    {
        Vector3 centro = transform.TransformPoint(_centro);
        int encontrados = Physics.OverlapSphereNonAlloc(centro, _radio, _buffer, _capaRecolectable);

        for (int i = 0; i < encontrados; i++)
        {
            Recolectable pieza = _buffer[i].GetComponentInParent<Recolectable>();
            if (pieza == null) continue;
            if (!pieza.PuedeRecogerse) continue;

            if (_inventario.TryAnadir(pieza.Tipo, pieza.Cantidad))
            {
                // SetActive(false) la quita de las consultas al instante.
                // Destroy se ejecuta al final del frame, y podría recogerse dos veces antes.
                pieza.gameObject.SetActive(false);
                Destroy(pieza.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.TransformPoint(_centro), _radio);
    }
}