using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrenoArena : MonoBehaviour
{
    [Header("Tamaño")]
    [SerializeField] private int _celdas = 175;               // celdas por lado
    [SerializeField] private float _tamanoCelda = 2f;         // m: 175 × 2 = 350 m de lado

    [Header("Dunas grandes")]
    [SerializeField] private float _alturaDunas = 3.5f;       // m
    [SerializeField] private float _escalaDunas = 0.022f;     // más alto = más dunas y más estrechas

    [Header("Dunas medianas")]
    [SerializeField] private float _alturaMedianas = 1.2f;    // m: rompe la repetición de las grandes
    [SerializeField] private float _escalaMedianas = 0.05f;

    [Header("Ondulación fina")]
    [SerializeField] private float _alturaOndulacion = 0.15f; // m: las ondas pequeñas hacen el terreno más "nervioso"
    [SerializeField] private float _escalaOndulacion = 0.09f;
    [SerializeField] private int _semilla = 1234;             // cambia el número, cambia el desierto

    [Header("Redondez")]
    [SerializeField, Range(0f, 1f)] private float _suavizadoNormales = 0.85f;   // 0 = facetado, 1 = casi liso

    [Header("Zona plana (base y salida)")]
    [SerializeField] private float _radioPlano = 25f;         // m completamente llanos
    [SerializeField] private float _radioTransicion = 40f;    // m hasta donde llegan las dunas enteras
    [SerializeField] private Vector2 _centroPlano = Vector2.zero;   // X y Z en el mundo

    [Header("Aspecto")]
    [SerializeField] private Material _material;              // el material Arena
    [SerializeField] private Color _colorArena = new Color(0.9f, 0.75f, 0.48f);   // solo se usa si no hay material
    [SerializeField] private bool _proyectarSombras = false;  // más bonito, más caro

    private void Awake()
    {
        // El terreno debe estar en la capa Suelo para que lo detecten las ruedas
        int capa = LayerMask.NameToLayer("Suelo");
        if (capa >= 0) gameObject.layer = capa;
        else Debug.LogWarning("TerrenoArena: no existe la capa 'Suelo'.", this);

        PonerMaterial();
        Generar();
    }

    private void PonerMaterial()
    {
        MeshRenderer render = GetComponent<MeshRenderer>();

        if (_material != null)
        {
            render.sharedMaterial = _material;
        }
        else if (render.sharedMaterial == null)
        {
            // Rescate: un material básico de URP para que no salga rosa
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                Material rescate = new Material(shader) { color = _colorArena };
                rescate.SetFloat("_Smoothness", 0f);
                render.sharedMaterial = rescate;
            }
            else
            {
                Debug.LogWarning("TerrenoArena: no hay material y no encuentro el shader de URP.", this);
            }
        }
    }

    private void Generar()
    {
        int n = Mathf.Max(1, _celdas);
        float mitad = n * _tamanoCelda * 0.5f;
        Vector3 origen = transform.position;

        // 1) Alturas de los (n+1)² puntos de la rejilla, con coordenadas del MUNDO:
        // así, más adelante, varios terrenos juntos encajarán en los bordes
        float[] alturas = new float[(n + 1) * (n + 1)];
        for (int z = 0; z <= n; z++)
        {
            for (int x = 0; x <= n; x++)
            {
                float mundoX = origen.x + x * _tamanoCelda - mitad;
                float mundoZ = origen.z + z * _tamanoCelda - mitad;
                alturas[z * (n + 1) + x] = Altura(mundoX, mundoZ);
            }
        }

        Vector3 Punto(int x, int z) => new Vector3(x * _tamanoCelda - mitad, alturas[z * (n + 1) + x], z * _tamanoCelda - mitad);

        // Normal "suave" de un punto de la rejilla: sale de la pendiente entre sus vecinos
        Vector3 NormalSuave(int x, int z)
        {
            int xi = Mathf.Max(x - 1, 0), xd = Mathf.Min(x + 1, n);
            int zi = Mathf.Max(z - 1, 0), zd = Mathf.Min(z + 1, n);

            float dx = (alturas[z * (n + 1) + xd] - alturas[z * (n + 1) + xi]) / ((xd - xi) * _tamanoCelda);
            float dz = (alturas[zd * (n + 1) + x] - alturas[zi * (n + 1) + x]) / ((zd - zi) * _tamanoCelda);
            return new Vector3(-dx, 1f, -dz).normalized;
        }

        // 2) Triángulos SIN vértices compartidos. Cada vértice recibe una normal que mezcla
        // la de su cara (facetado) con la suave (redondo), según _suavizadoNormales
        Vector3[] vertices = new Vector3[n * n * 6];
        Vector3[] normales = new Vector3[n * n * 6];
        int[] indices = new int[n * n * 6];
        int v = 0;

        void Triangulo(int x0, int z0, int x1, int z1, int x2, int z2)
        {
            Vector3 p0 = Punto(x0, z0), p1 = Punto(x1, z1), p2 = Punto(x2, z2);
            Vector3 cara = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            vertices[v] = p0; normales[v] = Vector3.Lerp(cara, NormalSuave(x0, z0), _suavizadoNormales).normalized; v++;
            vertices[v] = p1; normales[v] = Vector3.Lerp(cara, NormalSuave(x1, z1), _suavizadoNormales).normalized; v++;
            vertices[v] = p2; normales[v] = Vector3.Lerp(cara, NormalSuave(x2, z2), _suavizadoNormales).normalized; v++;
        }

        for (int z = 0; z < n; z++)
        {
            for (int x = 0; x < n; x++)
            {
                // La diagonal se alterna en tablero de ajedrez: sin direcciones marcadas en las dunas
                if (((x + z) & 1) == 0)
                {
                    Triangulo(x, z, x, z + 1, x + 1, z + 1);
                    Triangulo(x, z, x + 1, z + 1, x + 1, z);
                }
                else
                {
                    Triangulo(x, z, x, z + 1, x + 1, z);
                    Triangulo(x + 1, z, x, z + 1, x + 1, z + 1);
                }
            }
        }

        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        // 3) El mesh. Con más de 65 535 vértices hace falta el formato de 32 bits
        Mesh malla = new Mesh { name = "TerrenoArena", indexFormat = IndexFormat.UInt32 };
        malla.vertices = vertices;
        malla.triangles = indices;
        malla.normals = normales;       // las nuestras, no RecalculateNormals (que las dejaría facetadas)
        malla.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = malla;
        GetComponent<MeshCollider>().sharedMesh = malla;
        GetComponent<MeshRenderer>().shadowCastingMode = _proyectarSombras ? ShadowCastingMode.On : ShadowCastingMode.Off;
    }

    // Altura del terreno en un punto del mundo
    private float Altura(float x, float z)
    {
        float desfase = _semilla * 0.37f;

        // Perlin devuelve 0-1: cada capa se pasa a -1..1 y se multiplica por su altura
        float grandes = Mathf.PerlinNoise(x * _escalaDunas + desfase, z * _escalaDunas + desfase * 1.7f);
        float medianas = Mathf.PerlinNoise(x * _escalaMedianas + desfase * 0.6f, z * _escalaMedianas + desfase * 2.1f);
        float ondas = Mathf.PerlinNoise(x * _escalaOndulacion + desfase * 2.3f, z * _escalaOndulacion + desfase * 0.9f);

        float altura = (grandes - 0.5f) * 2f * _alturaDunas
                     + (medianas - 0.5f) * 2f * _alturaMedianas
                     + (ondas - 0.5f) * 2f * _alturaOndulacion;

        // Cerca del centro, el terreno se aplana suavemente (la base, la tienda y la salida)
        float dx = x - _centroPlano.x;
        float dz = z - _centroPlano.y;
        float distancia = Mathf.Sqrt(dx * dx + dz * dz);
        float fin = Mathf.Max(_radioTransicion, _radioPlano + 0.1f);
        float mezcla = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_radioPlano, fin, distancia));

        return altura * mezcla;
    }
}