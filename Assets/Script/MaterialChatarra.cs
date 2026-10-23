using UnityEngine;

[CreateAssetMenu(menuName = "PatrolScrap/Material de chatarra", fileName = "Material_")]
public class MaterialChatarra : ScriptableObject
{
    [SerializeField] private string _nombre = "Chatarra";
    [SerializeField] private Color _color = Color.gray;
    [SerializeField] private Sprite _icono;           // opcional, para el inventario
    [SerializeField] private int _valorBase = 10;     // precio de referencia en la tienda
    [SerializeField] private int _espacio = 1;        // cuánto ocupa en el maletero

    public string Nombre => _nombre;
    public Color Color => _color;
    public Sprite Icono => _icono;
    public int ValorBase => _valorBase;
    public int Espacio => _espacio;
}