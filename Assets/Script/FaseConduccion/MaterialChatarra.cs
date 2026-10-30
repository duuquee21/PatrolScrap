using UnityEngine;

[CreateAssetMenu(menuName = "PatrolScrap/Material de chatarra", fileName = "Material_")]
public class MaterialChatarra : ScriptableObject
{
    [Header("Datos")]
    [SerializeField] private string _nombre = "";             // si queda vacío, se usa el nombre del archivo
    [SerializeField] private Color _color = Color.gray;       // casilla del inventario, y cubo si no hay modelo
    [SerializeField] private Sprite _icono;                   // opcional, para el inventario
    [SerializeField] private int _valorBase = 10;             // precio de referencia en la tienda
    [SerializeField] private int _espacio = 1;                // cuánto ocupa en el maletero

    [Header("Modelo 3D (opcional)")]
    [SerializeField] private GameObject _prefabPieza;         // el mesh descargado
    [SerializeField] private float _tamanoPieza = 0.7f;       // m que mide su dimensión mayor en el juego

    public string Nombre => string.IsNullOrWhiteSpace(_nombre) ? name : _nombre;
    public Color Color => _color;
    public Sprite Icono => _icono;
    public int ValorBase => _valorBase;
    public int Espacio => _espacio;
    public GameObject PrefabPieza => _prefabPieza;
    public float TamanoPieza => _tamanoPieza;
}