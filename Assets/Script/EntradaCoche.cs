using UnityEngine;
using UnityEngine.InputSystem;

public class EntradaCoche : MonoBehaviour
{
    [SerializeField] private float _velocidadAcelerador = 3f;   // unidades/s
    [SerializeField] private float _velocidadDireccion = 4f;

    public float Acelerador { get; private set; }   // -1 (freno/marcha atrás) a 1 (acelerar)
    public float Direccion { get; private set; }    // -1 (izquierda) a 1 (derecha)

    public bool FrenoMano { get; private set; }
    public bool Turbo { get; private set; }
    private void Update()
    {
        float objetivoAcelerador = 0f;
        float objetivoDireccion = 0f;

        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.wKey.isPressed) objetivoAcelerador += 1f;
            if (teclado.sKey.isPressed) objetivoAcelerador -= 1f;
            if (teclado.dKey.isPressed) objetivoDireccion += 1f;
            if (teclado.aKey.isPressed) objetivoDireccion -= 1f;
        }

        Gamepad mando = Gamepad.current;
        if (mando != null)
        {
            // RT acelera, LT frena/marcha atrás (analógico)
            float gatillos = mando.rightTrigger.ReadValue() - mando.leftTrigger.ReadValue();
            if (Mathf.Abs(gatillos) > Mathf.Abs(objetivoAcelerador)) objetivoAcelerador = gatillos;

            float stick = mando.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > Mathf.Abs(objetivoDireccion)) objetivoDireccion = stick;
        }
        
        bool frenoMano = false;
        bool turbo = false;

        if (teclado != null)
        {
            frenoMano = teclado.spaceKey.isPressed;
            turbo = teclado.leftShiftKey.isPressed;
        }

        if (mando != null)
        {
            frenoMano |= mando.buttonEast.isPressed;    // B (Xbox) / Círculo (PlayStation)
            turbo |= mando.buttonSouth.isPressed;       // A (Xbox) / X (PlayStation)
        }


        FrenoMano = frenoMano;
        Turbo = turbo;
        Acelerador = Mathf.MoveTowards(Acelerador, objetivoAcelerador, _velocidadAcelerador * Time.deltaTime);
        Direccion = Mathf.MoveTowards(Direccion, objetivoDireccion, _velocidadDireccion * Time.deltaTime);
    }
}