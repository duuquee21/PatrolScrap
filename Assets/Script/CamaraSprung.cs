using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraSprung : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform _objetivo;              // arrastra Patrol

    [Header("Posición")]
    [SerializeField] private float _distanciaMin = 40f;        // distancia en línea recta, parado
    [SerializeField] private float _distanciaMax = 46f;        // a tope de velocidad
    [SerializeField, Range(20f, 85f)] private float _anguloElevacion = 78f;   // 90 = cenital puro (no usar)
    [SerializeField] private float _adelantoMin = 5f;          // el centro del encuadre va delante del coche
    [SerializeField] private float _adelantoMax = 8f;          // a más adelanto, el coche queda más abajo

    [Header("Resorte")]
    [SerializeField] private float _rigidez = 20f;
    [SerializeField, Range(0.2f, 1.5f)] private float _factorAmortiguacion = 0.9f;   // 1 = sin rebote

    [Header("Dirección (estilo drift)")]
    [SerializeField, Range(0f, 1f)] private float _seguirTrayectoria = 1f;   // 0 = sigue el morro, 1 = sigue la velocidad
    [SerializeField] private float _velocidadMinimaTrayectoria = 4f;         // m/s: por debajo usa el morro
    [SerializeField] private float _suavizadoGiro = 2.5f;                    // bajo = gira con más retraso

    [Header("Velocidad")]
    [SerializeField] private float _velocidadReferencia = 25f;   // m/s a los que se llega al máximo

    [Header("FOV")]
    [SerializeField] private float _fovBase = 35f;
    [SerializeField] private float _fovMaximo = 40f;
    [SerializeField] private float _fovExtraTurbo = 5f;
    [SerializeField] private float _suavizadoFov = 4f;

    private Camera _camara;
    private Rigidbody _cuerpoObjetivo;
    private MotorCoche _motor;

    private Vector3 _posicion;
    private Vector3 _velocidadCamara;
    private Vector3 _direccionCamara = Vector3.forward;

    private void Awake()
    {
        _camara = GetComponent<Camera>();
    }

    private void Start()
    {
        _cuerpoObjetivo = _objetivo.GetComponent<Rigidbody>();
        _motor = _objetivo.GetComponent<MotorCoche>();

        // Empezamos ya colocados para que no "vuele" al darle a Play
        _direccionCamara = Vector3.ProjectOnPlane(_objetivo.forward, Vector3.up).normalized;
        _posicion = CalcularPosicionDeseada(0f);
        _velocidadCamara = Vector3.zero;
        transform.position = _posicion;
        transform.rotation = CalcularRotacion();
    }

    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.033f);

        float velocidad = _cuerpoObjetivo.linearVelocity.magnitude;
        float t = Mathf.Clamp01(velocidad / _velocidadReferencia);

        ActualizarDireccion(dt);
        ActualizarPosicion(t, dt);
        transform.rotation = CalcularRotacion();
        ActualizarFov(t, dt);
    }

    // La cámara tiene su propia dirección, que persigue la trayectoria del coche
    private void ActualizarDireccion(float dt)
    {
        Vector3 morro = Vector3.ProjectOnPlane(_objetivo.forward, Vector3.up).normalized;
        Vector3 velocidad = Vector3.ProjectOnPlane(_cuerpoObjetivo.linearVelocity, Vector3.up);

        Vector3 deseada = morro;

        // Solo seguimos la trayectoria si va rápido y hacia delante (no en marcha atrás)
        if (velocidad.magnitude > _velocidadMinimaTrayectoria && Vector3.Dot(velocidad, morro) > 0f)
        {
            deseada = Vector3.Slerp(morro, velocidad.normalized, _seguirTrayectoria);
        }

        float factor = 1f - Mathf.Exp(-_suavizadoGiro * dt);
        _direccionCamara = Vector3.Slerp(_direccionCamara, deseada, factor).normalized;
    }

    private Vector3 CalcularPosicionDeseada(float t)
    {
        float distancia = Mathf.Lerp(_distanciaMin, _distanciaMax, t);
        float adelanto = Mathf.Lerp(_adelantoMin, _adelantoMax, t);
        float angulo = _anguloElevacion * Mathf.Deg2Rad;

        // Centro del encuadre: por delante del coche, así el coche queda en la parte baja
        Vector3 centro = _objetivo.position + _direccionCamara * adelanto;

        return centro
               - _direccionCamara * (distancia * Mathf.Cos(angulo))
               + Vector3.up * (distancia * Mathf.Sin(angulo));
    }

    private void ActualizarPosicion(float t, float dt)
    {
        Vector3 deseada = CalcularPosicionDeseada(t);

        // Resorte: Hooke + amortiguador
        float amortiguacion = 2f * _factorAmortiguacion * Mathf.Sqrt(_rigidez);
        Vector3 aceleracion = (deseada - _posicion) * _rigidez - _velocidadCamara * amortiguacion;

        _velocidadCamara += aceleracion * dt;
        _posicion += _velocidadCamara * dt;

        transform.position = _posicion;
    }

    // Rotación fija por ángulo y dirección: no depende del resorte, así no oscila
    private Quaternion CalcularRotacion()
    {
        float angulo = _anguloElevacion * Mathf.Deg2Rad;
        Vector3 mirada = _direccionCamara * Mathf.Cos(angulo) - Vector3.up * Mathf.Sin(angulo);
        return Quaternion.LookRotation(mirada, Vector3.up);
    }

    private void ActualizarFov(float t, float dt)
    {
        float fovDeseado = Mathf.Lerp(_fovBase, _fovMaximo, t);
        if (_motor != null && _motor.TurboActivo) fovDeseado += _fovExtraTurbo;

        float factor = 1f - Mathf.Exp(-_suavizadoFov * dt);
        _camara.fieldOfView = Mathf.Lerp(_camara.fieldOfView, fovDeseado, factor);
    }
}