using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche))]
public class AgarreCoche : MonoBehaviour
{
    [Header("Agarre lateral")]
    [SerializeField] private float _rigidezLateral = 4000f;   // N por m/s de deslizamiento
    [SerializeField] private float _coeficienteAgarre = 1.0f; // 1 = asfalto, 0.5 = arena suelta

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
    }

    private void FixedUpdate()
    {
        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (!rueda.EstaEnSuelo) continue;

            // 1) Eje lateral de la rueda, pegado al suelo
            Vector3 lateral = Vector3.ProjectOnPlane(rueda.Anclaje.right, rueda.Contacto.normal).normalized;

            // 2) Velocidad del punto de la rueda (incluye el efecto de girar)
            Vector3 velocidadPunto = _cuerpo.GetPointVelocity(rueda.Anclaje.position);

            // 3) Cuánto se desliza de lado (m/s)
            float velocidadLateral = Vector3.Dot(velocidadPunto, lateral);

            // 4) Fuerza contraria al deslizamiento
            float fuerza = -velocidadLateral * _rigidezLateral;

            // 5) Límite de adherencia: no más de lo que permite el peso sobre la rueda
            float fuerzaMaxima = rueda.FuerzaSuspension * _coeficienteAgarre;
            fuerza = Mathf.Clamp(fuerza, -fuerzaMaxima, fuerzaMaxima);

            _cuerpo.AddForceAtPosition(lateral * fuerza, rueda.Anclaje.position);
        }
    }
}