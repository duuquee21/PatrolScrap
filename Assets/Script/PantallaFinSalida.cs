using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PantallaFinSalida : MonoBehaviour
{
    [Header("Referencias (si se deja vacío, se busca solo)")]
    [SerializeField] private SalidaManager _manager;        // el objeto Salida

    [Header("Aspecto")]
    [SerializeField] private float _velocidadFundido = 2.5f;    // alfa por segundo (2.5 = 0.4 s)
    [SerializeField] private Color _colorFondo = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private int _tamanoTitulo = 64;
    [SerializeField] private int _tamanoTexto = 30;

    private Font _fuente;
    private CanvasGroup _grupo;
    private Text _titulo;
    private Text _subtitulo;
    private Text _lista;
    private float _alfaObjetivo;
    private bool _listo;

    private void Awake()
    {
        if (_manager == null) _manager = FindFirstObjectByType<SalidaManager>();

        if (_manager == null)
        {
            Debug.LogError("PantallaFinSalida: falta el SalidaManager en la escena.", this);
            enabled = false;
            return;
        }

        _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConstruirInterfaz();
        _grupo.alpha = 0f;
        _listo = true;
    }

    private void OnEnable()
    {
        if (!_listo) return;
        _manager.SalidaTerminada += Mostrar;
        _manager.NuevaSalida += Ocultar;
    }

    private void OnDisable()
    {
        if (!_listo) return;
        _manager.SalidaTerminada -= Mostrar;
        _manager.NuevaSalida -= Ocultar;
    }

    private void Update()
    {
        _grupo.alpha = Mathf.MoveTowards(_grupo.alpha, _alfaObjetivo, _velocidadFundido * Time.unscaledDeltaTime);
    }

    private void Mostrar(ResumenSalida resumen)
    {
        if (resumen.Motivo == MotivoFin.SinGasolina)
        {
            _titulo.text = "SIN GASOLINA";
            _subtitulo.text = resumen.PiezasPerdidas > 0
                ? $"Te remolcan a la base y pierdes el {Mathf.RoundToInt(resumen.FraccionPerdida * 100f)} % del botín ({resumen.PiezasPerdidas} piezas)"
                : "Te remolcan a la base. No llevabas botín que perder";
        }
        else
        {
            _titulo.text = "DE VUELTA EN LA BASE";
            _subtitulo.text = "Botín entregado en la tienda";
        }

        _lista.text = ConstruirLista(resumen.Entregado);
        _alfaObjetivo = 1f;
    }

    private void Ocultar()
    {
        _alfaObjetivo = 0f;
    }

    private string ConstruirLista(List<KeyValuePair<MaterialChatarra, int>> entregado)
    {
        StringBuilder texto = new StringBuilder("Entregado en la tienda:\n");

        if (entregado == null || entregado.Count == 0)
        {
            texto.AppendLine("Nada");
            return texto.ToString();
        }

        foreach (KeyValuePair<MaterialChatarra, int> par in entregado)
        {
            texto.AppendLine($"{par.Key.Nombre}  x{par.Value}");
        }
        return texto.ToString();
    }

    // ---------- Construcción ----------

    private void ConstruirInterfaz()
    {
        GameObject objetoCanvas = new GameObject("CanvasFinSalida", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        objetoCanvas.transform.SetParent(transform, false);

        Canvas canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // por encima del resto del HUD

        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        _grupo = objetoCanvas.GetComponent<CanvasGroup>();
        _grupo.interactable = false;
        _grupo.blocksRaycasts = false;

        Image fondo = CrearImagen("Fondo", objetoCanvas.transform, _colorFondo);
        fondo.rectTransform.anchorMin = Vector2.zero;
        fondo.rectTransform.anchorMax = Vector2.one;
        fondo.rectTransform.offsetMin = Vector2.zero;
        fondo.rectTransform.offsetMax = Vector2.zero;

        _titulo = CrearTexto("Titulo", fondo.transform, _tamanoTitulo, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(1600f, 100f));
        _subtitulo = CrearTexto("Subtitulo", fondo.transform, _tamanoTexto, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(1600f, 60f));
        _lista = CrearTexto("Lista", fondo.transform, _tamanoTexto, TextAnchor.UpperCenter, new Vector2(0f, -40f), new Vector2(800f, 400f));
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

    private Text CrearTexto(string nombre, Transform padre, int tamano, TextAnchor alineacion, Vector2 posicion, Vector2 dimension)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(padre, false);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = dimension;

        Text texto = objeto.GetComponent<Text>();
        texto.font = _fuente;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }
}