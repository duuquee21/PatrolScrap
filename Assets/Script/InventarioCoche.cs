using System.Collections.Generic;
using UnityEngine;

public class InventarioCoche : MonoBehaviour
{
    [SerializeField] private int _capacidad = 20;      // espacio total del maletero

    private readonly Dictionary<MaterialChatarra, int> _contenido = new Dictionary<MaterialChatarra, int>();
    private int _espacioUsado;

    // La UI se suscribe a este evento para redibujarse solo cuando cambie algo
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

    // Devuelve false si no hay tantas unidades
    public bool TryQuitar(MaterialChatarra tipo, int cantidad)
    {
        if (Cantidad(tipo) < cantidad) return false;

        _contenido[tipo] -= cantidad;
        _espacioUsado -= tipo.Espacio * cantidad;

        Cambiado?.Invoke();
        return true;
    }

    public int Cantidad(MaterialChatarra tipo)
    {
        return _contenido.TryGetValue(tipo, out int cantidad) ? cantidad : 0;
    }

    // Foto del contenido actual (sin materiales a cero). No cambia con el inventario
    public List<KeyValuePair<MaterialChatarra, int>> Instantanea()
    {
        List<KeyValuePair<MaterialChatarra, int>> lista = new List<KeyValuePair<MaterialChatarra, int>>();
        foreach (KeyValuePair<MaterialChatarra, int> par in _contenido)
        {
            if (par.Value > 0) lista.Add(par);
        }
        return lista;
    }

    // Quita una fracción de cada material. Devuelve cuántas piezas se han perdido
    public int PerderFraccion(float fraccion)
    {
        int perdidas = 0;

        // Copiamos las claves: no se puede modificar un diccionario mientras se recorre
        List<MaterialChatarra> tipos = new List<MaterialChatarra>(_contenido.Keys);

        foreach (MaterialChatarra tipo in tipos)
        {
            int actual = _contenido[tipo];
            int quitar = Mathf.Min(actual, Mathf.CeilToInt(actual * fraccion));

            _contenido[tipo] = actual - quitar;
            _espacioUsado -= quitar * tipo.Espacio;
            perdidas += quitar;
        }

        if (perdidas > 0) Cambiado?.Invoke();
        return perdidas;
    }

    // Deja todos los materiales a 0 (las claves se mantienen para que el HUD se actualice)
    public void VaciarTodo()
    {
        List<MaterialChatarra> tipos = new List<MaterialChatarra>(_contenido.Keys);
        foreach (MaterialChatarra tipo in tipos) _contenido[tipo] = 0;

        _espacioUsado = 0;
        Cambiado?.Invoke();
    }
}