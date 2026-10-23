using UnityEngine;

// Un struct basta aquí: son datos sencillos que solo se leen
[System.Serializable]
public struct Botin
{
    public MaterialChatarra Tipo;
    public int Minimo;
    public int Maximo;
}

public class Destruible : MonoBehaviour
{
    [Header("Rotura")]
    [SerializeField] private float _velocidadMinima = 8f;     // m/s de impacto para romperlo (8 m/s ≈ 29 km/h)

    [Header("Fragmentos (solo visual)")]
    [SerializeField] private GameObject _prefabFragmento;
    [SerializeField] private int _cantidadFragmentos = 10;
    [SerializeField] private Color _colorFragmentos = Color.gray;
    [SerializeField] private float _vidaFragmentos = 4f;       // segundos hasta que desaparecen
    [SerializeField] private float _fuerzaFragmentos = 6f;

    [Header("Botín (piezas recogibles)")]
    [SerializeField] private Recolectable _prefabRecolectable;
    [SerializeField] private Botin[] _botin;

    private Collider _collider;
    private bool _roto;

    private void Awake()
    {
        _collider = GetComponentInChildren<Collider>();
    }

    private void OnCollisionEnter(Collision colision)
    {
        if (_roto) return;

        // Solo nos rompe el coche, no cualquier cosa que nos toque
        Rigidbody otro = colision.rigidbody;
        if (otro == null || !otro.TryGetComponent(out MotorCoche _)) return;

        float impacto = colision.relativeVelocity.magnitude;
        if (impacto < _velocidadMinima) return;

        Romper(otro, impacto);
    }

    private void Romper(Rigidbody coche, float impacto)
    {
        _roto = true;   // evita romperlo dos veces si hay dos contactos en el mismo paso

        // Dirección en la que salen los trozos: del coche hacia el objeto, en horizontal
        Vector3 direccion = transform.position - coche.worldCenterOfMass;
        direccion.y = 0f;
        direccion = direccion.sqrMagnitude > 0.001f ? direccion.normalized : coche.transform.forward;

        // Cuanto más fuerte el impacto, más lejos salen (entre x1 y x2)
        float intensidad = Mathf.Clamp(impacto / _velocidadMinima, 1f, 2f);

        LanzarFragmentos(direccion, intensidad);
        SoltarBotin(direccion);

        Destroy(gameObject);
    }

    private void LanzarFragmentos(Vector3 direccion, float intensidad)
    {
        for (int i = 0; i < _cantidadFragmentos; i++)
        {
            GameObject fragmento = Instantiate(_prefabFragmento, PuntoAleatorio(), Random.rotation);

            if (fragmento.TryGetComponent(out Renderer render))
            {
                render.material.color = _colorFragmentos;
            }

            if (fragmento.TryGetComponent(out Rigidbody cuerpo))
            {
                Vector3 dispersion = Random.insideUnitSphere * 0.5f;
                Vector3 velocidad = (direccion + Vector3.up * 0.6f + dispersion) * (_fuerzaFragmentos * intensidad);
                cuerpo.AddForce(velocidad, ForceMode.VelocityChange);
                cuerpo.AddTorque(Random.insideUnitSphere * 5f, ForceMode.VelocityChange);
            }

            Destroy(fragmento, _vidaFragmentos);
        }
    }

    private void SoltarBotin(Vector3 direccion)
    {
        foreach (Botin botin in _botin)
        {
            if (botin.Tipo == null) continue;

            // Random.Range con enteros excluye el máximo, por eso el +1
            int cantidad = Random.Range(botin.Minimo, botin.Maximo + 1);

            for (int i = 0; i < cantidad; i++)
            {
                Recolectable pieza = Instantiate(_prefabRecolectable, PuntoAleatorio(), Quaternion.identity);
                pieza.Configurar(botin.Tipo);

                if (pieza.TryGetComponent(out Rigidbody cuerpo))
                {
                    Vector3 salto = direccion * 0.5f + Vector3.up + Random.insideUnitSphere * 0.5f;
                    cuerpo.AddForce(salto * 4f, ForceMode.VelocityChange);
                }
            }
        }
    }

    // Punto al azar en la mitad de arriba del objeto, para no crear nada dentro del suelo
    private Vector3 PuntoAleatorio()
    {
        Bounds b = _collider.bounds;
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.center.y, b.max.y),
            Random.Range(b.min.z, b.max.z));
    }
}