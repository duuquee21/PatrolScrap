using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MotivoFin { SinGasolina, VueltaABase }

public struct ResumenSalida
{
    public MotivoFin Motivo;
    public int PiezasPerdidas;
    public float FraccionPerdida;
    public List<KeyValuePair<MaterialChatarra, int>> Entregado;   // lo que ha llegado a la tienda
}

public class SalidaManager : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private Rigidbody _coche;              // Patrol
    [SerializeField] private CamaraSprung _camara;          // Main Camera
    [SerializeField] private DescargaBotin _descarga;       // en este mismo objeto (Salida)
    [SerializeField] private TiendaDestino _tienda;         // el cubo de la tienda

    [Header("Parada del coche")]
    [SerializeField] private float _velocidadParada = 1.5f;       // m/s: por debajo se considera parado
    [SerializeField] private float _esperaMaxima = 6f;            // s: tope esperando a que se pare
    [SerializeField] private float _pausaTrasParar = 1.2f;        // s: respiro tras quedarte sin gasolina
    [SerializeField] private float _pausaAntesDeDescargar = 0.3f; // s: respiro tras parar en la base

    [Header("Sin gasolina")]
    [SerializeField, Range(0f, 1f)] private float _fraccionPerdida = 0.4f;

    [Header("Fin de salida")]
    [SerializeField] private float _pausaTrasDescarga = 0.6f;            // s entre el último cubo y el resumen
    [SerializeField] private float _duracionPantalla = 4f;               // s con el resumen en pantalla
    [SerializeField] private float _distanciaParaContarSalida = 40f;     // m: hasta que te alejas, la base no cuenta

    private enum Estado { Explorando, Descargando, Terminada }

    private Estado _estado = Estado.Explorando;
    private bool _haSalido;
    private bool _esperandoParada;

    private GasolinaCoche _gasolina;
    private InventarioCoche _inventario;
    private EntradaCoche _entrada;

    private Vector3 _posicionInicio;
    private Quaternion _rotacionInicio;

    public bool Explorando => _estado == Estado.Explorando;
    public bool Descargando => _estado == Estado.Descargando;
    public bool PuedeTerminarEnBase => _estado == Estado.Explorando && _haSalido;   // lo lee el aro de la base

    public event Action<ResumenSalida> SalidaTerminada;
    public event Action NuevaSalida;

    private void Awake()
    {
        if (_coche == null)
        {
            MotorCoche motor = FindFirstObjectByType<MotorCoche>();
            if (motor != null) _coche = motor.GetComponent<Rigidbody>();
        }
        if (_camara == null) _camara = FindFirstObjectByType<CamaraSprung>();
        if (_descarga == null) _descarga = GetComponent<DescargaBotin>();
        if (_descarga == null) _descarga = FindFirstObjectByType<DescargaBotin>();
        if (_tienda == null) _tienda = FindFirstObjectByType<TiendaDestino>();

        if (_coche == null)
        {
            Debug.LogError("SalidaManager: no encuentro el coche. Arrastra Patrol al campo Coche.", this);
            enabled = false;
            return;
        }

        _gasolina = _coche.GetComponent<GasolinaCoche>();
        _inventario = _coche.GetComponent<InventarioCoche>();
        _entrada = _coche.GetComponent<EntradaCoche>();

        if (_gasolina == null) Debug.LogError("SalidaManager: Patrol no tiene GasolinaCoche.", this);
        if (_inventario == null) Debug.LogError("SalidaManager: Patrol no tiene InventarioCoche.", this);
        if (_entrada == null) Debug.LogError("SalidaManager: Patrol no tiene EntradaCoche.", this);
        if (_camara == null) Debug.LogWarning("SalidaManager: no hay CamaraSprung; la cámara no se recolocará.", this);
        if (_tienda == null) Debug.LogWarning("SalidaManager: no hay TiendaDestino en la escena; el botín no se guardará.", this);
        if (_descarga == null) Debug.LogWarning("SalidaManager: no hay DescargaBotin; no habrá animación de descarga.", this);

        // El punto de inicio es donde has colocado el coche en la escena
        _posicionInicio = _coche.position;
        _rotacionInicio = _coche.rotation;
    }

    private void OnEnable()
    {
        if (_gasolina != null) _gasolina.SeAgoto += AlAgotarse;
    }

    private void OnDisable()
    {
        if (_gasolina != null) _gasolina.SeAgoto -= AlAgotarse;
    }

    private void Update()
    {
        // La vuelta a la base solo vale cuando ya te has alejado lo suficiente
        if (_estado != Estado.Explorando || _haSalido) return;

        float distancia2 = (_coche.position - _posicionInicio).sqrMagnitude;
        if (distancia2 > _distanciaParaContarSalida * _distanciaParaContarSalida) _haSalido = true;
    }

    // ---------- Vuelta a la base ----------

    // La llama ZonaBase mientras el coche está dentro del aro
    public void TerminarEnBase()
    {
        if (!PuedeTerminarEnBase) return;
        StartCoroutine(DescargarYTerminar());
    }

    private IEnumerator DescargarYTerminar()
    {
        _estado = Estado.Descargando;
        _entrada.Bloqueada = true;   // el coche se frena solo con el freno motor

        yield return EsperarAQueParen(_pausaAntesDeDescargar);

        List<KeyValuePair<MaterialChatarra, int>> lote = _inventario.Instantanea();

        if (_descarga != null && _tienda != null)
        {
            yield return _descarga.Descargar(lote, _inventario, _coche.transform, _tienda);
        }
        else
        {
            EntregarDeGolpe(lote);
        }

        yield return new WaitForSeconds(_pausaTrasDescarga);
        Terminar(MotivoFin.VueltaABase, lote, 0, 0f);
    }

    // ---------- Sin gasolina ----------

    private void AlAgotarse()
    {
        if (_estado != Estado.Explorando || _esperandoParada) return;
        _esperandoParada = true;
        StartCoroutine(SinGasolinaTrasParar());
    }

    private IEnumerator SinGasolinaTrasParar()
    {
        yield return EsperarAQueParen(_pausaTrasParar);

        _esperandoParada = false;
        if (_estado != Estado.Explorando) yield break;   // por si has llegado a la base rodando

        TerminarSinGasolina();
    }

    private void TerminarSinGasolina()
    {
        _entrada.Bloqueada = true;

        int perdidas = _inventario.PerderFraccion(_fraccionPerdida);

        // Lo que queda lo recoge la grúa y llega a la tienda, sin animación
        List<KeyValuePair<MaterialChatarra, int>> lote = _inventario.Instantanea();
        EntregarDeGolpe(lote);

        Terminar(MotivoFin.SinGasolina, lote, perdidas, _fraccionPerdida);
    }

    // ---------- Común ----------

    private IEnumerator EsperarAQueParen(float pausa)
    {
        // Esperamos a que el coche casi se pare, pero con un tope por si se queda deslizando
        float tiempo = 0f;
        while (_coche.linearVelocity.magnitude > _velocidadParada && tiempo < _esperaMaxima)
        {
            tiempo += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(pausa);
    }

    private void EntregarDeGolpe(List<KeyValuePair<MaterialChatarra, int>> lote)
    {
        if (_tienda != null)
        {
            foreach (KeyValuePair<MaterialChatarra, int> par in lote) _tienda.Recibir(par.Key, par.Value);
        }
        _inventario.VaciarTodo();
    }

    private void Terminar(MotivoFin motivo, List<KeyValuePair<MaterialChatarra, int>> entregado, int perdidas, float fraccion)
    {
        _estado = Estado.Terminada;
        _entrada.Bloqueada = true;

        SalidaTerminada?.Invoke(new ResumenSalida
        {
            Motivo = motivo,
            PiezasPerdidas = perdidas,
            FraccionPerdida = fraccion,
            Entregado = entregado
        });

        StartCoroutine(ReiniciarTrasPausa());
    }

    private IEnumerator ReiniciarTrasPausa()
    {
        yield return new WaitForSeconds(_duracionPantalla);
        Reiniciar();
    }

    private void Reiniciar()
    {
        // 1) Teletransporte: velocidades a cero, mover el Rigidbody y sincronizar el Transform
        _coche.linearVelocity = Vector3.zero;
        _coche.angularVelocity = Vector3.zero;
        _coche.position = _posicionInicio;
        _coche.rotation = _rotacionInicio;
        Physics.SyncTransforms();

        // 2) La cámara se recoloca de golpe
        if (_camara != null) _camara.Recolocar();

        // 3) TEMPORAL: depósito lleno. Cuando exista el dinero, la gasolina se comprará
        _gasolina.RellenarTodo();

        // 4) Nueva salida
        _haSalido = false;
        _entrada.Bloqueada = false;
        _estado = Estado.Explorando;
        NuevaSalida?.Invoke();
    }

    // Para probar sin tener que ir hasta la base o vaciar el depósito:
    // clic derecho en el título del componente, con el juego en marcha
    [ContextMenu("TEST: terminar sin gasolina")]
    private void ProbarSinGasolina()
    {
        if (Application.isPlaying && _estado == Estado.Explorando) TerminarSinGasolina();
    }

    [ContextMenu("TEST: volver a la base")]
    private void ProbarVueltaABase()
    {
        if (Application.isPlaying && _estado == Estado.Explorando) StartCoroutine(DescargarYTerminar());
    }
}