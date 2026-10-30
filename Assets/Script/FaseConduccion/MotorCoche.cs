using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche), typeof(EntradaCoche))]
public class MotorCoche : MonoBehaviour
{
    [Header("Motor")]
    [SerializeField] private float _fuerzaMotor = 9000f;      // N en total
    [SerializeField] private float _fuerzaFreno = 8000f;
    [SerializeField] private float _velocidadMaxima = 25f;    // m/s
    [SerializeField] private float _rozamiento = 100f;        // N por m/s, siempre
    [SerializeField] private float _frenoMotor = 1500f;       // N por m/s, solo sin acelerar

    [Header("Dirección")]
    [SerializeField] private float _anguloMaximo = 30f;               // grados
    [SerializeField] private float _giroAVelocidadMaxima = 0.35f;     // 0.35 = a tope gira un 35 %

    [Header("Turbo")]
    [SerializeField] private float _multiplicadorTurbo = 1.8f;      // empuje extra
    [SerializeField] private float _velocidadMaximaTurbo = 32f;     // m/s con turbo
    [SerializeField] private float _capacidadTurbo = 100f;
    [SerializeField] private float _consumoTurbo = 25f;             // unidades por segundo
    [SerializeField] private float _recargaTurbo = 10f;

    private GasolinaCoche _gasolina;

    private bool HayGasolina => _gasolina == null || _gasolina.TieneGasolina;// unidades por segundo

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;
    private EntradaCoche _entrada;

    private float _depositoTurbo;
    private bool _turboActivo;

    public float DepositoTurbo01 => _depositoTurbo / _capacidadTurbo;   // para la UI más adelante
    public bool TurboActivo => _turboActivo;

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
        _entrada = GetComponent<EntradaCoche>();
        _depositoTurbo = _capacidadTurbo;
        _gasolina = GetComponent<GasolinaCoche>();

    }

    private void FixedUpdate()
    {
        ActualizarTurbo();
        AplicarDireccion();
        AplicarMotor();
    }

    private void ActualizarTurbo()
    {
        _turboActivo = _entrada.Turbo && HayGasolina && _depositoTurbo > 0f && _entrada.Acelerador > 0.1f;

        if (_turboActivo)
        {
            _depositoTurbo -= _consumoTurbo * Time.fixedDeltaTime;
        }
        else
        {
            _depositoTurbo += _recargaTurbo * Time.fixedDeltaTime;
        }

        _depositoTurbo = Mathf.Clamp(_depositoTurbo, 0f, _capacidadTurbo);
    }

    private void AplicarDireccion()
    {
        float proporcionVelocidad = Mathf.Clamp01(_cuerpo.linearVelocity.magnitude / _velocidadMaxima);
        float factorGiro = Mathf.Lerp(1f, _giroAVelocidadMaxima, proporcionVelocidad);
        float angulo = _entrada.Direccion * _anguloMaximo * factorGiro;

        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.Delantera) continue;
            // El Pivote es hijo del anclaje, así que gira con él
            rueda.Anclaje.localRotation = Quaternion.Euler(0f, angulo, 0f);
        }
    }

    private void AplicarMotor()
    {
        // Contamos ruedas motrices en el suelo para repartir la fuerza
        int ruedasMotrices = 0;
        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (rueda.Traccion && rueda.EstaEnSuelo) ruedasMotrices++;
        }
        if (ruedasMotrices == 0) return;   // en el aire no hay empuje

        float acelerador = _entrada.Acelerador;
        float aceleradorMotor = HayGasolina ? acelerador : 0f;   // sin gasolina, el motor no empuja

        // Valores efectivos según si el turbo está activo
        float velocidadTope = _turboActivo ? _velocidadMaximaTurbo : _velocidadMaxima;
        float potencia = _turboActivo ? _fuerzaMotor * _multiplicadorTurbo : _fuerzaMotor;

        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.Traccion || !rueda.EstaEnSuelo) continue;

            // 1) "Adelante" de la rueda, pegado al suelo
            Vector3 adelante = Vector3.ProjectOnPlane(rueda.Anclaje.forward, rueda.Contacto.normal).normalized;

            // 2) Velocidad en esa dirección (negativa = marcha atrás)
            float velocidadAdelante = Vector3.Dot(_cuerpo.linearVelocity, adelante);

            // 3) Motor o freno
            float fuerza;
            if (acelerador * velocidadAdelante < -0.5f)
            {
                fuerza = Mathf.Sign(acelerador) * _fuerzaFreno * Mathf.Abs(acelerador);
            }
            else
            {
                float factorVelocidad = Mathf.Clamp01(1f - Mathf.Abs(velocidadAdelante) / velocidadTope);
                fuerza = aceleradorMotor * potencia * factorVelocidad;
            }

            // 4) Rozamiento base + freno motor (solo al soltar el acelerador)
            float sinAcelerar = 1f - Mathf.Abs(aceleradorMotor);
            fuerza -= velocidadAdelante * (_rozamiento + _frenoMotor * sinAcelerar);

            // 5) Repartir entre ruedas y aplicar
            fuerza /= ruedasMotrices;
            _cuerpo.AddForceAtPosition(adelante * fuerza, rueda.Anclaje.position);
        }
    }
}