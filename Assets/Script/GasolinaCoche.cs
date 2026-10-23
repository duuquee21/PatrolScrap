using UnityEngine;

[RequireComponent(typeof(EntradaCoche))]
public class GasolinaCoche : MonoBehaviour
{
    [Header("Depósito")]
    [SerializeField] private float _capacidad = 100f;
    [SerializeField, Range(0f, 1f)] private float _umbralReserva = 0.2f;   // por debajo, aviso de reserva

    [Header("Consumo (unidades por segundo)")]
    [SerializeField] private float _consumoRalenti = 0.1f;        // siempre
    [SerializeField] private float _consumoAcelerando = 0.5f;     // a fondo; se escala con el acelerador
    [SerializeField] private float _consumoTurbo = 2f;            // extra mientras el turbo está activo

    [Header("Colisiones")]
    [SerializeField] private float _impactoMinimo = 6f;           // m/s: por debajo no cuesta nada
    [SerializeField] private float _perdidaPorImpacto = 0.8f;     // unidades por cada m/s por encima del mínimo
    [SerializeField] private float _factorCarroceria = 1f;        // la mejora Carrocería lo bajará (0.5 = la mitad)

    private EntradaCoche _entrada;
    private MotorCoche _motor;
    private float _actual;

    public float Actual => _actual;
    public float Capacidad => _capacidad;
    public float Nivel01 => _capacidad > 0f ? _actual / _capacidad : 0f;
    public bool TieneGasolina => _actual > 0f;
    public bool EnReserva => Nivel01 <= _umbralReserva;

    // El juego se suscribirá a esto más adelante (fin de la salida, aviso, etc.)
    public event System.Action SeAgoto;

    private void Awake()
    {
        _entrada = GetComponent<EntradaCoche>();
        _motor = GetComponent<MotorCoche>();   // puede ser null: entonces no hay gasto de turbo
        _actual = _capacidad;
    }

    private void FixedUpdate()
    {
        if (_actual <= 0f) return;

        float consumo = _consumoRalenti + _consumoAcelerando * Mathf.Abs(_entrada.Acelerador);
        if (_motor != null && _motor.TurboActivo) consumo += _consumoTurbo;

        Consumir(consumo * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision colision)
    {
        // Romper objetos es el objetivo del juego: no se penaliza (borra esta línea si quieres que cueste)
        if (colision.collider.GetComponentInParent<Destruible>() != null) return;

        float impacto = colision.relativeVelocity.magnitude;
        if (impacto < _impactoMinimo) return;

        float perdida = (impacto - _impactoMinimo) * _perdidaPorImpacto * _factorCarroceria;
        Consumir(perdida);
    }

    public void Consumir(float cantidad)
    {
        if (_actual <= 0f) return;   // ya vacío: evita avisar dos veces

        _actual = Mathf.Max(0f, _actual - cantidad);
        if (_actual <= 0f) SeAgoto?.Invoke();
    }

    // Para la tienda y el garaje, más adelante
    public void Rellenar(float cantidad)
    {
        _actual = Mathf.Min(_capacidad, _actual + cantidad);
    }

    public void RellenarTodo()
    {
        _actual = _capacidad;
    }
}