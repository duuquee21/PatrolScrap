using UnityEngine;

public class ZonaBase : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private SalidaManager _manager;
    [SerializeField] private Transform _coche;                 // Patrol

    [Header("Zona")]
    [SerializeField] private float _radio = 15f;               // m
    [SerializeField] private float _toleranciaAltura = 6f;     // m: diferencia de altura máxima para contar como "dentro"

    [Header("Aro")]
    [SerializeField] private LayerMask _capaSuelo;             // para pegar el aro al terreno
    [SerializeField] private int _segmentos = 64;
    [SerializeField] private float _grosor = 0.7f;             // m
    [SerializeField] private float _alturaSobreSuelo = 0.15f;  // m: evita que el aro parpadee contra el suelo

    [Header("Colores")]
    [SerializeField] private Color _colorInactiva = new Color(0.7f, 0.7f, 0.7f, 0.35f);
    [SerializeField] private Color _colorActiva = new Color(0.3f, 0.95f, 0.45f, 0.9f);
    [SerializeField] private Color _colorDentro = new Color(0.7f, 1f, 0.8f, 1f);
    [SerializeField] private float _velocidadColor = 6f;

    [Header("Onda")]
    [SerializeField] private float _duracionOnda = 2.5f;       // s por oleada
    [SerializeField, Range(0f, 1f)] private float _escalaInicialOnda = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _intensidadOnda = 0.7f;

    private LineRenderer _aro;
    private LineRenderer _onda;
    private Material _material;
    private Color _colorActual;
    private bool _dentro;

    private void Awake()
    {
        if (_manager == null) _manager = FindFirstObjectByType<SalidaManager>();
        if (_coche == null)
        {
            MotorCoche motor = FindFirstObjectByType<MotorCoche>();
            if (motor != null) _coche = motor.transform;
        }

        if (_manager == null || _coche == null)
        {
            Debug.LogError("ZonaBase: falta el SalidaManager o el coche en la escena.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // Sprites/Default pinta el color del vértice y no depende de luces ni del pipeline
        _material = new Material(Shader.Find("Sprites/Default"));

        _aro = CrearLinea("Aro");
        _onda = CrearLinea("Onda");

        Vector3[] puntos = CalcularPuntos();
        _aro.SetPositions(puntos);
        _onda.SetPositions(puntos);

        _colorActual = _colorInactiva;
    }

    private void Update()
    {
        _dentro = EstaDentro();

        // Si estás dentro y la base ya cuenta, el manager inicia la descarga.
        // Si no cuenta todavía (acabas de salir) o ya está descargando, lo ignora solo.
        if (_dentro) _manager.TerminarEnBase();

        ActualizarAspecto();
    }

    private bool EstaDentro()
    {
        Vector3 diferencia = _coche.position - transform.position;
        if (Mathf.Abs(diferencia.y) > _toleranciaAltura) return false;

        diferencia.y = 0f;   // la distancia es solo horizontal: la zona es un círculo
        return diferencia.sqrMagnitude <= _radio * _radio;
    }

    private void ActualizarAspecto()
    {
        Color objetivo = _manager.Descargando ? _colorDentro
                       : !_manager.PuedeTerminarEnBase ? _colorInactiva
                       : _dentro ? _colorDentro
                       : _colorActiva;

        // Mismo suavizado que en la cámara: igual a cualquier framerate
        float factor = 1f - Mathf.Exp(-_velocidadColor * Time.deltaTime);
        _colorActual = Color.Lerp(_colorActual, objetivo, factor);

        PintarLinea(_aro, _colorActual);

        // Onda: crece del centro al borde y se va apagando
        float t = Mathf.Repeat(Time.time / _duracionOnda, 1f);
        float escala = Mathf.Lerp(_escalaInicialOnda, 1f, 1f - (1f - t) * (1f - t));   // arranca rápido y frena (ease out)

        _onda.transform.localScale = new Vector3(escala, 1f, escala);   // Y se queda en 1: no toca la altura

        Color colorOnda = _colorActual;
        colorOnda.a *= (1f - t) * _intensidadOnda;
        PintarLinea(_onda, colorOnda);
    }

    // ---------- Construcción ----------

    private LineRenderer CrearLinea(string nombre)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);   // posición y escala locales neutras

        LineRenderer linea = objeto.AddComponent<LineRenderer>();
        linea.useWorldSpace = false;
        linea.loop = true;
        linea.positionCount = _segmentos;
        linea.widthMultiplier = _grosor;
        linea.sharedMaterial = _material;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        return linea;
    }

    private Vector3[] CalcularPuntos()
    {
        Vector3[] puntos = new Vector3[_segmentos];

        for (int i = 0; i < _segmentos; i++)
        {
            float angulo = (float)i / _segmentos * Mathf.PI * 2f;
            Vector3 mundo = transform.position + new Vector3(Mathf.Cos(angulo) * _radio, 0f, Mathf.Sin(angulo) * _radio);

            // Pegamos el punto al terreno con un rayo hacia abajo
            float y = transform.position.y + _alturaSobreSuelo;
            if (_capaSuelo.value != 0 &&
                Physics.Raycast(mundo + Vector3.up * 50f, Vector3.down, out RaycastHit golpe, 100f, _capaSuelo))
            {
                y = golpe.point.y + _alturaSobreSuelo;
            }

            mundo.y = y;
            puntos[i] = transform.InverseTransformPoint(mundo);
        }

        return puntos;
    }

    private void PintarLinea(LineRenderer linea, Color color)
    {
        linea.startColor = color;
        linea.endColor = color;
    }

    private void OnDrawGizmosSelected()
    {
        // Círculo de ayuda para colocar la base en el editor
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Vector3 anterior = transform.position + new Vector3(_radio, 0f, 0f);
        for (int i = 1; i <= 48; i++)
        {
            float angulo = i / 48f * Mathf.PI * 2f;
            Vector3 siguiente = transform.position + new Vector3(Mathf.Cos(angulo) * _radio, 0f, Mathf.Sin(angulo) * _radio);
            Gizmos.DrawLine(anterior, siguiente);
            anterior = siguiente;
        }
    }
}