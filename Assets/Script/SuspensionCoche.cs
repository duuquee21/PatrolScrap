using UnityEngine;

[System.Serializable]
public class DatosRueda
{
    [Header("Referencias")]
    public Transform Anclaje;   // de aquí sale el SphereCast (y gira con la dirección)
    public Transform Pivote;
    // sube y baja con el resorte

    [Header("Función")]
    public bool Delantera;      // ¿gira con la dirección?
    public bool Traccion;       // ¿recibe fuerza del motor?
    
   

    [HideInInspector] public float CompresionAnterior;
    [HideInInspector] public float DistanciaActual;
    [HideInInspector] public float FuerzaSuspension;
    [HideInInspector] public bool EstaEnSuelo;
    [HideInInspector] public bool HabiaContacto;
    [HideInInspector] public RaycastHit Contacto;
}

[RequireComponent(typeof(Rigidbody))]
public class SuspensionCoche : MonoBehaviour
{
    [Header("Ruedas")]
    [SerializeField] private DatosRueda[] _ruedas;

    [Header("Suspensión (igual para todas)")]
    [SerializeField] private LayerMask _capaSuelo;
    [SerializeField] private float _longitudMaxima = 0.9f;
    [SerializeField] private float _radioRueda = 0.5f;      // = radio REAL de la rueda
    [SerializeField] private float _rigidez = 25000f;
    [SerializeField] private float _amortiguacion = 2500f;

    [Header("Tope de suspensión")]
    [SerializeField, Range(0.5f, 0.95f)] private float _inicioTope = 0.8f;
    [SerializeField] private float _rigidezTope = 150000f;

    [Header("Depuración")]
    [SerializeField] private bool _mostrarGizmos = false;

    private Rigidbody _cuerpo;
    private float _tiempoDiagnostico;

    public DatosRueda[] Ruedas => _ruedas;

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
        // Solo visual: el Pivote baja lo que haya viajado el SphereCast
        foreach (DatosRueda rueda in _ruedas)
        {
            Vector3 posicion = rueda.Pivote.localPosition;
            posicion.y = -rueda.DistanciaActual;
            rueda.Pivote.localPosition = posicion;
        }
    }

    private void CalcularSuspension(DatosRueda rueda)
    {
        Transform anclaje = rueda.Anclaje;

        rueda.EstaEnSuelo = Physics.SphereCast(
            anclaje.position, _radioRueda, -anclaje.up,
            out RaycastHit golpe, _longitudMaxima, _capaSuelo);

        if (!rueda.EstaEnSuelo)
        {
            rueda.DistanciaActual = _longitudMaxima;   // rueda colgando, resorte extendido
            rueda.CompresionAnterior = 0f;
            rueda.FuerzaSuspension = 0f;
            rueda.HabiaContacto = false;
            return;
        }

        rueda.Contacto = golpe;
        rueda.DistanciaActual = golpe.distance;

        // 1) Compresión del resorte (0 = suelto, _longitudMaxima = aplastado)
        float compresion = _longitudMaxima - golpe.distance;

        // En el primer contacto no hay velocidad previa que comparar
        if (!rueda.HabiaContacto)
        {
            rueda.CompresionAnterior = compresion;
            rueda.HabiaContacto = true;
        }

        // 2) Velocidad de compresión (m/s)
        float velocidadCompresion = (compresion - rueda.CompresionAnterior) / Time.fixedDeltaTime;
        rueda.CompresionAnterior = compresion;

        // 3) Hooke + amortiguador (nunca "chupa" el coche hacia el suelo)
        float fuerza = (compresion * _rigidez) + (velocidadCompresion * _amortiguacion);
        fuerza = Mathf.Max(fuerza, 0f);

        // 4) Tope progresivo al final del recorrido
        float compresionTope = _longitudMaxima * _inicioTope;
        if (compresion > compresionTope)
        {
            fuerza += (compresion - compresionTope) * _rigidezTope;
        }

        rueda.FuerzaSuspension = fuerza;

        // 5) Empuje hacia arriba desde la posición de la rueda
        _cuerpo.AddForceAtPosition(anclaje.up * fuerza, anclaje.position);
    }

    private void OnDrawGizmos()
    {
        if (!_mostrarGizmos || _ruedas == null) return;

        foreach (DatosRueda rueda in _ruedas)
        {
            if (rueda.Anclaje == null) continue;

            // Amarillo: recorrido máximo del resorte
            Gizmos.color = Color.yellow;
            Vector3 fin = rueda.Anclaje.position - rueda.Anclaje.up * _longitudMaxima;
            Gizmos.DrawLine(rueda.Anclaje.position, fin);
            Gizmos.DrawWireSphere(fin, _radioRueda);

            // Rojo: la esfera de física ahora mismo (solo en Play)
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Vector3 centro = rueda.Anclaje.position - rueda.Anclaje.up * rueda.DistanciaActual;
                Gizmos.DrawWireSphere(centro, _radioRueda);
            }
        }
    }
    
}