using UnityEngine;
using UnityEngine.UI;

public class GasolinaUI : MonoBehaviour
{
    [Header("Origen de datos")]
    [SerializeField] private GasolinaCoche _gasolina;   // arrastra Patrol
    [SerializeField] private MotorCoche _motor;         // arrastra Patrol (para la barra de turbo)

    [Header("Posición y tamaño")]
    [SerializeField] private Vector2 _margen = new Vector2(30f, 30f);
    [SerializeField] private float _ancho = 360f;
    [SerializeField] private float _alto = 30f;
    [SerializeField] private float _altoTurbo = 10f;
    [SerializeField] private int _tamanoTexto = 18;

    [Header("Colores")]
    [SerializeField] private Color _colorFondo = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _colorLleno = new Color(0.35f, 0.8f, 0.4f);
    [SerializeField] private Color _colorMedio = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color _colorVacio = new Color(0.9f, 0.3f, 0.25f);
    [SerializeField] private Color _colorTurbo = new Color(0.3f, 0.7f, 1f);
    [SerializeField] private float _velocidadParpadeo = 4f;

    private Font _fuente;
    private Image _relleno;
    private Text _texto;
    private Image _rellenoTurbo;

    private void Awake()
    {
        _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
    }

    // La gasolina cambia en cada paso de física, así que aquí sí se lee cada frame.
    // (El inventario cambia a golpes, por eso usaba un evento.)
    private void Update()
    {
        float nivel = _gasolina.Nivel01;

        _relleno.rectTransform.anchorMax = new Vector2(nivel, 1f);

        // Verde -> amarillo -> rojo según el nivel
        Color color = nivel > 0.5f
            ? Color.Lerp(_colorMedio, _colorLleno, (nivel - 0.5f) / 0.5f)
            : Color.Lerp(_colorVacio, _colorMedio, nivel / 0.5f);

        // En reserva, parpadea hacia blanco
        if (_gasolina.EnReserva && _gasolina.TieneGasolina)
        {
            float parpadeo = Mathf.PingPong(Time.unscaledTime * _velocidadParpadeo, 1f);
            color = Color.Lerp(color, Color.white, parpadeo * 0.5f);
        }
        _relleno.color = color;

        _texto.text = _gasolina.TieneGasolina
            ? $"GASOLINA  {Mathf.CeilToInt(nivel * 100f)}%"
            : "SIN GASOLINA";

        if (_rellenoTurbo != null)
        {
            _rellenoTurbo.rectTransform.anchorMax = new Vector2(_motor.DepositoTurbo01, 1f);
        }
    }

    // ---------- Construcción ----------

    private void ConstruirInterfaz()
    {
        GameObject objetoCanvas = new GameObject("CanvasGasolina", typeof(Canvas), typeof(CanvasScaler));
        objetoCanvas.transform.SetParent(transform, false);

        Canvas canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        // Barra de turbo (abajo del todo, fina), solo si hay motor
        float yGasolina = _margen.y;
        if (_motor != null)
        {
            Image fondoTurbo = CrearImagen("TurboFondo", objetoCanvas.transform, _colorFondo);
            AnclarAbajoIzquierda(fondoTurbo.rectTransform, new Vector2(_margen.x, _margen.y), new Vector2(_ancho, _altoTurbo));

            _rellenoTurbo = CrearImagen("TurboRelleno", fondoTurbo.transform, _colorTurbo);
            Estirar(_rellenoTurbo.rectTransform);

            yGasolina += _altoTurbo + 6f;
        }

        // Barra de gasolina, encima de la de turbo
        Image fondo = CrearImagen("GasolinaFondo", objetoCanvas.transform, _colorFondo);
        AnclarAbajoIzquierda(fondo.rectTransform, new Vector2(_margen.x, yGasolina), new Vector2(_ancho, _alto));

        _relleno = CrearImagen("GasolinaRelleno", fondo.transform, _colorLleno);
        Estirar(_relleno.rectTransform);

        _texto = CrearTexto("GasolinaTexto", fondo.transform);
        Estirar(_texto.rectTransform);
    }

    private Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private Text CrearTexto(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text), typeof(Outline));
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.GetComponent<Text>();
        texto.font = _fuente;
        texto.fontSize = _tamanoTexto;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;

        Outline contorno = objeto.GetComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);
        return texto;
    }

    private void AnclarAbajoIzquierda(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }

    private void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}