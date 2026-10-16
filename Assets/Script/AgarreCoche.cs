using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche))]
public class AgarreCoche : MonoBehaviour
{
    [Header("Agarre lateral")]
    [SerializeField] private float _rigidezLateral = 4000f;    // N por m/s de deslizamiento
    [SerializeField] private float _coeficienteAgarre = 1.0f;  // 1 = asfalto, 0.5 = arena suelta

    [Header("Freno de mano")]
    [SerializeField, Range(0f, 1f)] private float _agarreTraseroConFreno = 0.35f;   // 35 % del agarre normal
    [SerializeField] private float _frenadoFrenoMano = 3000f;                       // N por m/s de velocidad

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;
    private EntradaCoche _entrada;

    private void Awake()
    {
        _entrada = GetComponent<EntradaCoche>();
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
    }

    private void FixedUpdate()
    {
        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.EstaEnSuelo) continue;

            Vector3 lateral = Vector3.ProjectOnPlane(rueda.Anclaje.right, rueda.Contacto.normal).normalized;
            Vector3 velocidadPunto = _cuerpo.GetPointVelocity(rueda.Anclaje.position);
            float velocidadLateral = Vector3.Dot(velocidadPunto, lateral);

            float fuerza = -velocidadLateral * _rigidezLateral;

            // NUEVO: con el freno de mano, las traseras agarran menos
            float agarre = _coeficienteAgarre;
            bool trasera = !rueda.Delantera;
            if (_entrada.FrenoMano && trasera)
            {
                agarre *= _agarreTraseroConFreno;
            }

            float fuerzaMaxima = rueda.FuerzaSuspension * agarre;
            fuerza = Mathf.Clamp(fuerza, -fuerzaMaxima, fuerzaMaxima);
            _cuerpo.AddForceAtPosition(lateral * fuerza, rueda.Anclaje.position);

            // NUEVO: el freno de mano también frena un poco hacia delante
            if (_entrada.FrenoMano && trasera)
            {
                Vector3 adelante = Vector3.ProjectOnPlane(rueda.Anclaje.forward, rueda.Contacto.normal).normalized;
                float velocidadAdelante = Vector3.Dot(velocidadPunto, adelante);
                _cuerpo.AddForceAtPosition(-adelante * velocidadAdelante * _frenadoFrenoMano, rueda.Anclaje.position);
            }
        }
    }
}