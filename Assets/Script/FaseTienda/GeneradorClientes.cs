using System.Collections.Generic;
using UnityEngine;

public class GeneradorClientes : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private TipoCliente[] _tipos;
    [SerializeField] private TiendaDestino _tienda;               // si se deja vacío, se busca solo
    [SerializeField, Min(1)] private int _maximoPorPedido = 5;    // unidades máximas que pide un cliente

    [Header("Pruebas")]
    [SerializeField] private MaterialChatarra[] _materialesDePrueba;

    private readonly List<MaterialChatarra> _disponibles = new List<MaterialChatarra>();

    private void Awake()
    {
        if (_tienda == null) _tienda = FindFirstObjectByType<TiendaDestino>();
    }

    // Devuelve un cliente nuevo, o null si no hay nada que vender
    public Cliente Generar()
    {
        if (_tienda == null || _tipos == null || _tipos.Length == 0)
        {
            Debug.LogWarning("GeneradorClientes: falta la tienda o la lista de tipos de cliente.", this);
            return null;
        }

        _disponibles.Clear();
        foreach (KeyValuePair<MaterialChatarra, int> par in _tienda.Stock)
        {
            if (par.Value > 0) _disponibles.Add(par.Key);
        }
        if (_disponibles.Count == 0) return null;

        TipoCliente tipo = ElegirTipo();
        MaterialChatarra material = _disponibles[Random.Range(0, _disponibles.Count)];

        // Random.Range con enteros excluye el máximo, por eso el +1
        int disponible = _tienda.Stock[material];
        int cantidad = Random.Range(1, Mathf.Min(disponible, _maximoPorPedido) + 1);

        return new Cliente(tipo, material, cantidad);
    }

    // Elección ponderada: cada tipo ocupa un trozo de la "ruleta" proporcional a su peso
    private TipoCliente ElegirTipo()
    {
        float total = 0f;
        foreach (TipoCliente t in _tipos)
        {
            if (t != null) total += t.Peso;
        }

        if (total <= 0f) return _tipos[Random.Range(0, _tipos.Length)];

        float tirada = Random.value * total;
        foreach (TipoCliente t in _tipos)
        {
            if (t == null) continue;
            tirada -= t.Peso;
            if (tirada <= 0f) return t;
        }

        return _tipos[_tipos.Length - 1];   // por redondeo de floats
    }

    // ---------- Pruebas: clic derecho en el título del componente, con el juego en marcha ----------

    [ContextMenu("TEST: llenar stock de prueba (10 de cada)")]
    private void LlenarStock()
    {
        if (!Application.isPlaying || _tienda == null) return;

        foreach (MaterialChatarra material in _materialesDePrueba)
        {
            if (material != null) _tienda.Recibir(material, 10);
        }
    }

    [ContextMenu("TEST: generar un cliente")]
    private void ProbarUno()
    {
        if (!Application.isPlaying) return;

        Cliente cliente = Generar();
        Debug.Log(cliente != null ? cliente.ToString() : "No hay stock en la tienda: llénalo primero.");
    }

    [ContextMenu("TEST: generar 200 y ver el reparto")]
    private void ProbarReparto()
    {
        if (!Application.isPlaying) return;

        const int total = 200;
        int[] cuenta = new int[_tipos.Length];
        float[] sumaLimite = new float[_tipos.Length];
        float[] sumaOferta = new float[_tipos.Length];

        for (int i = 0; i < total; i++)
        {
            Cliente cliente = Generar();
            if (cliente == null)
            {
                Debug.Log("No hay stock en la tienda: llénalo primero.");
                return;
            }

            int indice = System.Array.IndexOf(_tipos, cliente.Tipo);
            cuenta[indice]++;
            sumaLimite[indice] += (float)cliente.Limite / cliente.PrecioReferencia;
            sumaOferta[indice] += (float)cliente.OfertaInicial / cliente.PrecioReferencia;
        }

        string texto = $"Reparto de {total} clientes:\n";
        for (int i = 0; i < _tipos.Length; i++)
        {
            if (cuenta[i] == 0) continue;
            texto += $"{_tipos[i].Nombre}: {cuenta[i]} ({100f * cuenta[i] / total:F0} %) | " +
                     $"límite medio {sumaLimite[i] / cuenta[i]:F2}x | oferta inicial media {sumaOferta[i] / cuenta[i]:F2}x el precio de referencia\n";
        }
        Debug.Log(texto);
    }
}