using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche), typeof(EntradaCoche))]
public class MotorCoche : MonoBehaviour
{
    [Header("Motor")]
    [SerializeField] private float _fuerzaMotor = 9000f;       // N en total (repartidos entre ruedas de tracción)
    [SerializeField] private float _fuerzaFreno = 8000f;
    [SerializeField] private float _velocidadMaxima = 25f;     // m/s (90 km/h)
    [SerializeField] private float _rozamiento = 100f;
    [SerializeField] private float _frenoMotor = 1500f;   // N por m/s, solo cuando no aceleras// N por cada m/s de velocidad
    [SerializeField] private float _giroAVelocidadMaxima = 0.35f;   // 0.35 = a tope de velocidad gira un 35 %
    [Header("Dirección")]
    [SerializeField] private float _anguloMaximo = 30f;        // grados

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;
    private EntradaCoche _entrada;

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
        _entrada = GetComponent<EntradaCoche>();
    }

    private void FixedUpdate()
    {
        AplicarDireccion();
        AplicarMotor();
    }

    private void AplicarDireccion()
    {
        float proporcionVelocidad = Mathf.Clamp01(_cuerpo.linearVelocity.magnitude / _velocidadMaxima);
        float factorGiro = Mathf.Lerp(1f, _giroAVelocidadMaxima, proporcionVelocidad);
        float angulo = _entrada.Direccion * _anguloMaximo * factorGiro;

        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.Delantera) continue;
            // Giramos el anclaje sobre su eje vertical. El Pivote es hijo, así que gira con él.
            rueda.Anclaje.localRotation = Quaternion.Euler(0f, angulo, 0f);
        }
    }

    private void AplicarMotor()
    {
        // Contamos cuántas ruedas de tracción tocan suelo para repartir la fuerza
        int ruedasMotrices = 0;
        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (rueda.Traccion && rueda.EstaEnSuelo) ruedasMotrices++;
        }
        if (ruedasMotrices == 0) return;   // en el aire no hay empuje

        float acelerador = _entrada.Acelerador;

        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.Traccion || !rueda.EstaEnSuelo) continue;

            // 1) Dirección "adelante" de la rueda, pegada al suelo
            Vector3 adelante = Vector3.ProjectOnPlane(rueda.Anclaje.forward, rueda.Contacto.normal).normalized;

            // 2) Cuánto avanza el coche en esa dirección (m/s, negativo = marcha atrás)
            float velocidadAdelante = Vector3.Dot(_cuerpo.linearVelocity, adelante);

            // 3) Motor o freno
            float fuerza;
            if (acelerador * velocidadAdelante < -0.5f)
            {
                // Pido lo contrario a lo que hago: freno
                fuerza = Mathf.Sign(acelerador) * _fuerzaFreno * Mathf.Abs(acelerador);
            }
            else
            {
                float factorVelocidad = Mathf.Clamp01(1f - Mathf.Abs(velocidadAdelante) / _velocidadMaxima);
                fuerza = acelerador * _fuerzaMotor * factorVelocidad;
            }

            // 4) Rozamiento base + freno motor (este último solo al soltar el acelerador)
            float sinAcelerar = 1f - Mathf.Abs(acelerador);   // 1 = suelto, 0 = a fondo
            fuerza -= velocidadAdelante * (_rozamiento + _frenoMotor * sinAcelerar);

            // 5) Repartir entre ruedas y aplicar
            fuerza /= ruedasMotrices;
            _cuerpo.AddForceAtPosition(adelante * fuerza, rueda.Anclaje.position);
        }
    }
}