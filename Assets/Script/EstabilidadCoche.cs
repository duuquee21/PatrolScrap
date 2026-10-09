using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche))]
public class EstabilidadCoche : MonoBehaviour
{
    [Header("Centro de masa")]
    [SerializeField] private Vector3 _centroDeMasa = new Vector3(0f, -0.4f, 0f);

    [Header("Damping rotacional (aceleración por rad/s)")]
    [SerializeField] private float _dampingYaw = 1.5f;     // giro izquierda/derecha
    [SerializeField] private float _dampingRoll = 4f;      // vuelco lateral
    [SerializeField] private float _dampingPitch = 4f;     // cabeceo

    [Header("Asistencia de conducción")]
    [SerializeField] private bool _ayudasActivas = true;
    [SerializeField] private float _velocidadMinimaAsistencia = 5f;   // m/s
    [SerializeField] private float _fuerzaAlineacion = 0.05f;         // torque por grado de derrape
    [SerializeField] private float _anguloMaximoAsistencia = 45f;     // grados

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
        _cuerpo.centerOfMass = _centroDeMasa;
    }

    private void FixedUpdate()
    {
        AplicarDampingRotacional();

        if (_ayudasActivas)
        {
            AplicarAlineacion();
        }
    }

    private void AplicarDampingRotacional()
    {
        // Velocidad de giro expresada en los ejes del coche (x = pitch, y = yaw, z = roll)
        Vector3 velocidadLocal = transform.InverseTransformDirection(_cuerpo.angularVelocity);

        Vector3 torqueLocal = new Vector3(
            -velocidadLocal.x * _dampingPitch,
            -velocidadLocal.y * _dampingYaw,
            -velocidadLocal.z * _dampingRoll);

        // Acceleration = ignora la masa y la inercia, así los valores no dependen del peso del coche
        _cuerpo.AddRelativeTorque(torqueLocal, ForceMode.Acceleration);
    }

    private void AplicarAlineacion()
    {
        // Sin al menos 2 ruedas en el suelo no hay agarre que alinear
        int ruedasEnSuelo = 0;
        foreach (DatosRueda rueda in _suspension.Ruedas)
        {
            if (rueda.EstaEnSuelo) ruedasEnSuelo++;
        }
        if (ruedasEnSuelo < 2) return;

        // Velocidad horizontal respecto al coche
        Vector3 velocidadPlano = Vector3.ProjectOnPlane(_cuerpo.linearVelocity, transform.up);
        if (velocidadPlano.magnitude < _velocidadMinimaAsistencia) return;

        // Solo hacia delante, no en marcha atrás
        if (Vector3.Dot(velocidadPlano, transform.forward) <= 0f) return;

        // Ángulo entre hacia dónde apunta el coche y hacia dónde se mueve
        float angulo = Vector3.SignedAngle(transform.forward, velocidadPlano, transform.up);
        angulo = Mathf.Clamp(angulo, -_anguloMaximoAsistencia, _anguloMaximoAsistencia);

        // Torque que gira el morro hacia la trayectoria
        _cuerpo.AddTorque(transform.up * (angulo * _fuerzaAlineacion), ForceMode.Acceleration);
    }
}