using UnityEngine;
using UnityEngine.InputSystem;

public class EntradaCoche : MonoBehaviour
{
    [SerializeField] private float _velocidadAcelerador = 3f;
    [SerializeField] private float _velocidadDireccion = 4f;

    public float Acelerador { get; private set; }
    public float Direccion { get; private set; }

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
            // Gatillos analógicos: RT acelera, LT frena/marcha atrás
            float gatillos = mando.rightTrigger.ReadValue() - mando.leftTrigger.ReadValue();
            if (Mathf.Abs(gatillos) > Mathf.Abs(objetivoAcelerador)) objetivoAcelerador = gatillos;

            float stick = mando.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > Mathf.Abs(objetivoDireccion)) objetivoDireccion = stick;
        }

        Acelerador = Mathf.MoveTowards(Acelerador, objetivoAcelerador, _velocidadAcelerador * Time.deltaTime);
        Direccion = Mathf.MoveTowards(Direccion, objetivoDireccion, _velocidadDireccion * Time.deltaTime);
    }
}