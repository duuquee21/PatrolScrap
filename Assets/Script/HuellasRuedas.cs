using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody), typeof(SuspensionCoche), typeof(EntradaCoche))]
[RequireComponent(typeof(MotorCoche))]
public class HuellasRuedas : MonoBehaviour
{
    [Header("Huellas")]
    [SerializeField] private float _anchoHuella = 0.45f;                 // m
    [SerializeField] private float _duracionHuella = 6f;                 // s hasta que desaparece
    [SerializeField] private float _distanciaEntrePuntos = 0.5f;         // m: más alto = menos puntos = más barato
    [SerializeField] private float _alturaSobreSuelo = 0.08f;            // m: evita que la huella parpadee contra el suelo
    [SerializeField] private float _velocidadMinima = 1.5f;              // m/s: parado no deja huella
    [SerializeField] private float _toleranciaSinContacto = 0.12f;       // s en el aire antes de cortar la huella
    [SerializeField] private float _saltoMaximo = 5f;                    // m por frame: más que esto es un teletransporte
    [SerializeField] private Color _colorHuella = new Color(0.55f, 0.4f, 0.22f, 0.55f);   // arena más oscura que el terreno
    [SerializeField] private int _cintasIniciales = 10;

    [Header("Polvo")]
    [SerializeField] private int _maximoParticulasPorRueda = 40;
    [SerializeField] private float _particulasPorSegundo = 35f;          // a intensidad máxima
    [SerializeField] private float _tamanoParticula = 0.35f;             // m
    [SerializeField] private float _vidaParticula = 0.9f;                // s
    [SerializeField] private Color _colorPolvoA = new Color(0.93f, 0.8f, 0.55f, 0.6f);
    [SerializeField] private Color _colorPolvoB = new Color(0.82f, 0.66f, 0.42f, 0.6f);
    [SerializeField] private float _velocidadPolvoMax = 22f;             // m/s a los que el polvo por velocidad es máximo
    [SerializeField] private float _deslizamientoPolvoMax = 6f;          // m/s de deslizamiento lateral que da el polvo máximo

    private struct CintaSuelta
    {
        public TrailRenderer Cinta;
        public float Hasta;          // momento en que ya está vacía y se puede reutilizar
    }

    private class MarcaRueda
    {
        public TrailRenderer Cinta;                    // la huella que se dibuja ahora (null = la rueda no deja huella)
        public float TiempoSinContacto;
        public ParticleSystem Polvo;
        public ParticleSystem.EmissionModule Emision;
    }

    private Rigidbody _cuerpo;
    private SuspensionCoche _suspension;
    private EntradaCoche _entrada;
    private MotorCoche _motor;

    private Transform _raiz;
    private Material _material;
    private Gradient _gradienteHuella;

    private readonly List<MarcaRueda> _marcas = new List<MarcaRueda>();
    private readonly Stack<TrailRenderer> _libres = new Stack<TrailRenderer>();
    private readonly Queue<CintaSuelta> _enfriando = new Queue<CintaSuelta>();

    private void Awake()
    {
        _cuerpo = GetComponent<Rigidbody>();
        _suspension = GetComponent<SuspensionCoche>();
        _entrada = GetComponent<EntradaCoche>();
        _motor = GetComponent<MotorCoche>();
    }

    private void Start()
    {
        // Sprites/Default pinta el color tal cual y no depende de luces ni del pipeline
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("HuellasRuedas: no encuentro el shader Sprites/Default.", this);
            enabled = false;
            return;
        }

        _material = new Material(shader);
        _raiz = new GameObject("Efectos de ruedas").transform;   // fuera del coche: los efectos se quedan en el mundo

        // Degradado de la huella: opaca la más reciente, transparente la más antigua
        _gradienteHuella = new Gradient();
        _gradienteHuella.SetKeys(
            new[] { new GradientColorKey(_colorHuella, 0f), new GradientColorKey(_colorHuella, 1f) },
            new[] { new GradientAlphaKey(_colorHuella.a, 0f), new GradientAlphaKey(0f, 1f) });

        // Un cubo para las partículas (se saca de una primitiva y se tira la primitiva)
        GameObject auxiliar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubo = auxiliar.GetComponent<MeshFilter>().sharedMesh;
        Destroy(auxiliar);

        for (int i = 0; i < _suspension.Ruedas.Length; i++)
        {
            ParticleSystem polvo = CrearPolvo(cubo);
            _marcas.Add(new MarcaRueda { Polvo = polvo, Emision = polvo.emission });
        }

        for (int i = 0; i < _cintasIniciales; i++) _libres.Push(CrearCinta());
    }

    private void OnDestroy()
    {
        if (_raiz != null) Destroy(_raiz.gameObject);
        if (_material != null) Destroy(_material);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        LiberarCintasViejas();

        float radio = _suspension.RadioRueda;
        DatosRueda[] ruedas = _suspension.Ruedas;

        for (int i = 0; i < ruedas.Length; i++)
        {
            DatosRueda rueda = ruedas[i];
            MarcaRueda marca = _marcas[i];

            if (!rueda.EstaEnSuelo)
            {
                marca.Emision.rateOverTime = 0f;
                CortarSiLlevaMucho(marca, dt);
                continue;
            }

            Vector3 normal = rueda.Contacto.normal;
            Vector3 contacto = rueda.Pivote.position - normal * radio;   // el punto más bajo de la rueda

            // Velocidad del punto de la rueda y cuánto se desliza de lado
            Vector3 velocidadPunto = _cuerpo.GetPointVelocity(rueda.Anclaje.position);
            float velocidad = Vector3.ProjectOnPlane(velocidadPunto, normal).magnitude;
            Vector3 lateral = Vector3.ProjectOnPlane(rueda.Anclaje.right, normal).normalized;
            float deslizamiento = Mathf.Abs(Vector3.Dot(velocidadPunto, lateral));

            ActualizarPolvo(marca, rueda, contacto, normal, velocidad, deslizamiento);
            ActualizarHuella(marca, contacto, normal, velocidad, dt);
        }
    }

    // ---------- Polvo ----------

    private void ActualizarPolvo(MarcaRueda marca, DatosRueda rueda, Vector3 contacto, Vector3 normal, float velocidad, float deslizamiento)
    {
        // Cuatro causas de polvo, sumadas y limitadas a 1
        float intensidad = Mathf.Clamp01(velocidad / _velocidadPolvoMax) * 0.5f;                  // ir deprisa
        intensidad += Mathf.Clamp01(deslizamiento / _deslizamientoPolvoMax);                      // derrapar
        intensidad += Mathf.Abs(_entrada.Acelerador) * (1f - Mathf.Clamp01(velocidad / 8f)) * 0.6f;   // arrancar levanta arena
        if (_entrada.FrenoMano && !rueda.Delantera) intensidad += 0.5f;                           // freno de mano
        if (_motor.TurboActivo) intensidad += 0.3f;                                               // turbo
        intensidad = Mathf.Clamp01(intensidad);

        marca.Polvo.transform.position = contacto + normal * 0.1f;
        marca.Emision.rateOverTime = intensidad * _particulasPorSegundo;
    }

    private ParticleSystem CrearPolvo(Mesh cubo)
    {
        GameObject objeto = new GameObject("Polvo", typeof(ParticleSystem));
        objeto.transform.SetParent(_raiz, false);
        objeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // el cono de partículas sale por su eje Z: así apunta hacia arriba

        ParticleSystem ps = objeto.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // al crearlo ya arranca solo; lo paramos para configurarlo

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;       // el polvo se queda donde se levantó
        main.maxParticles = _maximoParticulasPorRueda;
        main.startLifetime = new ParticleSystem.MinMaxCurve(_vidaParticula * 0.6f, _vidaParticula);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(_tamanoParticula * 0.5f, _tamanoParticula);
        main.startColor = new ParticleSystem.MinMaxGradient(_colorPolvoA, _colorPolvoB);
        main.gravityModifier = 0.3f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emision = ps.emission;
        emision.rateOverTime = 0f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 30f;
        forma.radius = 0.25f;

        // Se desvanece y se encoge a lo largo de su vida
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient desvanecer = new Gradient();
        desvanecer.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = desvanecer;

        var tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var render = objeto.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Mesh;
        render.mesh = cubo;
        render.sharedMaterial = _material;
        render.shadowCastingMode = ShadowCastingMode.Off;
        render.receiveShadows = false;

        ps.Play();
        return ps;
    }

    // ---------- Huellas ----------

    private void ActualizarHuella(MarcaRueda marca, Vector3 contacto, Vector3 normal, float velocidad, float dt)
    {
        if (velocidad <= _velocidadMinima)
        {
            CortarSiLlevaMucho(marca, dt);
            return;
        }

        marca.TiempoSinContacto = 0f;
        Vector3 posicion = contacto + normal * _alturaSobreSuelo;
        Quaternion rotacion = Quaternion.FromToRotation(Vector3.forward, normal);   // la cinta se tumba sobre la superficie

        if (marca.Cinta != null)
        {
            // Si la rueda ha dado un salto enorme en un frame (teletransporte), cortamos la huella para que no cruce el mapa
            float salto2 = (posicion - marca.Cinta.transform.position).sqrMagnitude;
            if (salto2 > _saltoMaximo * _saltoMaximo)
            {
                SoltarCinta(marca.Cinta);
                marca.Cinta = null;
            }
        }

        if (marca.Cinta == null) marca.Cinta = TomarCinta(posicion, rotacion);
        else marca.Cinta.transform.SetPositionAndRotation(posicion, rotacion);
    }

    // La rueda no deja huella ahora: si lleva así un rato, soltamos la cinta
    private void CortarSiLlevaMucho(MarcaRueda marca, float dt)
    {
        if (marca.Cinta == null) return;

        marca.TiempoSinContacto += dt;
        if (marca.TiempoSinContacto >= _toleranciaSinContacto)
        {
            SoltarCinta(marca.Cinta);
            marca.Cinta = null;
        }
    }

    private TrailRenderer TomarCinta(Vector3 posicion, Quaternion rotacion)
    {
        TrailRenderer cinta = _libres.Count > 0 ? _libres.Pop() : CrearCinta();

        cinta.gameObject.SetActive(true);
        cinta.transform.SetPositionAndRotation(posicion, rotacion);
        cinta.Clear();             // sin esto dibujaría una línea desde donde se quedó la última vez
        cinta.emitting = true;
        return cinta;
    }

    private void SoltarCinta(TrailRenderer cinta)
    {
        cinta.emitting = false;    // deja de añadir puntos; lo ya dibujado se va desvaneciendo solo

        _enfriando.Enqueue(new CintaSuelta
        {
            Cinta = cinta,
            Hasta = Time.time + _duracionHuella + 0.25f
        });
    }

    private void LiberarCintasViejas()
    {
        while (_enfriando.Count > 0 && _enfriando.Peek().Hasta <= Time.time)
        {
            TrailRenderer cinta = _enfriando.Dequeue().Cinta;
            cinta.gameObject.SetActive(false);
            _libres.Push(cinta);
        }
    }

    private TrailRenderer CrearCinta()
    {
        GameObject objeto = new GameObject("Huella");
        objeto.transform.SetParent(_raiz, false);

        TrailRenderer cinta = objeto.AddComponent<TrailRenderer>();
        cinta.alignment = LineAlignment.TransformZ;                    // la cinta mira hacia el eje Z del objeto (la normal del suelo)
        cinta.time = _duracionHuella;
        cinta.minVertexDistance = _distanciaEntrePuntos;
        cinta.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        cinta.widthMultiplier = _anchoHuella;
        cinta.numCornerVertices = 0;
        cinta.numCapVertices = 0;
        cinta.colorGradient = _gradienteHuella;
        cinta.sharedMaterial = _material;
        cinta.shadowCastingMode = ShadowCastingMode.Off;
        cinta.receiveShadows = false;
        cinta.emitting = false;

        objeto.SetActive(false);
        return cinta;
    }
}