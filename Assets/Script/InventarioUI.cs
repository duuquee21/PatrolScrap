using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventarioUI : MonoBehaviour
{
    [Header("Origen de datos")]
    [SerializeField] private InventarioCoche _inventario;   // arrastra Patrol

    [Header("Posición y tamaño")]
    [SerializeField] private Vector2 _margen = new Vector2(30f, 30f);
    [SerializeField] private float _anchoBarra = 360f;
    [SerializeField] private float _altoBarra = 28f;
    [SerializeField] private float _tamanoCasilla = 76f;
    [SerializeField] private float _separacion = 8f;

    [Header("Colores")]
    [SerializeField] private Color _colorFondo = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _colorBarra = new Color(0.35f, 0.8f, 0.4f);
    [SerializeField] private Color _colorBarraLlena = new Color(0.9f, 0.3f, 0.25f);

    [Header("Texto")]
    [SerializeField] private int _tamanoTexto = 22;

    [Header("Pulso al recoger")]
    [SerializeField] private float _fuerzaPulso = 0.3f;     // 0.3 = la casilla crece un 30 %
    [SerializeField] private float _velocidadPulso = 4f;    // cuanto más alto, antes vuelve

    private class Casilla
    {
        public RectTransform Raiz;
        public Text Cantidad;
        public int UltimaCantidad;
        public float Pulso;   // 1 = recién recogido, 0 = en reposo
    }

    private readonly Dictionary<MaterialChatarra, Casilla> _casillas = new Dictionary<MaterialChatarra, Casilla>();

    private Font _fuente;
    private RectTransform _contenedorCasillas;
    private Image _rellenoBarra;
    private Text _textoBarra;

    private void Awake()
    {
        _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
    }

    private void OnEnable()
    {
        if (_inventario != null) _inventario.Cambiado += Refrescar;
    }

    private void OnDisable()
    {
        if (_inventario != null) _inventario.Cambiado -= Refrescar;
    }

    private void Start()
    {
        Refrescar();   // estado inicial: barra vacía
    }

    private void Update()
    {
        // Animación del pulso. unscaledDeltaTime: funciona igual aunque pausemos el juego
        foreach (Casilla casilla in _casillas.Values)
        {
            if (casilla.Pulso <= 0f) continue;

            casilla.Pulso = Mathf.MoveTowards(casilla.Pulso, 0f, _velocidadPulso * Time.unscaledDeltaTime);
            casilla.Raiz.localScale = Vector3.one * (1f + _fuerzaPulso * casilla.Pulso);
        }
    }

    // ---------- Actualizar datos ----------

    private void Refrescar()
    {
        // Barra de capacidad
        float fraccion = _inventario.Capacidad > 0
            ? (float)_inventario.EspacioUsado / _inventario.Capacidad
            : 0f;
        bool lleno = fraccion >= 1f;

        _rellenoBarra.rectTransform.anchorMax = new Vector2(fraccion, 1f);
        _rellenoBarra.color = lleno ? _colorBarraLlena : _colorBarra;
        _textoBarra.text = $"{_inventario.EspacioUsado} / {_inventario.Capacidad}" + (lleno ? "  LLENO" : "");

        // Una casilla por material
        foreach (KeyValuePair<MaterialChatarra, int> par in _inventario.Contenido)
        {
            if (!_casillas.TryGetValue(par.Key, out Casilla casilla))
            {
                casilla = CrearCasilla(par.Key);
                _casillas.Add(par.Key, casilla);
            }

            casilla.Cantidad.text = "x" + par.Value;

            if (par.Value > casilla.UltimaCantidad) casilla.Pulso = 1f;
            casilla.UltimaCantidad = par.Value;
        }
    }

    // ---------- Construcción del interfaz ----------

    private void ConstruirInterfaz()
    {
        // Canvas que cubre toda la pantalla y escala con la resolución
        GameObject objetoCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        objetoCanvas.transform.SetParent(transform, false);

        Canvas canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        // Panel anclado arriba a la izquierda
        RectTransform panel = CrearRect("Panel", objetoCanvas.transform);
        AnclarArribaIzquierda(panel, new Vector2(_margen.x, -_margen.y), new Vector2(_anchoBarra, _altoBarra + _tamanoCasilla + 12f));

        // Barra de capacidad: fondo, relleno y texto
        Image fondoBarra = CrearImagen("BarraFondo", panel, _colorFondo);
        AnclarArribaIzquierda(fondoBarra.rectTransform, Vector2.zero, new Vector2(_anchoBarra, _altoBarra));

        _rellenoBarra = CrearImagen("BarraRelleno", fondoBarra.transform, _colorBarra);
        Estirar(_rellenoBarra.rectTransform);

        _textoBarra = CrearTexto("BarraTexto", fondoBarra.transform, _tamanoTexto - 4, TextAnchor.MiddleCenter);
        Estirar(_textoBarra.rectTransform);

        // Fila de casillas, colocada con un HorizontalLayoutGroup para que se alineen solas
        _contenedorCasillas = CrearRect("Casillas", panel);
        AnclarArribaIzquierda(_contenedorCasillas, new Vector2(0f, -(_altoBarra + 12f)), new Vector2(_anchoBarra, _tamanoCasilla));

        HorizontalLayoutGroup fila = _contenedorCasillas.gameObject.AddComponent<HorizontalLayoutGroup>();
        fila.spacing = _separacion;
        fila.childAlignment = TextAnchor.UpperLeft;
        fila.childControlWidth = false;
        fila.childControlHeight = false;
        fila.childForceExpandWidth = false;
        fila.childForceExpandHeight = false;
    }

    private Casilla CrearCasilla(MaterialChatarra tipo)
    {
        // Fondo oscuro (el marco)
        Image fondo = CrearImagen("Casilla_" + tipo.Nombre, _contenedorCasillas, _colorFondo);
        fondo.rectTransform.sizeDelta = new Vector2(_tamanoCasilla, _tamanoCasilla);

        // Muestra del color del material, con un margen de 6 para que quede el marco
        Image muestra = CrearImagen("Color", fondo.transform, tipo.Color);
        RectTransform rectMuestra = muestra.rectTransform;
        rectMuestra.anchorMin = Vector2.zero;
        rectMuestra.anchorMax = Vector2.one;
        rectMuestra.offsetMin = new Vector2(6f, 6f);
        rectMuestra.offsetMax = new Vector2(-6f, -6f);

        // Si el material tiene icono, se dibuja encima del color
        if (tipo.Icono != null)
        {
            Image icono = CrearImagen("Icono", muestra.transform, Color.white);
            icono.sprite = tipo.Icono;
            icono.preserveAspect = true;
            Estirar(icono.rectTransform);
        }

        // Nombre arriba y cantidad abajo a la derecha
        Text nombre = CrearTexto("Nombre", fondo.transform, 14, TextAnchor.UpperCenter);
        nombre.text = tipo.Nombre;
        Estirar(nombre.rectTransform);

        Text cantidad = CrearTexto("Cantidad", fondo.transform, _tamanoTexto + 4, TextAnchor.LowerRight);
        Estirar(cantidad.rectTransform);
        cantidad.rectTransform.offsetMax = new Vector2(-6f, 0f);
        cantidad.rectTransform.offsetMin = new Vector2(0f, 2f);

        return new Casilla { Raiz = fondo.rectTransform, Cantidad = cantidad };
    }

    // ---------- Utilidades para crear elementos ----------

    private RectTransform CrearRect(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return objeto.GetComponent<RectTransform>();
    }

    private Image CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;   // es solo visual: no necesita recibir clics
        return imagen;
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

    private void AnclarArribaIzquierda(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
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