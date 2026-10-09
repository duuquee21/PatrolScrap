using UnityEngine;

[System.Serializable]
public class DatosRueda
{
    [Header("Referencias")]
    public Transform Anclaje;   // Rueda_XX: de aquí sale el SphereCast
    public Transform Pivote;    // lo que sube y baja visualmente

    [Header("Función")]          
    public bool Delantera;       
    public bool Traccion;

    // Estado de ejecución (no hace falta verlo en el Inspector)
    [HideInInspector] public float CompresionAnterior;
    [HideInInspector] public float DistanciaActual;
    [HideInInspector] public bool EstaEnSuelo;
    [HideInInspector] public RaycastHit Contacto;
    [HideInInspector] public float FuerzaSuspension;
}

[RequireComponent(typeof(Rigidbody))]
public class SuspensionCoche : MonoBehaviour
{
    [Header("Ruedas")]
    [SerializeField] private DatosRueda[] _ruedas;

    [Header("Suspensión (igual para todas)")]
    [SerializeField] private LayerMask _capaSuelo;
    [SerializeField] private float _longitudMaxima = 0.6f;
    [SerializeField] private float _radioRueda = 0.35f;
    [SerializeField] private float _rigidez = 25000f;
    [SerializeField] private float _amortiguacion = 2500f;

    private Rigidbody _cuerpo;

    public DatosRueda[] Ruedas => _ruedas;   // para que el motor las lea después

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        foreach (DatosRueda rueda in _ruedas)
        {
            CalcularSuspension(rueda);
        }
    }

    private void Update()
    {
        ActualizarVisuales();
    }

    private void CalcularSuspension(DatosRueda rueda)
    {
        Transform anclaje = rueda.Anclaje;

        rueda.EstaEnSuelo = Physics.SphereCast(
            anclaje.position, _radioRueda, -anclaje.up,
            out RaycastHit golpe, _longitudMaxima, _capaSuelo);

        if (!rueda.EstaEnSuelo)
        {
            rueda.DistanciaActual = _longitudMaxima;
            rueda.CompresionAnterior = 0f;
            rueda.FuerzaSuspension = 0f;
            return;
        }

        rueda.Contacto = golpe;
        rueda.DistanciaActual = golpe.distance;

        float compresion = _longitudMaxima - golpe.distance;
        float velocidadCompresion = (compresion - rueda.CompresionAnterior) / Time.fixedDeltaTime;
        rueda.CompresionAnterior = compresion;

        float fuerza = (compresion * _rigidez) + (velocidadCompresion * _amortiguacion);
        fuerza = Mathf.Max(fuerza, 0f);
        rueda.FuerzaSuspension = fuerza;

        _cuerpo.AddForceAtPosition(anclaje.up * fuerza, anclaje.position);
    }

    private void ActualizarVisuales()
    {
        foreach (DatosRueda rueda in _ruedas)
        {
            Vector3 posicion = rueda.Pivote.localPosition;
            posicion.y = -rueda.DistanciaActual;   // solo cambiamos la altura
            rueda.Pivote.localPosition = posicion;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_ruedas == null) return;

        Gizmos.color = Color.yellow;
        foreach (DatosRueda rueda in _ruedas)
        {
            if (rueda.Anclaje == null) continue;
            Vector3 inicio = rueda.Anclaje.position;
            Vector3 fin = inicio - rueda.Anclaje.up * _longitudMaxima;
            Gizmos.DrawLine(inicio, fin);
            Gizmos.DrawWireSphere(fin, _radioRueda);
        }
    }
}