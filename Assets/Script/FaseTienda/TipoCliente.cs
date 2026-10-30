using UnityEngine;

public enum TipoFrase { Saludo, Oferta, ContraOferta, Aceptacion, Rechazo, Impaciencia, Marcha, Ofensa }

[CreateAssetMenu(menuName = "PatrolScrap/Tipo de cliente", fileName = "Cliente_")]
public class TipoCliente : ScriptableObject
{
    [Header("Identidad (lo que ve el jugador)")]
    [SerializeField] private string _nombre = "Cliente";
    [SerializeField, TextArea(2, 4)] private string _pista = "";          // descripción para ti (diseño), no para el jugador
    [SerializeField] private Color _color = Color.gray;                   // color del retrato: primera pista visual
    [SerializeField] private Sprite _retrato;                             // opcional

    [Header("Aparición")]
    [SerializeField, Min(0f)] private float _peso = 1f;                   // probabilidad relativa

    [Header("Lo que pagaría (OCULTO) - rangos: X = mínimo, Y = máximo")]
    [SerializeField] private Vector2 _limite = new Vector2(1f, 1.25f);          // × precio de referencia
    [SerializeField] private Vector2 _ofertaInicial = new Vector2(0.7f, 0.85f); // × su límite
    [SerializeField, Range(0f, 1f)] private float _cesionPorRonda = 0.35f;      // fracción de la distancia a su límite que sube cada ronda

    [Header("Paciencia (OCULTA)")]
    [SerializeField] private Vector2 _paciencia = new Vector2(80f, 100f);       // puntos iniciales
    [SerializeField] private float _perdidaPorRonda = 15f;                      // puntos que gasta cada ronda de negociación
    [SerializeField] private float _sensibilidadCodicia = 20f;                  // puntos extra por cada 10 % que pides sobre su límite
    [SerializeField, Min(0f)] private float _umbralOfensa = 0f;                 // 0.35 = se va si pides un 35 % más de su límite (0 = nunca)
    [SerializeField, Range(0f, 1f)] private float _avisoImpaciencia = 0.35f;    // fracción de paciencia restante a la que avisa

    [Header("Frases")]
    [SerializeField] private string[] _saludo;
    [SerializeField] private string[] _oferta;
    [SerializeField] private string[] _contraOferta;
    [SerializeField] private string[] _aceptacion;
    [SerializeField] private string[] _rechazo;
    [SerializeField] private string[] _impaciencia;
    [SerializeField] private string[] _marcha;
    [SerializeField] private string[] _ofensa;

    public string Nombre => _nombre;
    public string Pista => _pista;
    public Color Color => _color;
    public Sprite Retrato => _retrato;
    public float Peso => _peso;
    public float CesionPorRonda => _cesionPorRonda;
    public float PerdidaPorRonda => _perdidaPorRonda;
    public float SensibilidadCodicia => _sensibilidadCodicia;
    public float UmbralOfensa => _umbralOfensa;
    public float AvisoImpaciencia => _avisoImpaciencia;

    public float SortearLimite() => Random.Range(_limite.x, _limite.y);
    public float SortearOfertaInicial() => Random.Range(_ofertaInicial.x, _ofertaInicial.y);
    public float SortearPaciencia() => Random.Range(_paciencia.x, _paciencia.y);

    // Elige una frase al azar y sustituye los marcadores
    public string Frase(TipoFrase tipo, string pieza, int cantidad, int precio)
    {
        string[] lista = tipo switch
        {
            TipoFrase.Saludo => _saludo,
            TipoFrase.Oferta => _oferta,
            TipoFrase.ContraOferta => _contraOferta,
            TipoFrase.Aceptacion => _aceptacion,
            TipoFrase.Rechazo => _rechazo,
            TipoFrase.Impaciencia => _impaciencia,
            TipoFrase.Marcha => _marcha,
            _ => _ofensa
        };

        if (lista == null || lista.Length == 0) return "...";

        return lista[Random.Range(0, lista.Length)]
            .Replace("{pieza}", pieza)
            .Replace("{cantidad}", cantidad.ToString())
            .Replace("{precio}", precio.ToString());
    }

    // Que el mínimo de un rango no pase nunca de su máximo
    private void OnValidate()
    {
        _limite.y = Mathf.Max(_limite.x, _limite.y);
        _ofertaInicial.y = Mathf.Max(_ofertaInicial.x, _ofertaInicial.y);
        _paciencia.y = Mathf.Max(_paciencia.x, _paciencia.y);
    }

    // ---------- Valores de ejemplo: clic derecho en el título del componente ----------

    [ContextMenu("Rellenar: Normal")]
    private void RellenarNormal()
    {
        Rellenar("Normal", "Sin ninguna señal particular. Compra a un precio cercano al de referencia.",
            new Color(0.62f, 0.68f, 0.78f), 45f,
            new Vector2(1.00f, 1.25f), new Vector2(0.70f, 0.85f), 0.35f,
            new Vector2(80f, 100f), 15f, 20f, 0f, 0.35f,
            new[] { "Buenas. Vengo a ver qué tienes.", "Hola, ¿qué hay hoy en el almacén?" },
            new[] { "Quiero {cantidad} de {pieza}. Te doy {precio} $.", "Me llevo {cantidad} de {pieza} por {precio} $." },
            new[] { "Mmm... subo a {precio} $.", "Está bien, {precio} $ y cerramos." },
            new[] { "Trato hecho.", "Perfecto, me lo llevo." },
            new[] { "Qué pena. Otra vez será.", "Bueno, buscaré en otro sitio." },
            new[] { "Se me acaba la paciencia...", "No me hagas perder el tiempo." },
            new[] { "Se acabó. Me voy.", "Esto no lleva a ningún sitio." },
            new[] { "Eso es una falta de respeto.", "Con esas pretensiones, no." });
    }

    [ContextMenu("Rellenar: Regateador")]
    private void RellenarRegateador()
    {
        Rellenar("Regateador", "Ropa remendada y monedas contadas. Ofrece muy poco y nunca pagará mucho, pero aguanta el regateo.",
            new Color(0.75f, 0.82f, 0.35f), 25f,
            new Vector2(0.60f, 0.80f), new Vector2(0.55f, 0.70f), 0.15f,
            new Vector2(110f, 140f), 12f, 12f, 0f, 0.35f,
            new[] { "A ver... espero que los precios sean razonables.", "Antes de nada: no pienso pagar de más." },
            new[] { "{cantidad} de {pieza}... eso vale {precio} $, y ya es generoso.", "Te doy {precio} $. Es lo que vale, ni un céntimo más." },
            new[] { "Ni hablar. {precio} $ y gracias.", "Uf, mi cartera sufre... {precio} $." },
            new[] { "Esto es un robo, pero vale.", "Está bien. Me arruinas." },
            new[] { "Lo sabía, todo el mundo se quiere forrar.", "Pues me voy con la cartera intacta." },
            new[] { "Estoy cansándome de tanto número.", "Cada vez me gusta menos este trato." },
            new[] { "No pago eso. Adiós.", "Mi dinero se queda conmigo." },
            new[] { "¡Eso es un atraco!", "¡Ni en sueños!" });
    }

    [ContextMenu("Rellenar: Impaciente")]
    private void RellenarImpaciente()
    {
        Rellenar("Impaciente", "Mira el reloj y golpea el suelo con el pie. Paga bien, pero se va a la primera de cambio.",
            new Color(0.95f, 0.40f, 0.35f), 20f,
            new Vector2(1.15f, 1.45f), new Vector2(0.80f, 0.90f), 0.50f,
            new Vector2(30f, 45f), 25f, 25f, 0f, 0.5f,
            new[] { "¡Rápido, que tengo prisa!", "Sin rodeos, que llego tarde." },
            new[] { "{cantidad} de {pieza}. {precio} $. ¡Ya!", "Toma {precio} $ y dame {cantidad} de {pieza}." },
            new[] { "¡Vale, {precio} $! ¡Pero date prisa!", "{precio} $. Última vez." },
            new[] { "¡Perfecto! ¡Adiós!", "¡Hecho, me voy!" },
            new[] { "¡No tengo tiempo para esto!", "Pues nada, adiós." },
            new[] { "¡Venga ya!", "¡No puedo esperar más!" },
            new[] { "¡Me voy! ¡Pierdes la venta!", "¡Se acabó, llego tarde!" },
            new[] { "¡Lo que me faltaba!", "¡No me hagas perder el tiempo!" });
    }

    [ContextMenu("Rellenar: Rico")]
    private void RellenarRico()
    {
        Rellenar("Rico", "Anillos y traje caro. Pagaría mucho más del precio, pero se ofende si abusas.",
            new Color(0.95f, 0.78f, 0.25f), 10f,
            new Vector2(1.60f, 2.20f), new Vector2(0.60f, 0.75f), 0.40f,
            new Vector2(90f, 120f), 10f, 15f, 0.35f, 0.35f,
            new[] { "Buenas tardes. El dinero no es problema.", "Vengo a por lo mejor que tengas." },
            new[] { "{cantidad} de {pieza}. Digamos {precio} $.", "Pagaré {precio} $, sin regatear." },
            new[] { "Sea. {precio} $.", "Si insistes... {precio} $." },
            new[] { "Excelente. Un placer hacer negocios.", "Perfecto. Quédate el cambio." },
            new[] { "Qué lástima. Me llevaré mi dinero a otra parte.", "Otra vez será." },
            new[] { "Empiezo a pensar que no sabes con quién hablas...", "No abuses." },
            new[] { "Esto me aburre. Buenas tardes.", "Me has decepcionado." },
            new[] { "¿Cómo te atreves? ¡Se acabó!", "Eso es un insulto. Adiós." });
    }

    private void Rellenar(string nombre, string pista, Color color, float peso,
        Vector2 limite, Vector2 oferta, float cesion,
        Vector2 paciencia, float perdida, float codicia, float ofensa, float aviso,
        string[] saludo, string[] ofertaF, string[] contra, string[] acepta,
        string[] rechazo, string[] impaciencia, string[] marcha, string[] ofensaF)
    {
        _nombre = nombre; _pista = pista; _color = color; _peso = peso;
        _limite = limite; _ofertaInicial = oferta; _cesionPorRonda = cesion;
        _paciencia = paciencia; _perdidaPorRonda = perdida; _sensibilidadCodicia = codicia;
        _umbralOfensa = ofensa; _avisoImpaciencia = aviso;
        _saludo = saludo; _oferta = ofertaF; _contraOferta = contra; _aceptacion = acepta;
        _rechazo = rechazo; _impaciencia = impaciencia; _marcha = marcha; _ofensa = ofensaF;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);   // para que Unity guarde los cambios en el asset
#endif
    }
}