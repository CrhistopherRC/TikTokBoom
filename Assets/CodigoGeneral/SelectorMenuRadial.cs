
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

[RequireComponent(typeof(CanvasGroup))]
public class SelectorMenuRadial : MonoBehaviour
{
    [Header("CONTROLADOR VR")]
    public Transform manoDerecha;

    [Header("OPCIONES DEL MENU")]
    public Image[] opciones;
    public Image centroMenu;

    [Header("COLORES")]
    public Color colorSeleccionado = Color.cyan;
    public Color colorConfirmado = Color.green;
    public Color colorSinSeleccion = Color.red;

    [Header("DETECCION DE LA MANO")]
    public float distanciaMaxima = 0.25f;

    [Range(0f, 0.9f)]
    public float proporcionCentro = 0.34f;

    [Header("ANIMACION")]
    public float velocidadAnimacion = 8f;
    public float escalaCerrada = 0.8f;

    private CanvasGroup grupoCanvas;
    private Vector3 escalaOriginal;
    private Color[] coloresOriginales;
    private Color colorCentroOriginal;

    private bool menuAbierto = false;
    private bool botonBAnterior = false;
    private bool botonXAnterior = false;

    private int opcionActual = -1;
    private float tiempoRestaurarCentro = 0f;

    void Awake()
    {
        grupoCanvas = GetComponent<CanvasGroup>();

        escalaOriginal = transform.localScale;

        grupoCanvas.alpha = 0f;
        grupoCanvas.interactable = false;
        grupoCanvas.blocksRaycasts = false;

        transform.localScale =
            escalaOriginal * escalaCerrada;
    }

    void Start()
    {
        if (manoDerecha == null ||
            opciones == null ||
            opciones.Length == 0 ||
            opciones[0] == null)
        {
            Debug.LogError(
                "Falta configurar la mano derecha o las opciones."
            );

            enabled = false;
            return;
        }

        coloresOriginales = new Color[opciones.Length];

        for (int i = 0; i < opciones.Length; i++)
        {
            if (opciones[i] != null)
                coloresOriginales[i] = opciones[i].color;
        }

        if (centroMenu != null)
            colorCentroOriginal = centroMenu.color;

        Debug.Log("Menu radial iniciado correctamente.");
    }

    void Update()
    {
        InputDevice derecho =
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        InputDevice izquierdo =
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        bool botonB = false;
        bool botonX = false;

        if (derecho.isValid)
        {
            derecho.TryGetFeatureValue(
                CommonUsages.secondaryButton,
                out botonB
            );
        }

        if (izquierdo.isValid)
        {
            izquierdo.TryGetFeatureValue(
                CommonUsages.primaryButton,
                out botonX
            );
        }

        // BOTON B: ABRIR Y CERRAR
        if (botonB && !botonBAnterior)
        {
            AlternarMenu();
        }

        botonBAnterior = botonB;

        // DETECTAR LA POSICION DEL MANDO
        if (menuAbierto)
        {
            DetectarMano();
        }

        // BOTON X: CONFIRMAR
        if (botonX && !botonXAnterior)
        {
            Debug.Log("BOTON X DETECTADO");

            if (menuAbierto)
            {
                if (opcionActual >= 0)
                {
                    ConfirmarSeleccion();
                }
                else
                {
                    Debug.LogWarning(
                        "X funciona, pero no hay opcion seleccionada."
                    );

                    MostrarColorCentro(colorSinSeleccion);
                }
            }
            else
            {
                Debug.Log("X detectado, menu cerrado.");
            }
        }

        botonXAnterior = botonX;

        // RESTAURAR COLOR DEL CENTRO
        if (centroMenu != null &&
            tiempoRestaurarCentro > 0f &&
            Time.time >= tiempoRestaurarCentro)
        {
            centroMenu.color = colorCentroOriginal;
            tiempoRestaurarCentro = 0f;
        }

        AnimarMenu();
    }

    void AlternarMenu()
    {
        menuAbierto = !menuAbierto;

        grupoCanvas.interactable = menuAbierto;
        grupoCanvas.blocksRaycasts = menuAbierto;

        if (!menuAbierto)
        {
            CambiarSeleccion(-1);
        }

        Debug.Log(
            menuAbierto ? "MENU ABIERTO" : "MENU CERRADO"
        );
    }

    void DetectarMano()
    {
        RectTransform rect =
            opciones[0].rectTransform;

        float distanciaPlano = Mathf.Abs(
            Vector3.Dot(
                manoDerecha.position - rect.position,
                rect.forward
            )
        );

        if (distanciaPlano > distanciaMaxima)
        {
            CambiarSeleccion(-1);
            return;
        }

        Vector3 puntoLocal =
            rect.InverseTransformPoint(
                manoDerecha.position
            );

        Vector2 posicion = new Vector2(
            puntoLocal.x - rect.rect.center.x,
            puntoLocal.y - rect.rect.center.y
        );

        float radioExterior = Mathf.Min(
            rect.rect.width,
            rect.rect.height
        ) / 2f;

        float radioInterior =
            radioExterior * proporcionCentro;

        float distanciaCentro = posicion.magnitude;

        if (distanciaCentro < radioInterior ||
            distanciaCentro > radioExterior)
        {
            CambiarSeleccion(-1);
            return;
        }

        float angulo = Mathf.Atan2(
            posicion.x,
            posicion.y
        ) * Mathf.Rad2Deg;

        if (angulo < 0)
            angulo += 360f;

        int cantidad = opciones.Length;

        int seleccion = Mathf.FloorToInt(
            angulo / (360f / cantidad)
        );

        seleccion = Mathf.Clamp(
            seleccion,
            0,
            cantidad - 1
        );

        CambiarSeleccion(seleccion);
    }

    void CambiarSeleccion(int nuevaSeleccion)
    {
        if (opcionActual == nuevaSeleccion)
            return;

        opcionActual = nuevaSeleccion;

        for (int i = 0; i < opciones.Length; i++)
        {
            if (opciones[i] != null)
            {
                opciones[i].color =
                    i == opcionActual
                    ? colorSeleccionado
                    : coloresOriginales[i];
            }
        }
    }

    void ConfirmarSeleccion()
    {
        Debug.Log(
            "OPCION CONFIRMADA CON X: " +
            (opcionActual + 1)
        );

        // Mostrar confirmacion visual
        MostrarColorCentro(colorConfirmado);

        // Las funciones reales se agregaran despues.
    }

    void MostrarColorCentro(Color nuevoColor)
    {
        if (centroMenu == null)
            return;

        centroMenu.color = nuevoColor;

        tiempoRestaurarCentro =
            Time.time + 0.7f;
    }

    void AnimarMenu()
    {
        float transparenciaObjetivo =
            menuAbierto ? 1f : 0f;

        grupoCanvas.alpha = Mathf.MoveTowards(
            grupoCanvas.alpha,
            transparenciaObjetivo,
            Time.deltaTime * velocidadAnimacion
        );

        Vector3 escalaObjetivo = menuAbierto
            ? escalaOriginal
            : escalaOriginal * escalaCerrada;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidadAnimacion
        );
    }
}
