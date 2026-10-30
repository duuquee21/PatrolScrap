using UnityEngine;

// Sprites de interfaz generados por código: un círculo y un rectángulo redondeado (9-slice).
// Se crean la primera vez que se piden y se reutilizan después.
public static class UiSprites
{
    private static Sprite _circulo;
    private static Sprite _redondeado;

    public static Sprite Circulo
    {
        get
        {
            if (_circulo == null) _circulo = CrearCirculo(128);
            return _circulo;
        }
    }

    // Úsalo con Image.type = Sliced: las esquinas no se deforman al cambiar el tamaño
    public static Sprite Redondeado
    {
        get
        {
            if (_redondeado == null) _redondeado = CrearRedondeado(64, 20);
            return _redondeado;
        }
    }

    private static Sprite CrearCirculo(int tam)
    {
        Texture2D textura = NuevaTextura(tam);
        Color32[] pixeles = new Color32[tam * tam];

        float centro = tam * 0.5f;
        float radio = centro - 1f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = x + 0.5f - centro;
                float dy = y + 0.5f - centro;
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);

                // El borde se suaviza en un píxel: así no se ven dientes de sierra
                byte alfa = (byte)(Mathf.Clamp01(radio - distancia + 0.5f) * 255f);
                pixeles[y * tam + x] = new Color32(255, 255, 255, alfa);
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply();
        return Sprite.Create(textura, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CrearRedondeado(int tam, int radio)
    {
        Texture2D textura = NuevaTextura(tam);
        Color32[] pixeles = new Color32[tam * tam];

        float mitad = tam * 0.5f;
        float interior = mitad - radio;   // la parte recta: solo las esquinas se redondean

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - mitad) - interior, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - mitad) - interior, 0f);
                float distancia = Mathf.Sqrt(dx * dx + dy * dy);

                byte alfa = (byte)(Mathf.Clamp01(radio - distancia + 0.5f) * 255f);
                pixeles[y * tam + x] = new Color32(255, 255, 255, alfa);
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply();

        // El borde (radio por lado) es lo que se mantiene sin estirar al usar Sliced
        return Sprite.Create(textura, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radio, radio, radio, radio));
    }

    private static Texture2D NuevaTextura(int tam)
    {
        return new Texture2D(tam, tam, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
    }
}