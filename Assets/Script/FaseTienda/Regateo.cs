using UnityEngine;

public enum EstadoRegateo { EnCurso, Vendido, Rechazado, SeFueSinPaciencia, SeFueOfendido }

public struct ResultadoRegateo
{
    public EstadoRegateo Estado;
    public int Precio;      // en curso: la oferta actual del cliente · vendido: el precio de la venta · si no hay venta: 0
    public string Frase;    // lo que dice el cliente
    public bool Aviso;      // ha soltado una frase de impaciencia
}

public class Regateo
{
    private const float TramoCodicia = 0.10f;   // la sensibilidad a la codicia es "por cada 10 %"

    public Cliente Cliente { get; }
    public EstadoRegateo Estado { get; private set; } = EstadoRegateo.EnCurso;
    public int Rondas { get; private set; }
    public int PrecioFinal { get; private set; }
    public bool EnCurso => Estado == EstadoRegateo.EnCurso;

    private readonly string _pieza;

    public Regateo(Cliente cliente)
    {
        Cliente = cliente;
        _pieza = cliente.Material.Nombre;
    }

    public string DecirSaludo() => Decir(TipoFrase.Saludo, Cliente.OfertaActual);
    public string DecirOferta() => Decir(TipoFrase.Oferta, Cliente.OfertaActual);

    public ResultadoRegateo Aceptar()
    {
        if (!EnCurso) return Cerrado();
        return Vender(Cliente.OfertaActual);
    }

    public ResultadoRegateo Rechazar()
    {
        if (!EnCurso) return Cerrado();

        Estado = EstadoRegateo.Rechazado;
        return Resultado(TipoFrase.Rechazo, 0, false);
    }

    public ResultadoRegateo Negociar(int precio)
    {
        if (!EnCurso) return Cerrado();

        // 0) Pedir lo mismo o menos de lo que ya ofrece no es negociar: es aceptar
        if (precio <= Cliente.OfertaActual) return Vender(Cliente.OfertaActual);

        Rondas++;

        // 1) Dentro de su límite: cierra la venta al precio pedido
        if (precio <= Cliente.Limite) return Vender(precio);

        // 2) Por encima de su límite: ¿cuánto se pasa?
        float exceso = (precio - Cliente.Limite) / (float)Cliente.Limite;
        float umbral = Cliente.Tipo.UmbralOfensa;
        if (umbral > 0f && exceso > umbral) return Irse(EstadoRegateo.SeFueOfendido, TipoFrase.Ofensa);

        // 3) Gasta paciencia: una parte fija por ronda y otra según lo codicioso que es lo que pides
        Cliente.Paciencia -= Cliente.Tipo.PerdidaPorRonda + Cliente.Tipo.SensibilidadCodicia * (exceso / TramoCodicia);
        if (Cliente.Paciencia <= 0f) return Irse(EstadoRegateo.SeFueSinPaciencia, TipoFrase.Marcha);

        // 4) Contraoferta: sube una fracción de la distancia que le separa de su límite
        int distancia = Cliente.Limite - Cliente.OfertaActual;
        int subida = Mathf.Max(1, Mathf.RoundToInt(distancia * Cliente.Tipo.CesionPorRonda));
        Cliente.OfertaActual = Mathf.Min(Cliente.Limite, Cliente.OfertaActual + subida);

        // 5) Si le queda poca paciencia, lo dice: es la pista que el jugador puede leer
        string frase = Decir(TipoFrase.ContraOferta, Cliente.OfertaActual);
        bool aviso = Cliente.Paciencia01 < Cliente.Tipo.AvisoImpaciencia;
        if (aviso) frase += " " + Decir(TipoFrase.Impaciencia, Cliente.OfertaActual);

        return new ResultadoRegateo { Estado = Estado, Precio = Cliente.OfertaActual, Frase = frase, Aviso = aviso };
    }

    // ---------- Ayudas ----------

    private ResultadoRegateo Vender(int precio)
    {
        Estado = EstadoRegateo.Vendido;
        PrecioFinal = precio;
        return Resultado(TipoFrase.Aceptacion, precio, false);
    }

    private ResultadoRegateo Irse(EstadoRegateo motivo, TipoFrase frase)
    {
        Estado = motivo;
        return Resultado(frase, 0, false);
    }

    private ResultadoRegateo Resultado(TipoFrase tipo, int precio, bool aviso)
    {
        int precioFrase = precio > 0 ? precio : Cliente.OfertaActual;
        return new ResultadoRegateo { Estado = Estado, Precio = precio, Frase = Decir(tipo, precioFrase), Aviso = aviso };
    }

    // Si ya terminó y alguien insiste, no cambia nada
    private ResultadoRegateo Cerrado()
    {
        return new ResultadoRegateo { Estado = Estado, Precio = PrecioFinal, Frase = "...", Aviso = false };
    }

    private string Decir(TipoFrase tipo, int precio)
    {
        return Cliente.Tipo.Frase(tipo, _pieza, Cliente.Cantidad, precio);
    }
}