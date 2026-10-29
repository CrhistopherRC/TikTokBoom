using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(XRSimpleInteractable))]
public sealed class PuertaHotelVR : MonoBehaviour
{
    [SerializeField] private XRSimpleInteractable interactuable;
    [SerializeField] private BoxCollider hoja;
    [SerializeField] private Renderer indicador;
    [SerializeField] private GameObject resaltado;
    [SerializeField] private float sentidoApertura = -1f;
    [SerializeField, Range(60f, 115f)] private float aperturaMaxima = 100f;
    [SerializeField] private float velocidadAngular = 150f;
    private Rigidbody cuerpo;
    private Transform mano;
    private float angulo;
    private float destino;
    private float anguloManoInicial;
    private float anguloPuertaInicial;
    private bool hovered;
    private int estadoAnterior = -1;
    private readonly Collider[] obstaculos = new Collider[32];
    private MaterialPropertyBlock propiedades;

    public float AnguloActual => angulo;
    public bool Sujetada => mano != null;
    public bool Abierta => Mathf.Abs(angulo) > 70f;
    public XRSimpleInteractable Interactuable => interactuable;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        if (interactuable == null) interactuable = GetComponent<XRSimpleInteractable>();
        propiedades = new MaterialPropertyBlock();
        angulo = destino = 0f;
        transform.localRotation = Quaternion.identity;
        ActualizarIndicador();
    }

    private void OnEnable()
    {
        if (interactuable == null) interactuable = GetComponent<XRSimpleInteractable>();
        interactuable.selectEntered.AddListener(Agarrar);
        interactuable.selectExited.AddListener(Soltar);
        interactuable.hoverEntered.AddListener(EntrarHover);
        interactuable.hoverExited.AddListener(SalirHover);
    }

    private void OnDisable()
    {
        if (interactuable != null)
        {
            interactuable.selectEntered.RemoveListener(Agarrar);
            interactuable.selectExited.RemoveListener(Soltar);
            interactuable.hoverEntered.RemoveListener(EntrarHover);
            interactuable.hoverExited.RemoveListener(SalirHover);
        }
        mano = null;
        hovered = false;
        if (resaltado != null) resaltado.SetActive(false);
    }

    private float AnguloMano(Vector3 posicion)
    {
        Vector3 local = transform.parent != null ? transform.parent.InverseTransformPoint(posicion) : posicion;
        local -= transform.localPosition;
        return Mathf.Atan2(-local.z, local.x) * Mathf.Rad2Deg;
    }

    private void Agarrar(SelectEnterEventArgs args)
    {
        mano = args.interactorObject.GetAttachTransform(interactuable);
        anguloManoInicial = AnguloMano(mano.position);
        anguloPuertaInicial = angulo;
        ActualizarIndicador();
    }

    private void Soltar(SelectExitEventArgs args)
    {
        mano = null;
        if (Mathf.Abs(destino) < 4f) destino = 0f;
        ActualizarIndicador();
    }

    private void EntrarHover(HoverEnterEventArgs args) { hovered = true; ActualizarIndicador(); }
    private void SalirHover(HoverExitEventArgs args) { hovered = interactuable.isHovered; ActualizarIndicador(); }

    private void FixedUpdate()
    {
        if (mano != null)
        {
            float cambio = Mathf.DeltaAngle(anguloManoInicial, AnguloMano(mano.position));
            destino = Mathf.Clamp((anguloPuertaInicial + cambio) * sentidoApertura, 0f, aperturaMaxima) * sentidoApertura;
        }
        float siguiente = Mathf.MoveTowards(angulo, destino, velocidadAngular * Time.fixedDeltaTime);
        if (!Mathf.Approximately(siguiente, angulo) && !JugadorEnBarrido(siguiente))
        {
            angulo = siguiente;
            Quaternion padre = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            cuerpo.MoveRotation(padre * Quaternion.Euler(0f, angulo, 0f));
        }
        ActualizarIndicador();
    }

    private bool JugadorEnBarrido(float candidato)
    {
        if (hoja == null) return false;
        Quaternion padre = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        Quaternion rotacion = padre * Quaternion.Euler(0f, candidato, 0f);
        Vector3 local = hoja.transform.localPosition + hoja.transform.localRotation * Vector3.Scale(hoja.center, hoja.transform.localScale);
        Vector3 centro = transform.position + rotacion * Vector3.Scale(local, transform.lossyScale);
        Vector3 mitad = Vector3.Scale(hoja.size, hoja.transform.lossyScale) * 0.5f + Vector3.one * 0.015f;
        int cantidad = Physics.OverlapBoxNonAlloc(centro, mitad, obstaculos, rotacion * hoja.transform.localRotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
            if (obstaculos[i] is CharacterController jugador && jugador.enabled && jugador.gameObject.activeInHierarchy)
                return true;
        return false;
    }

    private void ActualizarIndicador()
    {
        if (resaltado != null) resaltado.SetActive(hovered || mano != null);
        int estado = mano != null ? 2 : Mathf.Abs(angulo) > 12f ? 1 : 0;
        if (estado == estadoAnterior || indicador == null || propiedades == null) return;
        estadoAnterior = estado;
        Color color = estado == 2 ? new Color(.25f, 1f, .95f) : estado == 1 ? new Color(.2f, .9f, .55f) : new Color(.95f, .55f, .2f);
        indicador.GetPropertyBlock(propiedades);
        propiedades.SetColor("_BaseColor", color);
        propiedades.SetColor("_EmissionColor", color * 1.5f);
        indicador.SetPropertyBlock(propiedades);
    }

    [ContextMenu("Abrir puerta")]
    private void AbrirDesdeInspector() => EstablecerAbierta(true);

    [ContextMenu("Cerrar puerta")]
    private void CerrarDesdeInspector() => EstablecerAbierta(false);

    public void EstablecerAbierta(bool abrir)
    {
        if (!Application.isPlaying) return;
        destino = abrir ? aperturaMaxima * sentidoApertura : 0f;
    }
}
