using System;
using UnityEngine;

public class Cartera : MonoBehaviour
{
    [SerializeField] private int _dineroInicial = 0;

    private int _dinero;

    public int Dinero => _dinero;

    // total nuevo, y cuánto ha cambiado (positivo = ganancia, negativo = gasto)
    public event Action<int, int> Cambiado;

    private void Awake()
    {
        _dinero = Mathf.Max(0, _dineroInicial);
    }

    public bool PuedePagar(int cantidad)
    {
        return cantidad >= 0 && _dinero >= cantidad;
    }

    public void Ingresar(int cantidad)
    {
        if (cantidad <= 0) return;

        _dinero += cantidad;
        Cambiado?.Invoke(_dinero, cantidad);
    }

    // Devuelve false si no hay dinero suficiente: en ese caso no se resta nada
    public bool TryPagar(int cantidad)
    {
        if (cantidad < 0 || _dinero < cantidad) return false;
        if (cantidad == 0) return true;

        _dinero -= cantidad;
        Cambiado?.Invoke(_dinero, -cantidad);
        return true;
    }

    // Para cargar una partida guardada
    public void Establecer(int total)
    {
        int antes = _dinero;
        _dinero = Mathf.Max(0, total);
        Cambiado?.Invoke(_dinero, _dinero - antes);
    }

    // Para probar sin tienda: clic derecho en el título del componente, con el juego en marcha
    [ContextMenu("TEST: ingresar 50")]
    private void ProbarIngresar() { if (Application.isPlaying) Ingresar(50); }

    [ContextMenu("TEST: ingresar 1234")]
    private void ProbarIngresarMucho() { if (Application.isPlaying) Ingresar(1234); }

    [ContextMenu("TEST: pagar 30")]
    private void ProbarPagar() { if (Application.isPlaying) TryPagar(30); }

    [ContextMenu("TEST: pagar 100000 (debe fallar)")]
    private void ProbarPagarDeMas()
    {
        if (!Application.isPlaying) return;
        Debug.Log("TryPagar(100000) devuelve: " + TryPagar(100000));
    }
}