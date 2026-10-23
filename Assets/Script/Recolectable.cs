using UnityEngine;

public class Recolectable : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private MaterialChatarra _tipo;
    [SerializeField] private int _cantidad = 1;
    [SerializeField] private float _retrasoRecogida = 1f;   // segundos tras aparecer en los que no se puede recoger

    [Header("Vuelo")]
    [SerializeField] private LayerMask _capaSuelo;                 // si se deja vacía, usa la capa "Suelo"
    [SerializeField] private float _gravedad = 9f;                 // más baja que la real: caen más despacio
    [SerializeField] private float _rozamientoHorizontal = 2.5f;   // lo rápido que pierden el empuje lateral
    [SerializeField, Range(0f, 1f)] private float _rebote = 0.3f;  // fracción de la velocidad de caída que se conserva al llegar

    [Header("Flotación")]
    [SerializeField] private float _alturaFlotando = 0.6f;         // m sobre el suelo
    [SerializeField] private float _amplitudBalanceo = 0.15f;      // m que sube y baja
    [SerializeField] private float _frecuenciaBalanceo = 0.8f;     // oscilaciones por segundo
    [SerializeField] private float _rigidezResorte = 35f;          // cuánto tira hacia su altura
    [SerializeField, Range(0.1f, 1f)] private float _amortiguacion = 0.35f;   // 1 = sin rebote, menos = rebota más
    [SerializeField] private float _giro = 70f;                    // grados por segundo
    [SerializeField] private float _inclinacion = 8f;              // grados de balanceo

    private float _momentoRecogible;   // por defecto vale 0: una pieza colocada a mano se recoge siempre

    private Vector3 _velocidad;
    private bool _volando;
    private float _alturaSuelo;
    private bool _sueloCalculado;
    private float _fase;
    private float _yaw;

    public MaterialChatarra Tipo => _tipo;
    public int Cantidad => _cantidad;
    public bool PuedeRecogerse => Time.time >= _momentoRecogible;

    private void Awake()
    {
        if (_capaSuelo.value == 0) _capaSuelo = LayerMask.GetMask("Suelo");

        // Ya no la mueve la física: la mueve este script
        if (TryGetComponent(out Rigidbody cuerpo))
        {
            cuerpo.useGravity = false;
            cuerpo.isKinematic = true;
        }

        _fase = Random.value * Mathf.PI * 2f;    // para que no oscilen todas a la vez
        _yaw = Random.value * 360f;
        _alturaSuelo = transform.position.y - _alturaFlotando;
    }

    // La llama quien crea la pieza (el objeto destruible)
    public void Configurar(MaterialChatarra tipo, int cantidad = 1)
    {
        _tipo = tipo;
        _cantidad = cantidad;
        _momentoRecogible = Time.time + _retrasoRecogida;

        if (tipo.PrefabPieza != null)
        {
            ColocarModelo(tipo);
        }
        else
        {
            // Sin modelo: el cubo de siempre, pintado con el color del material
            Renderer render = GetComponentInChildren<Renderer>();
            if (render != null) render.material.color = tipo.Color;
        }
    }

    // Lanza la pieza en arco con esta velocidad inicial (m/s)
    public void Lanzar(Vector3 velocidad)
    {
        _velocidad = velocidad;
        _volando = true;
    }

    private void ColocarModelo(MaterialChatarra tipo)
    {
        // Primero ocultamos el cubo de relleno del prefab (antes de crear el modelo, o lo ocultaríamos también)
        foreach (Renderer render in GetComponentsInChildren<Renderer>()) render.enabled = false;

        MallaUtil.Colocar(tipo.PrefabPieza, transform, tipo.TamanoPieza);

        // La zona de colisión se ajusta al tamaño del modelo
        if (TryGetComponent(out BoxCollider caja))
        {
            float escala = Mathf.Max(transform.lossyScale.x, 0.0001f);   // el prefab raíz puede tener escala
            caja.center = Vector3.zero;
            caja.size = Vector3.one * (tipo.TamanoPieza / escala);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        ActualizarSuelo();

        // Altura a la que quiere estar: suelo + flotación + un balanceo suave
        float balanceo = Mathf.Sin(Time.time * _frecuenciaBalanceo * Mathf.PI * 2f + _fase);
        float objetivo = _alturaSuelo + _alturaFlotando + balanceo * _amplitudBalanceo;

        Vector3 posicion = transform.position;

        // Horizontal: se desliza y va perdiendo velocidad
        posicion.x += _velocidad.x * dt;
        posicion.z += _velocidad.z * dt;
        float freno = Mathf.Exp(-_rozamientoHorizontal * dt);   // igual a cualquier framerate
        _velocidad.x *= freno;
        _velocidad.z *= freno;

        if (_volando)
        {
            _velocidad.y -= _gravedad * dt;
            posicion.y += _velocidad.y * dt;

            // Al bajar hasta su altura, pasa a flotar. Conserva un poco de velocidad: ahí nace el rebote
            if (posicion.y <= objetivo && _velocidad.y < 0f)
            {
                _volando = false;
                _velocidad.y *= _rebote;
            }
        }
        else
        {
            // Resorte vertical: Hooke + amortiguador, el mismo principio que la suspensión
            float amortiguacion = 2f * _amortiguacion * Mathf.Sqrt(_rigidezResorte);
            float aceleracion = (objetivo - posicion.y) * _rigidezResorte - _velocidad.y * amortiguacion;

            _velocidad.y += aceleracion * dt;
            posicion.y += _velocidad.y * dt;

            // Nunca por debajo del suelo
            posicion.y = Mathf.Max(posicion.y, _alturaSuelo + 0.1f);
        }

        transform.position = posicion;

        // Giro lento con un ligero balanceo de inclinación
        _yaw += _giro * dt;
        float fase = Time.time * _frecuenciaBalanceo * Mathf.PI * 2f + _fase;
        transform.rotation = Quaternion.Euler(
            Mathf.Sin(fase) * _inclinacion,
            _yaw,
            Mathf.Cos(fase) * _inclinacion);
    }

    // Busca la altura del suelo bajo la pieza. Solo repite el rayo si se está moviendo de lado
    private void ActualizarSuelo()
    {
        bool seMueve = _velocidad.x * _velocidad.x + _velocidad.z * _velocidad.z > 0.01f;
        if (_sueloCalculado && !seMueve) return;

        Vector3 origen = transform.position + Vector3.up * 3f;
        if (Physics.Raycast(origen, Vector3.down, out RaycastHit golpe, 200f, _capaSuelo))
        {
            _alturaSuelo = golpe.point.y;
            _sueloCalculado = true;
        }
    }
}