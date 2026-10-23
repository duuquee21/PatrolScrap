using System.Collections.Generic;
using UnityEngine;

public class TiendaDestino : MonoBehaviour
{
    [Header("Punto de llegada")]
    [SerializeField] private float _alturaEntrada = 2f;    // m sobre el centro del cubo: por dónde "entran" las piezas

    [Header("Pulso al recibir")]
    [SerializeField] private float _fuerzaPulso = 0.12f;   // 0.12 = el cubo crece un 12 %
    [SerializeField] private float _velocidadPulso = 5f;

    private readonly Dictionary<MaterialChatarra, int> _stock = new Dictionary<MaterialChatarra, int>();
    private Vector3 _escalaBase;
    private float _pulso;   // 1 = recién recibido, 0 = en reposo

    public event System.Action StockCambiado;

    public IReadOnlyDictionary<MaterialChatarra, int> Stock => _stock;
    public Vector3 PuntoEntrada => transform.position + Vector3.up * _alturaEntrada;

    private void Awake()
    {
        _escalaBase = transform.localScale;
    }

    private void Update()
    {
        if (_pulso <= 0f) return;

        _pulso = Mathf.MoveTowards(_pulso, 0f, _velocidadPulso * Time.deltaTime);
        transform.localScale = _escalaBase * (1f + _fuerzaPulso * _pulso);
    }

    public void Recibir(MaterialChatarra tipo, int cantidad)
    {
        _stock.TryGetValue(tipo, out int actual);
        _stock[tipo] = actual + cantidad;

        _pulso = 1f;
        StockCambiado?.Invoke();
    }

    // TEMPORAL: stock en pantalla para comprobar que llega. La tienda real vendrá después
    private void OnGUI()
    {
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width - 220, 10, 210, 25), "STOCK EN TIENDA");

        int fila = 1;
        foreach (KeyValuePair<MaterialChatarra, int> par in _stock)
        {
            GUI.Label(new Rect(Screen.width - 220, 10 + fila * 22, 210, 25), $"{par.Key.Nombre}: {par.Value}");
            fila++;
        }
    }
}