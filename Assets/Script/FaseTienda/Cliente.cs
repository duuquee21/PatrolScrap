using UnityEngine;

public class Cliente
{
    // Lo que se ve
    public TipoCliente Tipo { get; }
    public MaterialChatarra Material { get; }
    public int Cantidad { get; }
    public int PrecioReferencia { get; }     // valor base × cantidad: el precio "de catálogo"

    // Lo oculto: el jugador debe averiguarlo
    public int Limite { get; }               // lo máximo que pagaría
    public float PacienciaInicial { get; }

    // Lo que cambia durante el regateo
    public int OfertaInicial { get; }
    public int OfertaActual { get; set; }
    public float Paciencia { get; set; }

    public float Paciencia01 => PacienciaInicial > 0f ? Mathf.Clamp01(Paciencia / PacienciaInicial) : 0f;

    public Cliente(TipoCliente tipo, MaterialChatarra material, int cantidad)
    {
        Tipo = tipo;
        Material = material;
        Cantidad = cantidad;

        PrecioReferencia = Mathf.Max(1, material.ValorBase * cantidad);
        Limite = Mathf.Max(1, Mathf.RoundToInt(PrecioReferencia * tipo.SortearLimite()));

        // La oferta inicial nunca supera lo que pagaría
        OfertaInicial = Mathf.Clamp(Mathf.RoundToInt(Limite * tipo.SortearOfertaInicial()), 1, Limite);
        OfertaActual = OfertaInicial;

        PacienciaInicial = tipo.SortearPaciencia();
        Paciencia = PacienciaInicial;
    }

    public override string ToString()
    {
        return $"{Tipo.Nombre} quiere {Material.Nombre} x{Cantidad} | referencia {PrecioReferencia} $ | " +
               $"límite {Limite} $ ({(float)Limite / PrecioReferencia:F2}x) | oferta inicial {OfertaInicial} $ | " +
               $"paciencia {PacienciaInicial:F0}";
    }
}