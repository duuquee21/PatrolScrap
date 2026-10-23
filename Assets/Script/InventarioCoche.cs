using System.Collections.Generic;
using UnityEngine;

public class InventarioCoche : MonoBehaviour
{
    [SerializeField] private int _capacidad = 20;      // espacio total del maletero

    private readonly Dictionary<MaterialChatarra, int> _contenido = new Dictionary<MaterialChatarra, int>();
    private int _espacioUsado;

    // La UI se suscribirá a este evento para redibujarse solo cuando cambie algo
    public event System.Action Cambiado;

    public int Capacidad => _capacidad;
    public int EspacioUsado => _espacioUsado;
    public IReadOnlyDictionary<MaterialChatarra, int> Contenido => _contenido;

    // Devuelve false si no cabe: la pieza se queda en el suelo
    public bool TryAnadir(MaterialChatarra tipo, int cantidad)
    {
        int espacioNecesario = tipo.Espacio * cantidad;
        if (_espacioUsado + espacioNecesario > _capacidad) return false;

        _contenido.TryGetValue(tipo, out int actual);   // si no existe, actual vale 0
        _contenido[tipo] = actual + cantidad;
        _espacioUsado += espacioNecesario;

        Cambiado?.Invoke();
        return true;
    }

    public int Cantidad(MaterialChatarra tipo)
    {
        return _contenido.TryGetValue(tipo, out int cantidad) ? cantidad : 0;
    }

    // TEMPORAL: lista en pantalla para comprobar que funciona. La UI real va después
    private void OnGUI()
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(10, 10, 400, 25), $"Maletero: {_espacioUsado}/{_capacidad}" + (_espacioUsado >= _capacidad ? "  (LLENO)" : ""));

        int fila = 1;
        foreach (KeyValuePair<MaterialChatarra, int> par in _contenido)
        {
            GUI.Label(new Rect(10, 10 + fila * 22, 400, 25), $"{par.Key.Nombre}: {par.Value}");
            fila++;
        }
    }
}