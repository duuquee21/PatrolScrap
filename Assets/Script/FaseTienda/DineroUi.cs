using UnityEngine;
using UnityEngine.UI;

public class DineroUI : MonoBehaviour
{
    [Header("Origen de datos (si se deja vacío, se busca solo)")]
    [SerializeField] private Cartera _cartera;

    [Header("Aspecto")]
    [SerializeField] private string _simbolo = "$";
    [SerializeField] private float _margenSuperior = 30f;
    [SerializeField] private Vector2 _tamanoPanel = new Vector2(260f, 64f);
    [SerializeField] private int _tamanoTexto = 36;
    [SerializeField] private Color _colorFondo = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _colorGanancia = new Color(0.45f, 1f, 0.5f);
    [SerializeField] private Color _colorPerdida = new Color(1f, 0.4f, 0.35f);

    [Header("Animación (en segundos)")]
    [SerializeField] private float _duracionContador = 0.9f;   // lo que tarda el número en llegar al valor nuevo
    [SerializeField] private float _duracionPulso = 0.7f;      // lo que dura el color y el "latido" del panel
    [SerializeField] private float _fuerzaPulso = 0.25f;       // 0.25 = el panel crece un 25 %
    [SerializeField] private float _duracionVariacion = 1.6f;  // lo que se ve el "+50"
    [SerializeField] private float _subidaVariacion = 50f;     // px que sube

    private Font _fuente;
    private RectTransform _panel;
    private Image _fondo;
    private Text _textoDinero;
    private Text _textoVariacion;
    private RectTransform _rectVariacion;

    private int _ultimoConocido;    // para detectar cambios aunque el evento no llegue
    private float _mostrado;        // lo que se ve en pantalla
    private float _desde, _hasta;   // tramo que recorre el contador
    private float _progresoContador = 1f;   // 0..1; 1 = terminado
    private float _pulso;           // 1 = acaba de cambiar, 0 = en reposo
    private Color _colorPulso = Color.white;
    private float _tiempoVariacion;
    private bool _listo;

    private void Awake()
    {
        if (_cartera == null) _cartera = FindFirstObjectByType<Cartera>();
        if (_cartera == null)
        {
            Debug.LogError("DineroUI: no hay ninguna Cartera en la escena.", this);
            enabled = false;
            return;
        }

        _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
        _listo = true;
    }

    private void OnEnable()
    {
        if (_listo) _cartera.Cambiado += AlCambiar;
    }

    private void OnDisable()
    {
        if (_listo) _cartera.Cambiado -= AlCambiar;
    }

    private void Start()
    {
        // Nos aseguramos de estar suscritos una sola vez
        _cartera.Cambiado -= AlCambiar;
        _cartera.Cambiado += AlCambiar;

        _ultimoConocido = _cartera.Dinero;
        _mostrado = _ultimoConocido;
        PintarNumero();
        _rectVariacion.gameObject.SetActive(false);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Red de seguridad: si el dinero ha cambiado y el evento no llegó, reaccionamos igual
        if (_cartera.Dinero != _ultimoConocido)
        {
            Reaccionar(_cartera.Dinero - _ultimoConocido);
            _ultimoConocido = _cartera.Dinero;
        }

        // 1) El número viaja de _desde a _hasta con una curva que frena al llegar
        if (_progresoContador < 1f)
        {
            _progresoContador = Mathf.Min(1f, _progresoContador + dt / _duracionContador);
            float suave = 1f - Mathf.Pow(1f - _progresoContador, 3f);
            _mostrado = Mathf.Lerp(_desde, _hasta, suave);
            PintarNumero();
        }

        // 2) Pulso: el panel late, el texto y el fondo se tiñen y vuelven a su color
        if (_pulso > 0f)
        {
            _pulso = Mathf.MoveTowards(_pulso, 0f, dt / _duracionPulso);

            _panel.localScale = Vector3.one * (1f + _fuerzaPulso * _pulso);
            _textoDinero.color = Color.Lerp(Color.white, _colorPulso, _pulso);

            Color fondoTenido = new Color(_colorPulso.r * 0.5f, _colorPulso.g * 0.5f, _colorPulso.b * 0.5f, 0.9f);
            _fondo.color = Color.Lerp(_colorFondo, fondoTenido, _pulso);
        }

        // 3) Variación flotante: sube y se desvanece
        if (_tiempoVariacion > 0f)
        {
            _tiempoVariacion -= dt;
            float t = 1f - Mathf.Clamp01(_tiempoVariacion / _duracionVariacion);   // 0 al aparecer, 1 al desaparecer

            _rectVariacion.anchoredPosition = new Vector2(0f, -8f + _subidaVariacion * t);

            Color c = _textoVariacion.color;
            c.a = 1f - t * t;   // se mantiene visible y se apaga al final
            _textoVariacion.color = c;

            if (_tiempoVariacion <= 0f) _rectVariacion.gameObject.SetActive(false);
        }
    }

    private void AlCambiar(int total, int variacion)
    {
        _ultimoConocido = total;   // así la red de seguridad no reacciona dos veces
        Reaccionar(variacion);
    }

    private void Reaccionar(int variacion)
    {
        Debug.Log("DineroUI: reacciona a una variación de " + variacion);   // TEMPORAL

        // El contador arranca desde lo que se ve ahora, aunque haya otro cambio en curso
        _desde = _mostrado;
        _hasta = _cartera.Dinero;
        _progresoContador = 0f;

        _pulso = 1f;
        _colorPulso = variacion >= 0 ? _colorGanancia : _colorPerdida;

        if (variacion == 0) return;

        _textoVariacion.text = (variacion > 0 ? "+" : "") + variacion + " " + _simbolo;
        _textoVariacion.color = _colorPulso;
        _tiempoVariacion = _duracionVariacion;
        _rectVariacion.gameObject.SetActive(true);
        _rectVariacion.anchoredPosition = new Vector2(0f, -8f);
    }

    private void PintarNumero()
    {
        _textoDinero.text = Mathf.RoundToInt(_mostrado) + " " + _simbolo;
    }

    // ---------- Construcción ----------

    private void ConstruirInterfaz()
    {
        GameObject objetoCanvas = new GameObject("CanvasDinero", typeof(Canvas), typeof(CanvasScaler));
        objetoCanvas.transform.SetParent(transform, false);

        Canvas canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        // Panel arriba en el centro
        GameObject objetoPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        objetoPanel.transform.SetParent(objetoCanvas.transform, false);

        _panel = objetoPanel.GetComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.5f, 1f);
        _panel.anchorMax = new Vector2(0.5f, 1f);
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.anchoredPosition = new Vector2(0f, -_margenSuperior);
        _panel.sizeDelta = _tamanoPanel;

        _fondo = objetoPanel.GetComponent<Image>();
        _fondo.color = _colorFondo;
        _fondo.raycastTarget = false;

        // Cantidad, ocupando todo el panel
        _textoDinero = CrearTexto("Dinero", _panel, _tamanoTexto, TextAnchor.MiddleCenter);
        Estirar(_textoDinero.rectTransform);

        // Variación flotante, justo debajo del panel
        _textoVariacion = CrearTexto("Variacion", _panel, _tamanoTexto - 8, TextAnchor.UpperCenter);
        _rectVariacion = _textoVariacion.rectTransform;
        _rectVariacion.anchorMin = new Vector2(0.5f, 0f);
        _rectVariacion.anchorMax = new Vector2(0.5f, 0f);
        _rectVariacion.pivot = new Vector2(0.5f, 1f);
        _rectVariacion.sizeDelta = new Vector2(_tamanoPanel.x, 40f);
    }

    private Text CrearTexto(string nombre, Transform padre, int tamano, TextAnchor alineacion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text), typeof(Outline));
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.GetComponent<Text>();
        texto.font = _fuente;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;

        Outline contorno = objeto.GetComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);
        return texto;
    }

    private void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}