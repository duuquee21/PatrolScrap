using UnityEngine;

public class Recolectable : MonoBehaviour
{
    [SerializeField] private MaterialChatarra _tipo;
    [SerializeField] private int _cantidad = 1;
    [SerializeField] private float _retrasoRecogida = 1f;   // segundos tras aparecer en los que no se puede recoger

    private float _momentoRecogible;   // por defecto vale 0: una pieza colocada a mano se recoge siempre

    public MaterialChatarra Tipo => _tipo;
    public int Cantidad => _cantidad;
    public bool PuedeRecogerse => Time.time >= _momentoRecogible;

    // La llama quien crea la pieza (el objeto destruible)
    public void Configurar(MaterialChatarra tipo, int cantidad = 1)
    {
        _tipo = tipo;
        _cantidad = cantidad;
        _momentoRecogible = Time.time + _retrasoRecogida;

        // Pintamos el cubo con el color del material
        Renderer render = GetComponentInChildren<Renderer>();
        if (render != null) render.material.color = tipo.Color;
    }
}