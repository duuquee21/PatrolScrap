using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DescargaBotin : MonoBehaviour
{
    [Header("Vuelo")]
    [SerializeField] private float _duracionVuelo = 0.9f;    // s de cada pieza en el aire
    [SerializeField] private float _alturaArco = 6f;         // m que sube la pieza en lo más alto del arco
    [SerializeField] private float _intervalo = 0.07f;       // s entre una pieza y la siguiente
    [SerializeField] private int _maximoCubos = 30;          // con más unidades, cada pieza lleva varias

    [Header("Aspecto")]
    [SerializeField] private float _tamanoCubo = 0.9f;       // m: tamaño del cubo, o del modelo si el material tiene uno
    [SerializeField] private float _alturaSalida = 1.2f;     // m sobre el coche por donde salen
    [SerializeField] private float _dispersion = 0.8f;       // m de azar al salir, para que no salgan en fila
    [SerializeField] private float _giroMaximo = 360f;       // grados por segundo

    private int _enVuelo;

    // Se usa con: yield return descarga.Descargar(...). Termina cuando ha aterrizado la última pieza
    public IEnumerator Descargar(List<KeyValuePair<MaterialChatarra, int>> lote, InventarioCoche inventario, Transform origen, TiendaDestino tienda)
    {
        int total = 0;
        foreach (KeyValuePair<MaterialChatarra, int> par in lote) total += par.Value;
        if (total <= 0) yield break;

        // Si hay más unidades que piezas máximas, cada pieza representa varias
        int unidadesPorCubo = Mathf.Max(1, Mathf.CeilToInt(total / (float)_maximoCubos));

        foreach (KeyValuePair<MaterialChatarra, int> par in lote)
        {
            int restantes = par.Value;

            while (restantes > 0)
            {
                int unidades = Mathf.Min(unidadesPorCubo, restantes);
                restantes -= unidades;

                // El maletero se vacía a la vez que sale cada pieza
                inventario.TryQuitar(par.Key, unidades);

                _enVuelo++;
                StartCoroutine(Volar(par.Key, unidades, origen.position, tienda));

                yield return new WaitForSeconds(_intervalo);
            }
        }

        // Esperamos a que aterrice la última
        while (_enVuelo > 0) yield return null;
    }

    private IEnumerator Volar(MaterialChatarra tipo, int unidades, Vector3 salida, TiendaDestino tienda)
    {
        GameObject pieza = CrearPieza(tipo);

        Vector3 inicio = salida + Vector3.up * _alturaSalida + Random.insideUnitSphere * _dispersion;
        Vector3 giro = Random.insideUnitSphere * _giroMaximo;

        float tiempo = 0f;
        while (tiempo < _duracionVuelo)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / _duracionVuelo);

            // Recta entre salida y destino, más un arco vertical: es la parábola
            Vector3 posicion = Vector3.Lerp(inicio, tienda.PuntoEntrada, t);
            posicion.y += 4f * _alturaArco * t * (1f - t);

            pieza.transform.position = posicion;
            pieza.transform.Rotate(giro * Time.deltaTime, Space.Self);

            yield return null;
        }

        Destroy(pieza);
        tienda.Recibir(tipo, unidades);
        _enVuelo--;
    }

    private GameObject CrearPieza(MaterialChatarra tipo)
    {
        if (tipo.PrefabPieza != null)
        {
            // Un vacío como "percha": el modelo queda centrado en él y gira alrededor de su centro
            GameObject contenedor = new GameObject("PiezaBotin");
            MallaUtil.Colocar(tipo.PrefabPieza, contenedor.transform, _tamanoCubo);
            return contenedor;
        }

        // Sin modelo: el cubo de color de siempre
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "CuboBotin";
        Destroy(cubo.GetComponent<Collider>());   // solo visual: no debe chocar con nada
        cubo.transform.localScale = Vector3.one * _tamanoCubo;
        cubo.GetComponent<Renderer>().material.color = tipo.Color;
        return cubo;
    }
}