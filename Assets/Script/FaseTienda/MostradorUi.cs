using UnityEngine;
using UnityEngine.UI;

public class MostradorUI : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private GeneradorClientes _generador;

    [Header("Aspecto")]
    [SerializeField] private string _simbolo = "$";
    [SerializeField] private int _tamanoTexto = 34;
    [SerializeField] private Color _colorTelon = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color _colorFondoRetrato = new Color(0.16f, 0.2f, 0.24f);
    [SerializeField] private Color _colorPapel = new Color(0.95f, 0.92f, 0.84f);
    [SerializeField] private Color _colorTinta = new Color(0.14f, 0.13f, 0.12f);
    [SerializeField] private Color _colorTintaSuave = new Color(0.45f, 0.42f, 0.38f);
    [SerializeField] private Color _colorOferta = new Color(0.1f, 0.45f, 0.2f);

    [Header("Animación")]
    [SerializeField] private float _duracionApertura = 0.35f;   // s
    [SerializeField] private float _entradaRetrato = 140f;      // px que entra desde la izquierda
    [SerializeField] private float _letrasPorSegundo = 60f;     // efecto máquina de escribir
    [SerializeField] private float _fuerzaPulsoOferta = 0.2f;   // 0.2 = el número crece un 20 % al cambiar
    [SerializeField] private float _velocidadPulsoOferta = 4f;

    [Header("Gestos (según lo nervioso que es el tipo)")]
    [SerializeField] private Vector2 _rangoPerdida = new Vector2(8f, 25f);       // pérdida por ronda: (calmado, nervioso)
    [SerializeField] private Vector2 _velocidadGesto = new Vector2(0.6f, 3.5f);  // balanceos por segundo
    [SerializeField] private Vector2 _amplitudGesto = new Vector2(2f, 9f);       // px

    [Header("Pruebas")]
    [SerializeField] private TipoCliente[] _tiposDePrueba;       // los 4 assets
    [SerializeField] private MaterialChatarra _materialDePrueba; // p. ej. Cobre

    // Medidas del mostrador, en la resolución de referencia 1920 x 1080
    private const float AnchoContenedor = 1360f;
    private const float AltoContenedor = 560f;

    private Font _fuente;
    private Canvas _canvas;
    private CanvasGroup _grupo;

    private RectTransform _rectRetrato;
    private Vector2 _posRetrato;
    private RectTransform _figura;
    private Image _imagenRetrato;
    private Image _cabeza, _hombros, _ojoIzquierdo, _ojoDerecho;

    private Text _textoDialogo;
    private Text _valorPieza, _valorCantidad, _valorReferencia, _valorOferta;
    private RectTransform _rectOferta;

    private Regateo _regateo;
    private bool _abierto;
    private float _apertura;
    private string _frase = "";
    private float _letras;
    private float _fase;
    private float _nervios;
    private float _pulsoOferta;
    private int _indicePrueba;

    public bool Abierto => _abierto;
    public Regateo RegateoActual => _regateo;

    private void Awake()
    {
        if (_generador == null) _generador = FindFirstObjectByType<GeneradorClientes>();

        _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
        _canvas.enabled = false;   // cerrado: no se dibuja nada
    }

    // ---------- API pública (la usarán los botones en el paso 4b) ----------

    public void Mostrar(Regateo regateo)
    {
        _regateo = regateo;
        Cliente cliente = regateo.Cliente;

        PintarRetrato(cliente.Tipo);
        _nervios = Mathf.Clamp01(Mathf.InverseLerp(_rangoPerdida.x, _rangoPerdida.y, cliente.Tipo.PerdidaPorRonda));

        _valorPieza.text = cliente.Material.Nombre;
        _valorCantidad.text = "x" + cliente.Cantidad;
        _valorReferencia.text = cliente.PrecioReferencia + " " + _simbolo;
        _valorOferta.text = cliente.OfertaActual + " " + _simbolo;
        _pulsoOferta = 0f;

        Decir(regateo.DecirSaludo() + " " + regateo.DecirOferta());
        _abierto = true;
    }

    // Pone una frase nueva en el globo, con efecto de máquina de escribir
    public void Decir(string frase)
    {
        _frase = frase ?? "";
        _letras = 0f;
        _textoDialogo.text = "";
    }

    // Refresca la oferta de la hoja de pedido (con un pequeño pulso si ha cambiado)
    public void ActualizarOferta()
    {
        if (_regateo == null) return;

        string nueva = _regateo.Cliente.OfertaActual + " " + _simbolo;
        if (_valorOferta.text != nueva) _pulsoOferta = 1f;
        _valorOferta.text = nueva;
    }

    public void Ocultar()
    {
        _abierto = false;
    }

    // ---------- Animación ----------

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Apertura y cierre: 0 = cerrado, 1 = abierto
        float objetivo = _abierto ? 1f : 0f;
        _apertura = Mathf.MoveTowards(_apertura, objetivo, dt / Mathf.Max(0.01f, _duracionApertura));

        _canvas.enabled = _apertura > 0.001f;
        if (!_canvas.enabled) return;

        float suave = _apertura * _apertura * (3f - 2f * _apertura);   // smoothstep: arranca y frena suave
        _grupo.alpha = suave;
        _rectRetrato.anchoredPosition = _posRetrato + new Vector2(-_entradaRetrato * (1f - suave), 0f);

        // Gesto: el cliente se balancea. Más nervioso = más rápido y más amplio
        if (_abierto)
        {
            float velocidad = Mathf.Lerp(_velocidadGesto.x, _velocidadGesto.y, _nervios);
            float amplitud = Mathf.Lerp(_amplitudGesto.x, _amplitudGesto.y, _nervios);

            _fase += dt * velocidad * Mathf.PI * 2f;
            _figura.anchoredPosition = new Vector2(0f, Mathf.Sin(_fase) * amplitud);
        }

        // Máquina de escribir
        if (_letras < _frase.Length)
        {
            _letras = Mathf.Min(_frase.Length, _letras + _letrasPorSegundo * dt);
            _textoDialogo.text = _frase.Substring(0, Mathf.FloorToInt(_letras));
        }

        // Pulso del número de la oferta
        if (_pulsoOferta > 0f)
        {
            _pulsoOferta = Mathf.MoveTowards(_pulsoOferta, 0f, _velocidadPulsoOferta * dt);
            _rectOferta.localScale = Vector3.one * (1f + _fuerzaPulsoOferta * _pulsoOferta);
        }
    }

    private void PintarRetrato(TipoCliente tipo)
    {
        bool hayRetrato = tipo.Retrato != null;

        _imagenRetrato.gameObject.SetActive(hayRetrato);
        _hombros.gameObject.SetActive(!hayRetrato);
        _cabeza.gameObject.SetActive(!hayRetrato);
        _ojoIzquierdo.gameObject.SetActive(!hayRetrato);
        _ojoDerecho.gameObject.SetActive(!hayRetrato);

        if (hayRetrato)
        {
            _imagenRetrato.sprite = tipo.Retrato;
        }
        else
        {
            // Silueta provisional: la cabeza del color del tipo y los hombros algo más oscuros
            _cabeza.color = tipo.Color;
            _hombros.color = Color.Lerp(tipo.Color, Color.black, 0.3f);
        }
    }

    // ---------- Construcción ----------

    private void ConstruirInterfaz()
    {
        GameObject objetoCanvas = new GameObject("CanvasMostrador", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        objetoCanvas.transform.SetParent(transform, false);

        _canvas = objetoCanvas.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;   // por encima del HUD y por debajo de la pantalla de fin de salida (100)

        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        _grupo = objetoCanvas.GetComponent<CanvasGroup>();
        _grupo.interactable = false;      // en el paso 4b esto cambiará
        _grupo.blocksRaycasts = false;
        _grupo.alpha = 0f;

        // Telón oscuro a pantalla completa: centra la atención en el mostrador
        Image telon = Imagen("Telon", objetoCanvas.transform, null, _colorTelon);
        Estirar(telon.rectTransform);

        // Contenedor centrado que agrupa todo el mostrador
        RectTransform contenedor = Rect("Contenedor", objetoCanvas.transform);
        contenedor.anchorMin = new Vector2(0.5f, 0.5f);
        contenedor.anchorMax = new Vector2(0.5f, 0.5f);
        contenedor.pivot = new Vector2(0.5f, 0.5f);
        contenedor.sizeDelta = new Vector2(AnchoContenedor, AltoContenedor);
        contenedor.anchoredPosition = Vector2.zero;

        ConstruirRetrato(contenedor);
        ConstruirGlobo(contenedor);
        ConstruirHojaDePedido(contenedor);
    }

    private void ConstruirRetrato(RectTransform contenedor)
    {
        Image marco = Panel("Retrato", contenedor, 0f, 0f, 360f, 560f, _colorFondoRetrato);
        _rectRetrato = marco.rectTransform;
        _posRetrato = _rectRetrato.anchoredPosition;
        marco.gameObject.AddComponent<RectMask2D>();   // recorta lo que se sale del marco (los hombros)

        _figura = Rect("Figura", marco.transform);
        Estirar(_figura);

        // Imagen propia del cliente, si el asset tiene un retrato
        _imagenRetrato = Imagen("Sprite", _figura, null, Color.white);
        _imagenRetrato.preserveAspect = true;
        Estirar(_imagenRetrato.rectTransform);

        // Silueta provisional: hombros (elipse que se sale por abajo), cabeza y ojos
        _hombros = Imagen("Hombros", _figura, UiSprites.Circulo, Color.gray);
        Colocar(_hombros.rectTransform, 20f, 310f, 320f, 300f, new Vector2(0f, 1f));

        _cabeza = Imagen("Cabeza", _figura, UiSprites.Circulo, Color.gray);
        Colocar(_cabeza.rectTransform, 100f, 160f, 160f, 160f, new Vector2(0f, 1f));

        Color ojo = new Color(0.12f, 0.12f, 0.14f);
        _ojoIzquierdo = Imagen("OjoIzquierdo", _figura, UiSprites.Circulo, ojo);
        Colocar(_ojoIzquierdo.rectTransform, 136f, 232f, 20f, 20f, new Vector2(0f, 1f));

        _ojoDerecho = Imagen("OjoDerecho", _figura, UiSprites.Circulo, ojo);
        Colocar(_ojoDerecho.rectTransform, 204f, 232f, 20f, 20f, new Vector2(0f, 1f));
    }

    private void ConstruirGlobo(RectTransform contenedor)
    {
        Image globo = Panel("Globo", contenedor, 400f, 0f, 960f, 250f, _colorPapel);

        // Pico del globo, apuntando al retrato: un cuadrado girado 45 grados
        Image pico = Imagen("Pico", globo.transform, UiSprites.Redondeado, _colorPapel);
        pico.type = Image.Type.Sliced;
        Colocar(pico.rectTransform, -16f, 106f, 36f, 36f, new Vector2(0f, 1f));
        pico.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        _textoDialogo = Texto("Dialogo", globo.transform, _tamanoTexto, TextAnchor.UpperLeft, _colorTinta, FontStyle.Normal);
        RectTransform rect = _textoDialogo.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(40f, 30f);
        rect.offsetMax = new Vector2(-40f, -30f);
        _textoDialogo.lineSpacing = 1.05f;
    }

    private void ConstruirHojaDePedido(RectTransform contenedor)
    {
        Image hoja = Panel("HojaDePedido", contenedor, 400f, 280f, 960f, 280f, _colorPapel);

        Text titulo = Texto("Titulo", hoja.transform, 26, TextAnchor.MiddleLeft, _colorTintaSuave, FontStyle.Bold);
        titulo.text = "HOJA DE PEDIDO";
        titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
        Colocar(titulo.rectTransform, 40f, 18f, 600f, 44f, new Vector2(0f, 1f));

        Color lineaColor = _colorTintaSuave;
        lineaColor.a = 0.5f;
        Image linea = Imagen("Linea", hoja.transform, null, lineaColor);
        Colocar(linea.rectTransform, 40f, 66f, 880f, 3f, new Vector2(0f, 1f));

        string[] etiquetas = { "Pieza", "Cantidad", "Precio de referencia", "Oferta del cliente" };

        for (int i = 0; i < etiquetas.Length; i++)
        {
            float y = 84f + i * 46f;
            bool esOferta = i == etiquetas.Length - 1;

            Text etiqueta = Texto("Etiqueta" + i, hoja.transform, _tamanoTexto - 2, TextAnchor.MiddleLeft, _colorTintaSuave, FontStyle.Normal);
            etiqueta.text = etiquetas[i];
            etiqueta.horizontalOverflow = HorizontalWrapMode.Overflow;
            Colocar(etiqueta.rectTransform, 40f, y, 500f, 44f, new Vector2(0f, 1f));

            // El valor está alineado a la derecha y con el pivote a la derecha: al pulsar, crece hacia la izquierda
            Text valor = Texto("Valor" + i, hoja.transform, esOferta ? _tamanoTexto + 4 : _tamanoTexto,
                TextAnchor.MiddleRight, esOferta ? _colorOferta : _colorTinta, FontStyle.Bold);
            valor.horizontalOverflow = HorizontalWrapMode.Overflow;
            Colocar(valor.rectTransform, 480f, y, 440f, 44f, new Vector2(1f, 0.5f));

            switch (i)
            {
                case 0: _valorPieza = valor; break;
                case 1: _valorCantidad = valor; break;
                case 2: _valorReferencia = valor; break;
                default: _valorOferta = valor; _rectOferta = valor.rectTransform; break;
            }
        }
    }

    // ---------- Utilidades para crear elementos ----------

    private RectTransform Rect(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return objeto.GetComponent<RectTransform>();
    }

    private Image Imagen(string nombre, Transform padre, Sprite sprite, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);

        Image imagen = objeto.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private Image Panel(string nombre, Transform padre, float x, float y, float ancho, float alto, Color color)
    {
        Image panel = Imagen(nombre, padre, UiSprites.Redondeado, color);
        panel.type = Image.Type.Sliced;
        Colocar(panel.rectTransform, x, y, ancho, alto, new Vector2(0f, 1f));
        return panel;
    }

    private Text Texto(string nombre, Transform padre, int tamano, TextAnchor alineacion, Color color, FontStyle estilo)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.GetComponent<Text>();
        texto.font = _fuente;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = color;
        texto.fontStyle = estilo;
        texto.raycastTarget = false;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
        return texto;
    }

    // Coloca un elemento por su esquina superior izquierda, medida desde la esquina superior izquierda del padre.
    // El pivote decide desde dónde crece al escalarlo
    private void Colocar(RectTransform rect, float x, float y, float ancho, float alto, Vector2 pivote)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = pivote;
        rect.sizeDelta = new Vector2(ancho, alto);
        rect.anchoredPosition = new Vector2(x + pivote.x * ancho, -(y + (1f - pivote.y) * alto));
    }

    private void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ---------- Pruebas: clic derecho en el título del componente, con el juego en marcha ----------

    [ContextMenu("TEST: llamar a un cliente (al azar, según el stock)")]
    private void ProbarCliente()
    {
        if (!Application.isPlaying) return;

        Cliente cliente = _generador != null ? _generador.Generar() : null;
        if (cliente == null)
        {
            Debug.Log("No hay cliente: la tienda está vacía. Llena el stock de prueba en GeneradorClientes.");
            return;
        }

        Mostrar(new Regateo(cliente));
    }

    [ContextMenu("TEST: llamar al siguiente tipo de cliente (Tipos De Prueba)")]
    private void ProbarSiguienteTipo()
    {
        if (!Application.isPlaying) return;
        if (_tiposDePrueba == null || _tiposDePrueba.Length == 0 || _materialDePrueba == null)
        {
            Debug.LogWarning("Asigna los Tipos De Prueba y el Material De Prueba.");
            return;
        }

        TipoCliente tipo = _tiposDePrueba[_indicePrueba % _tiposDePrueba.Length];
        _indicePrueba++;

        Mostrar(new Regateo(new Cliente(tipo, _materialDePrueba, 3)));
        Debug.Log("(depuración) Este cliente es de tipo: " + tipo.Nombre);   // el jugador no lo ve
    }

    [ContextMenu("TEST: repetir la frase (máquina de escribir)")]
    private void ProbarRepetirFrase()
    {
        if (Application.isPlaying && _regateo != null) Decir(_regateo.DecirOferta());
    }

    [ContextMenu("TEST: despedir al cliente")]
    private void ProbarDespedir()
    {
        if (Application.isPlaying) Ocultar();
    }
}