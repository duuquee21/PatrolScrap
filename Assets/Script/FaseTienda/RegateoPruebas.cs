using System.Text;
using UnityEngine;

public class RegateoPruebas : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private GeneradorClientes _generador;
    [SerializeField] private Cartera _cartera;
    [SerializeField] private TiendaDestino _tienda;

    [Header("Prueba manual")]
    [SerializeField] private int _precioAPedir = 100;

    [Header("Simulación de estrategias")]
    [SerializeField] private TipoCliente[] _tipos;                 // los 4 assets
    [SerializeField] private MaterialChatarra _materialSimulacion; // p. ej. Cobre
    [SerializeField, Min(1)] private int _cantidadSimulacion = 3;
    [SerializeField, Min(100)] private int _partidas = 2000;

    private Regateo _regateo;
    private bool _cobrado;

    private void Awake()
    {
        if (_generador == null) _generador = FindFirstObjectByType<GeneradorClientes>();
        if (_cartera == null) _cartera = FindFirstObjectByType<Cartera>();
        if (_tienda == null) _tienda = FindFirstObjectByType<TiendaDestino>();
    }

    // ---------- Prueba manual: clic derecho en el título del componente, con el juego en marcha ----------

    [ContextMenu("TEST: nuevo cliente")]
    private void NuevoCliente()
    {
        if (!Application.isPlaying) return;

        Cliente cliente = _generador != null ? _generador.Generar() : null;
        if (cliente == null)
        {
            Debug.Log("No hay cliente: la tienda está vacía. Llena el stock de prueba en GeneradorClientes.");
            return;
        }

        _regateo = new Regateo(cliente);
        _cobrado = false;

        Debug.Log($"NUEVO CLIENTE (los valores ocultos son solo para depurar): {cliente}\n" +
                  $"Dice: {_regateo.DecirSaludo()} {_regateo.DecirOferta()}");
    }

    [ContextMenu("TEST: aceptar")]
    private void Aceptar()
    {
        if (HayRegateo()) Mostrar(_regateo.Aceptar());
    }

    [ContextMenu("TEST: rechazar")]
    private void Rechazar()
    {
        if (HayRegateo()) Mostrar(_regateo.Rechazar());
    }

    [ContextMenu("TEST: negociar (usa Precio A Pedir)")]
    private void Negociar()
    {
        if (HayRegateo()) Mostrar(_regateo.Negociar(_precioAPedir));
    }

    [ContextMenu("TEST: negociar pidiendo el precio de referencia")]
    private void NegociarReferencia()
    {
        if (HayRegateo()) Mostrar(_regateo.Negociar(_regateo.Cliente.PrecioReferencia));
    }

    private bool HayRegateo()
    {
        if (!Application.isPlaying) return false;
        if (_regateo == null)
        {
            Debug.Log("Pulsa primero 'TEST: nuevo cliente'.");
            return false;
        }
        return true;
    }

    private void Mostrar(ResultadoRegateo res)
    {
        Cliente c = _regateo.Cliente;

        string linea = $"[{res.Estado}] ronda {_regateo.Rondas} | precio {res.Precio} $ | \"{res.Frase}\"" +
                       (res.Aviso ? "  <- AVISO DE IMPACIENCIA" : "");
        linea += $"\n   (depuración) límite {c.Limite} $ | oferta actual {c.OfertaActual} $ | paciencia {c.Paciencia:F0}/{c.PacienciaInicial:F0}";
        Debug.Log(linea);

        if (res.Estado == EstadoRegateo.Vendido && !_cobrado)
        {
            _cobrado = true;   // si alguien pulsa 'aceptar' otra vez, no se cobra dos veces
            CerrarVenta(res.Precio);
        }
    }

    // TEMPORAL: aquí se cobra para poder probar. En el paso 5 pasará al flujo de la tienda
    private void CerrarVenta(int precio)
    {
        Cliente c = _regateo.Cliente;

        if (_tienda != null && !_tienda.TryQuitar(c.Material, c.Cantidad))
        {
            Debug.LogWarning("La tienda ya no tiene ese stock: venta no cobrada.");
            return;
        }

        if (_cartera != null) _cartera.Ingresar(precio);

        Debug.Log($"VENTA: {c.Cantidad} x {c.Material.Nombre} por {precio} $ " +
                  $"(referencia {c.PrecioReferencia} $, {(float)precio / c.PrecioReferencia:F2}x)");
    }

    // ---------- Simulación ----------

    [ContextMenu("TEST: simular estrategias")]
    private void Simular()
    {
        if (!Application.isPlaying) return;
        if (_tipos == null || _tipos.Length == 0 || _materialSimulacion == null)
        {
            Debug.LogWarning("Asigna los Tipos y el Material Simulacion.");
            return;
        }

        string[] estrategias =
        {
            "Acepta la primera oferta",
            "Pide el precio de referencia",
            "Pide el doble del precio",
            "Conoce el límite (techo ideal)"
        };

        StringBuilder texto = new StringBuilder(
            $"Simulación de {_partidas} clientes por tipo y estrategia (pedido: {_cantidadSimulacion} x {_materialSimulacion.Nombre}).\n" +
            "Cada línea: % de ventas | ingreso medio × precio de referencia (cuenta 0 si no vende)\n\n");

        foreach (TipoCliente tipo in _tipos)
        {
            if (tipo == null) continue;
            texto.AppendLine(tipo.Nombre);

            for (int e = 0; e < estrategias.Length; e++)
            {
                int ventas = 0;
                float ingreso = 0f;

                for (int i = 0; i < _partidas; i++)
                {
                    Cliente cliente = new Cliente(tipo, _materialSimulacion, _cantidadSimulacion);
                    Regateo regateo = new Regateo(cliente);

                    JugarEstrategia(e, regateo);

                    if (regateo.Estado == EstadoRegateo.Vendido)
                    {
                        ventas++;
                        ingreso += (float)regateo.PrecioFinal / cliente.PrecioReferencia;
                    }
                }

                texto.AppendLine($"   {estrategias[e]}: {100f * ventas / _partidas:F0} % ventas | {ingreso / _partidas:F2}x");
            }
            texto.AppendLine();
        }

        Debug.Log(texto.ToString());
    }

    // Cada estrategia solo usa lo que el jugador puede ver, salvo la última, que hace trampa a propósito
    private static void JugarEstrategia(int estrategia, Regateo regateo)
    {
        Cliente c = regateo.Cliente;

        for (int ronda = 0; regateo.EnCurso; ronda++)
        {
            switch (estrategia)
            {
                case 0:
                    regateo.Aceptar();
                    break;

                case 1:   // pide el precio de catálogo; se conforma con el 90 % o tras 4 rondas
                    if (c.OfertaActual >= 0.9f * c.PrecioReferencia || ronda >= 4) regateo.Aceptar();
                    else regateo.Negociar(c.PrecioReferencia);
                    break;

                case 2:   // pide el doble; se conforma con el 120 % o tras 3 rondas
                    if (c.OfertaActual >= 1.2f * c.PrecioReferencia || ronda >= 3) regateo.Aceptar();
                    else regateo.Negociar(c.PrecioReferencia * 2);
                    break;

                default:  // trampa: conoce el límite oculto y pide justo eso. Es el máximo posible
                    regateo.Negociar(c.Limite);
                    break;
            }
        }
    }
}