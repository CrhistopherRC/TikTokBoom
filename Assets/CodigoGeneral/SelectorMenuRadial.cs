using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SelectorMenuRadial : MonoBehaviour
{
    [Header("CONTROLADOR VR")]
    public Transform manoDerecha;
    public Transform origenApuntado;

    [Header("OPCIONES DEL MENU")]
    public Image[] opciones;
    public Image centroMenu;
    public Button[] botonesOpciones;
    public TMP_Text[] textosOpciones;
    public TMP_Text textoEstado;
    public UnityEvent<int> alConfirmarOpcion = new UnityEvent<int>();

    [Header("COLORES")]
    public Color colorSeleccionado = Color.cyan;
    public Color colorConfirmado = Color.green;
    public Color colorSinSeleccion = Color.red;

    [Header("APUNTADO")]
    public float distanciaApuntado = 6f;
    [Tooltip("Se conserva por compatibilidad; la detección ahora utiliza el rayo del mando.")]
    public float distanciaMaxima = 0.25f;
    [Range(0f, 0.9f)] public float proporcionCentro = 0.37f;
    [Range(0f, 10f)] public float separacionSectores = 2.16f;
    public LineRenderer rayoApuntado;
    public Transform cursorExterior;
    public IndicadorApuntadoGlobalVR apuntadorGlobal;
    public bool detectarOtrosBotones = true;
    public bool comprobarObstaculos = true;

    [Header("ANIMACION")]
    public float velocidadAnimacion = 8f;
    public float escalaCerrada = 0.8f;

    private CanvasGroup grupoCanvas;
    private RectTransform canvasRect;
    private Vector3 escalaOriginal;
    private Color[] coloresOriginales;
    private Color colorCentroOriginal;
    private bool menuAbierto;
    private bool botonBAnterior;
    private bool botonXAnterior;
    private int opcionActual = -1;
    private float tiempoRestaurarCentro;
    private float tiempoMensaje;
    private Button[] otrosBotones;
    private Button botonApuntado;
    private Ray rayoActual;
    private Vector3 puntoActual;
    private bool puntoEnMenu;

    public bool MenuAbierto => menuAbierto;
    public int OpcionActual => opcionActual;
    public int UltimaOpcionConfirmada { get; private set; } = -1;
    public bool PuntoEnMenu => puntoEnMenu;
    public Vector3 PuntoApuntado => puntoActual;
    public Button BotonApuntado => apuntadorGlobal != null ? apuntadorGlobal.ControlApuntado as Button : botonApuntado;

    private void Awake()
    {
        // An accidentally duplicated component must not hide the cursor's CanvasGroup.
        if (manoDerecha == null || opciones == null || opciones.Length == 0 || opciones[0] == null)
        {
            enabled = false;
            return;
        }
        grupoCanvas = GetComponent<CanvasGroup>();
        canvasRect = GetComponent<RectTransform>();
        escalaOriginal = transform.localScale;
        coloresOriginales = new Color[opciones.Length];
        for (int i = 0; i < opciones.Length; i++)
            if (opciones[i] != null) coloresOriginales[i] = opciones[i].color;
        if (centroMenu != null) colorCentroOriginal = centroMenu.color;
        if (apuntadorGlobal == null)
        {
            apuntadorGlobal = manoDerecha.GetComponent<IndicadorApuntadoGlobalVR>();
            if (apuntadorGlobal == null) apuntadorGlobal = manoDerecha.gameObject.AddComponent<IndicadorApuntadoGlobalVR>();
            apuntadorGlobal.radial = this;
            apuntadorGlobal.origen = origenApuntado != null ? origenApuntado : manoDerecha;
            apuntadorGlobal.rayo = rayoApuntado;
            apuntadorGlobal.cursor = cursorExterior;
            apuntadorGlobal.distanciaMaxima = Mathf.Max(distanciaApuntado, 20f);
        }
        otrosBotones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        grupoCanvas.alpha = 0f;
        grupoCanvas.interactable = false;
        grupoCanvas.blocksRaycasts = false;
        transform.localScale = escalaOriginal * escalaCerrada;
        OcultarApuntado();
    }

    private void Update()
    {
        InputDevice derecho = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        InputDevice izquierdo = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool b = derecho.isValid && derecho.TryGetFeatureValue(CommonUsages.secondaryButton, out bool presionadoB) && presionadoB;
        bool x = izquierdo.isValid && izquierdo.TryGetFeatureValue(CommonUsages.primaryButton, out bool presionadoX) && presionadoX;
        ProcesarEntrada(b, x);
    }

    public void ProcesarEntrada(bool botonB, bool botonX)
    {
        if (grupoCanvas == null) return;
        if (botonB && !botonBAnterior) EstablecerMenuAbierto(!menuAbierto);
        botonBAnterior = botonB;
        ActualizarApuntado();
        if (botonX && !botonXAnterior)
        {
            if (menuAbierto && (apuntadorGlobal == null || puntoEnMenu)) ConfirmarSeleccion();
            else if (apuntadorGlobal == null && botonApuntado != null && botonApuntado.IsInteractable()) botonApuntado.onClick.Invoke();
        }
        botonXAnterior = botonX;
        if (centroMenu != null && tiempoRestaurarCentro > 0 && Time.unscaledTime >= tiempoRestaurarCentro)
        {
            centroMenu.color = colorCentroOriginal;
            tiempoRestaurarCentro = 0f;
        }
        if (tiempoMensaje <= Time.unscaledTime) ActualizarTexto();
        AnimarMenu();
    }

    public void EstablecerMenuAbierto(bool abierto)
    {
        if (grupoCanvas == null) return;
        menuAbierto = abierto;
        grupoCanvas.interactable = abierto;
        grupoCanvas.blocksRaycasts = abierto;
        tiempoMensaje = 0;
        if (!abierto) CambiarSeleccion(-1);
        CambiarBoton(null);
        puntoEnMenu = false;
        ActualizarTexto();
    }

    private Ray ObtenerRayo()
    {
        Transform origen = origenApuntado != null ? origenApuntado : manoDerecha;
        return new Ray(origen.position, origen.forward);
    }

    private bool Interseccion(RectTransform rect, Ray ray, out Vector3 punto, out float distancia)
    {
        punto = Vector3.zero;
        distancia = 0;
        Plane plano = new Plane(rect.forward, rect.TransformPoint(rect.rect.center));
        if (!plano.Raycast(ray, out distancia) || distancia <= 0 || distancia > distanciaApuntado) return false;
        punto = ray.GetPoint(distancia);
        if (comprobarObstaculos && Physics.Raycast(ray, out RaycastHit obstaculo, distancia, ~0, QueryTriggerInteraction.Ignore)
            && obstaculo.distance < distancia - .015f) return false;
        return true;
    }

    public void ActualizarApuntado()
    {
        if (manoDerecha == null || canvasRect == null) return;
        rayoActual = ObtenerRayo();
        puntoActual = rayoActual.GetPoint(distanciaApuntado);
        puntoEnMenu = false;
        if (menuAbierto)
        {
            CambiarBoton(null);
            int seleccion = -1;
            if (Interseccion(canvasRect, rayoActual, out Vector3 hit, out _))
            {
                Vector3 local = canvasRect.InverseTransformPoint(hit);
                RectTransform referencia = opciones[0].rectTransform;
                Vector3 centro = canvasRect.InverseTransformPoint(referencia.TransformPoint(referencia.rect.center));
                Vector2 posicion = new Vector2(local.x - centro.x, local.y - centro.y);
                float radio = Mathf.Min(referencia.rect.width, referencia.rect.height) * .5f;
                float distancia = posicion.magnitude;
                if (distancia <= radio)
                {
                    puntoActual = hit;
                    puntoEnMenu = true;
                    if (distancia >= radio * proporcionCentro)
                    {
                        // Sectors are measured in the Canvas, independent of decorative Image rotation.
                        float angulo = Mathf.Repeat(Mathf.Atan2(posicion.x, posicion.y) * Mathf.Rad2Deg, 360f);
                        float tamanoSector = 360f / opciones.Length;
                        float dentroSector = angulo % tamanoSector;
                        int indice = Mathf.FloorToInt(angulo / tamanoSector);
                        bool habilitada = botonesOpciones == null || indice >= botonesOpciones.Length || botonesOpciones[indice] == null
                            || botonesOpciones[indice].IsInteractable();
                        if (habilitada && dentroSector >= separacionSectores && dentroSector <= tamanoSector - separacionSectores)
                            seleccion = Mathf.Clamp(indice, 0, opciones.Length - 1);
                    }
                }
            }
            CambiarSeleccion(seleccion);
        }
        else
        {
            CambiarSeleccion(-1);
            if (apuntadorGlobal != null)
            {
                CambiarBoton(null);
                return;
            }
            Button cercano = null;
            float minima = distanciaApuntado;
            if (detectarOtrosBotones && otrosBotones != null)
                foreach (Button button in otrosBotones)
                {
                    if (button == null || !button.isActiveAndEnabled || !button.IsInteractable() || button.transform.IsChildOf(transform)) continue;
                    Canvas canvas = button.GetComponentInParent<Canvas>();
                    if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) continue;
                    CanvasGroup group = button.GetComponentInParent<CanvasGroup>();
                    if (group != null && (!group.interactable || group.alpha < .01f)) continue;
                    RectTransform rect = button.GetComponent<RectTransform>();
                    if (!Interseccion(rect, rayoActual, out Vector3 hit, out float distance) || distance >= minima) continue;
                    Vector3 local = rect.InverseTransformPoint(hit);
                    if (!rect.rect.Contains(new Vector2(local.x, local.y))) continue;
                    cercano = button;
                    minima = distance;
                    puntoActual = hit;
                }
            CambiarBoton(cercano);
        }
        ActualizarVisualRayo();
    }

    private void CambiarSeleccion(int indice)
    {
        if (indice == opcionActual) return;
        opcionActual = indice;
        for (int i = 0; i < opciones.Length; i++)
            if (opciones[i] != null) opciones[i].color = i == opcionActual ? colorSeleccionado : coloresOriginales[i];
        if (Time.unscaledTime >= tiempoMensaje) ActualizarTexto();
    }

    private void CambiarBoton(Button button)
    {
        if (button == botonApuntado) return;
        if (EventSystem.current != null)
        {
            var data = new PointerEventData(EventSystem.current);
            if (botonApuntado != null) botonApuntado.OnPointerExit(data);
            if (button != null) button.OnPointerEnter(data);
        }
        botonApuntado = button;
    }

    public bool ConfirmarSeleccion()
    {
        if (!menuAbierto || opcionActual < 0 || opcionActual >= opciones.Length)
        {
            if (menuAbierto) MostrarColorCentro(colorSinSeleccion);
            return false;
        }
        int confirmado = opcionActual;
        UltimaOpcionConfirmada = confirmado;
        MostrarColorCentro(colorConfirmado);
        tiempoMensaje = Time.unscaledTime + .7f;
        if (textoEstado != null) textoEstado.text = "SELECCIONADA: " + NombreOpcion(confirmado);
        if (botonesOpciones != null && confirmado < botonesOpciones.Length && botonesOpciones[confirmado] != null
            && botonesOpciones[confirmado].IsInteractable()) botonesOpciones[confirmado].onClick.Invoke();
        alConfirmarOpcion.Invoke(confirmado);
        return true;
    }

    private string NombreOpcion(int i)
    {
        return textosOpciones != null && i < textosOpciones.Length && textosOpciones[i] != null
            ? textosOpciones[i].text : "OPCIÓN " + (i + 1);
    }

    private void ActualizarTexto()
    {
        if (textoEstado != null) textoEstado.text = opcionActual >= 0
            ? "APUNTANDO: " + NombreOpcion(opcionActual) + "   ·   X CONFIRMAR" : "APUNTA A UNA OPCIÓN CON EL MANDO DERECHO";
    }

    private void MostrarColorCentro(Color color)
    {
        if (centroMenu == null) return;
        centroMenu.color = color;
        tiempoRestaurarCentro = Time.unscaledTime + .7f;
    }

    private void ActualizarVisualRayo()
    {
        if (apuntadorGlobal != null) return;
        bool visible = menuAbierto || botonApuntado != null;
        Color color = opcionActual >= 0 || botonApuntado != null ? Color.cyan : new Color(.7f, .8f, .9f, .7f);
        if (rayoApuntado != null)
        {
            rayoApuntado.enabled = visible;
            if (visible)
            {
                rayoApuntado.SetPosition(0, rayoActual.origin);
                rayoApuntado.SetPosition(1, puntoActual);
                rayoApuntado.startColor = color;
                rayoApuntado.endColor = color;
            }
        }
        if (cursorExterior != null)
        {
            cursorExterior.gameObject.SetActive(!menuAbierto && botonApuntado != null);
            cursorExterior.position = puntoActual - rayoActual.direction * .004f;
            cursorExterior.localScale = Vector3.one * Mathf.Clamp(Vector3.Distance(rayoActual.origin, puntoActual) * .008f, .005f, .025f);
        }
    }

    private void AnimarMenu()
    {
        grupoCanvas.alpha = Mathf.MoveTowards(grupoCanvas.alpha, menuAbierto ? 1f : 0f, Time.unscaledDeltaTime * velocidadAnimacion);
        transform.localScale = Vector3.Lerp(transform.localScale, menuAbierto ? escalaOriginal : escalaOriginal * escalaCerrada,
            Time.unscaledDeltaTime * velocidadAnimacion);
    }

    private void OcultarApuntado()
    {
        if (apuntadorGlobal != null) return;
        if (rayoApuntado != null) rayoApuntado.enabled = false;
        if (cursorExterior != null) cursorExterior.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CambiarBoton(null);
        OcultarApuntado();
    }
}
