using UnityEngine;

public class AjustarAlSuelo : MonoBehaviour
{
    [Header("Suelo")]
    [SerializeField] private LayerMask _capaSuelo;            // si se deja vacía, usa la capa "Suelo"
    [SerializeField] private float _alturaRayo = 60f;         // m por encima desde donde baja el rayo

    [Header("Posición")]
    [SerializeField] private float _hundimiento = 0.1f;       // m que se hunde en la arena: se ve plantada

    [Header("Orientación")]
    [SerializeField, Range(0f, 1f)] private float _alineacion = 0.6f;   // 0 = siempre recta, 1 = sigue la pendiente
    [SerializeField] private float _inclinacionMaxima = 20f;  // grados
    [SerializeField] private bool _giroAleatorio = true;

    private readonly RaycastHit[] _golpes = new RaycastHit[8];   // se reutiliza: sin basura

    private void Start()
    {
        Ajustar();
    }

    // Pública: un generador de chunks podrá llamarla al crear cada objeto
    public void Ajustar()
    {
        if (_capaSuelo.value == 0) _capaSuelo = LayerMask.GetMask("Suelo");

        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("AjustarAlSuelo: el objeto no tiene Collider para medir su base.", this);
            return;
        }

        // 1) Orientación: giro aleatorio y, según la pendiente, una inclinación limitada
        Quaternion giro = _giroAleatorio
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (!SueloEn(transform.position, out RaycastHit golpeCentro))
        {
            Debug.LogWarning("AjustarAlSuelo: no hay terreno bajo " + name + ". ¿Está el terreno en la capa Suelo y dentro de su tamaño?", this);
            return;
        }

        Quaternion pendiente = Quaternion.FromToRotation(Vector3.up, golpeCentro.normal) * giro;
        Quaternion resultado = Quaternion.Slerp(giro, pendiente, _alineacion);
        transform.rotation = Quaternion.RotateTowards(giro, resultado, _inclinacionMaxima);

        // El collider debe enterarse del giro antes de medir su base
        Physics.SyncTransforms();

        // Límites de TODOS los colliders juntos
        Bounds limites = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) limites.Encapsulate(colliders[i].bounds);

        // 2) Altura: el punto más alto del suelo bajo la huella (centro y cuatro esquinas)
        float sueloMaximo = golpeCentro.point.y;
        Vector3[] puntos =
        {
            new Vector3(limites.min.x, 0f, limites.min.z),
            new Vector3(limites.min.x, 0f, limites.max.z),
            new Vector3(limites.max.x, 0f, limites.min.z),
            new Vector3(limites.max.x, 0f, limites.max.z)
        };

        foreach (Vector3 punto in puntos)
        {
            Vector3 origen = new Vector3(punto.x, transform.position.y, punto.z);
            if (SueloEn(origen, out RaycastHit golpe)) sueloMaximo = Mathf.Max(sueloMaximo, golpe.point.y);
        }

        // 3) La base de la caja pasa a estar sobre ese punto
        float correccion = (sueloMaximo - _hundimiento) - limites.min.y;
        transform.position += Vector3.up * correccion;

        // TEMPORAL: para comprobar que funciona. Bórralo cuando lo veas bien
        Debug.Log($"AjustarAlSuelo {name}: suelo en Y={sueloMaximo:F2}, movida {correccion:F2} m (golpeó a {golpeCentro.collider.name})", this);
    }

    // Baja un rayo desde bien arriba y devuelve el golpe más cercano que NO sea este objeto.
    // Así funciona aunque la caja esté enterrada, y aunque esté en la capa Suelo por error
    private bool SueloEn(Vector3 posicion, out RaycastHit mejor)
    {
        Vector3 origen = new Vector3(posicion.x, posicion.y + _alturaRayo, posicion.z);
        int n = Physics.RaycastNonAlloc(origen, Vector3.down, _golpes, _alturaRayo * 2f + 100f, _capaSuelo, QueryTriggerInteraction.Ignore);

        mejor = default;
        float menor = float.MaxValue;
        bool hay = false;

        for (int i = 0; i < n; i++)
        {
            if (_golpes[i].transform.IsChildOf(transform)) continue;   // la propia caja no cuenta

            if (_golpes[i].distance < menor)
            {
                menor = _golpes[i].distance;
                mejor = _golpes[i];
                hay = true;
            }
        }

        return hay;
    }
}