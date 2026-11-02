using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

[DefaultExecutionOrder(100)]
public sealed class IndicadorApuntadoGlobalVR : MonoBehaviour
{
    public Transform origen;
    public SelectorMenuRadial radial;
    public LineRenderer rayo;
    public Transform cursor;
    public float distanciaMaxima = 20f;
    public float longitudSinImpacto = 4f;
    private Canvas[] interfaces;
    private float siguienteRevision;
    private Selectable control;
    private Selectable arrastrando;
    private PointerEventData datos;
    private EventSystem sistemaEventos;
    private Renderer rendererCursor;
    private MaterialPropertyBlock propiedadesCursor;
    private bool xAnterior;
    private float pulsoHasta;
    private Material materialCreado;
    private Material materialCentro;
    private List<RaycastResult> resultados;

    public Selectable ControlApuntado => control;
    public bool SobreInterfaz { get; private set; }
    public Vector3 PuntoActual { get; private set; }

    private void Start()
    {
        resultados = new List<RaycastResult>();
        propiedadesCursor = new MaterialPropertyBlock();
        if (origen == null) origen = transform;
        AsegurarVisuales();
        RefrescarInterfaces();
    }

    public void RefrescarInterfaces()
    {
        interfaces = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        siguienteRevision = Time.unscaledTime + .2f;
    }

    private void AsegurarVisuales()
    {
        if (rayo == null || cursor == null)
        {
            materialCreado = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            materialCreado.SetColor("_BaseColor", Color.white);
        }
        if (rayo == null)
        {
            var go = new GameObject("RayoInterfazVR", typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            rayo = go.GetComponent<LineRenderer>();
            rayo.sharedMaterial = materialCreado;
            rayo.startWidth = .002f;
            rayo.endWidth = .0012f;
            rayo.numCapVertices = 4;
            rayo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        rayo.useWorldSpace = true;
        rayo.positionCount = 2;
        if (cursor == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PuntoInterfazVR";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.GetComponent<Renderer>().sharedMaterial = materialCreado;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cursor = go.transform;
        }
        rendererCursor = cursor.GetComponent<Renderer>();
        if (cursor.Find("CentroPuntero") == null && cursor.GetComponent<MeshFilter>() != null)
        {
            materialCentro = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            materialCentro.SetColor("_BaseColor", new Color(.015f, .025f, .04f));
            var center = new GameObject("CentroPuntero", typeof(MeshFilter), typeof(MeshRenderer));
            center.transform.SetParent(cursor, false);
            center.transform.localPosition = new Vector3(0, 0, -.42f);
            center.transform.localScale = Vector3.one * .5f;
            center.GetComponent<MeshFilter>().sharedMesh = cursor.GetComponent<MeshFilter>().sharedMesh;
            center.GetComponent<MeshRenderer>().sharedMaterial = materialCentro;
            center.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void Update()
    {
        ActualizarApuntado();
        var device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool x = device.isValid && device.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed;
        ProcesarX(x);
    }

    private bool Visible(Canvas canvas)
    {
        if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace) return false;
        if (radial != null && canvas.transform.IsChildOf(radial.transform)) return false;
        foreach (var group in canvas.GetComponentsInParent<CanvasGroup>())
            if (group.alpha < .01f || !group.blocksRaycasts) return false;
        return true;
    }

    public void ActualizarApuntado()
    {
        if (origen == null || rayo == null || cursor == null) return;
        if (resultados == null) resultados = new List<RaycastResult>();
        if (propiedadesCursor == null) propiedadesCursor = new MaterialPropertyBlock();
        if (interfaces == null || Time.unscaledTime >= siguienteRevision) RefrescarInterfaces();
        Ray ray = new Ray(origen.position, origen.forward);
        float limite = distanciaMaxima;
        bool hitWorld = Physics.Raycast(ray, out RaycastHit obstacle, distanciaMaxima, ~0, QueryTriggerInteraction.Ignore);
        if (hitWorld) limite = obstacle.distance;
        PuntoActual = ray.GetPoint(hitWorld ? limite : Mathf.Min(longitudSinImpacto, distanciaMaxima));
        SobreInterfaz = false;
        Selectable hovered = null;
        bool usaRadial = radial != null && radial.isActiveAndEnabled && radial.MenuAbierto && radial.PuntoEnMenu;
        if (usaRadial)
        {
            PuntoActual = radial.PuntoApuntado;
            SobreInterfaz = true;
        }
        else
        {
            Canvas cercano = null;
            float minimo = limite + .015f;
            Vector3 puntoCanvas = Vector3.zero;
            foreach (Canvas canvas in interfaces)
            {
                if (!Visible(canvas)) continue;
                var rect = canvas.GetComponent<RectTransform>();
                var plane = new Plane(rect.forward, rect.TransformPoint(rect.rect.center));
                if (!plane.Raycast(ray, out float distance) || distance <= 0 || distance > distanciaMaxima || distance > minimo) continue;
                Vector3 point = ray.GetPoint(distance);
                Vector3 local = rect.InverseTransformPoint(point);
                if (!rect.rect.Contains(new Vector2(local.x, local.y))) continue;
                if (cercano != null && Mathf.Abs(distance - minimo) < .002f && canvas.sortingOrder < cercano.sortingOrder) continue;
                cercano = canvas;
                minimo = distance;
                puntoCanvas = point;
            }
            if (cercano != null)
            {
                SobreInterfaz = true;
                PuntoActual = puntoCanvas;
                if (EventSystem.current != null)
                {
                    if (datos == null || sistemaEventos != EventSystem.current)
                    {
                        sistemaEventos = EventSystem.current;
                        datos = new PointerEventData(sistemaEventos);
                    }
                    var camera = cercano.worldCamera != null ? cercano.worldCamera : Camera.main;
                    datos.position = camera != null ? (Vector2)camera.WorldToScreenPoint(puntoCanvas) : Vector2.zero;
                    var caster = cercano.GetComponent<GraphicRaycaster>();
                    resultados.Clear();
                    if (caster != null && caster.isActiveAndEnabled) caster.Raycast(datos, resultados);
                    if (resultados.Count > 0)
                    {
                        // The top graphic decides the target, preserving masks and blocking panels.
                        var result = resultados[0];
                        result.worldPosition = puntoCanvas;
                        datos.pointerCurrentRaycast = result;
                        var selectable = result.gameObject != null ? result.gameObject.GetComponentInParent<Selectable>() : null;
                        if (selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable()) hovered = selectable;
                    }
                }
            }
        }
        CambiarControl(hovered);
        bool accion = hovered != null || usaRadial && radial.OpcionActual >= 0;
        Color color = Time.unscaledTime < pulsoHasta ? new Color(.3f, 1f, .5f) : accion ? Color.cyan : new Color(.85f, .94f, 1f);
        rayo.enabled = true;
        rayo.SetPosition(0, ray.origin);
        rayo.SetPosition(1, PuntoActual);
        rayo.startColor = color;
        rayo.endColor = color;
        // The radial retains its UI cursor; everywhere else uses the same world-space point.
        cursor.gameObject.SetActive(!usaRadial);
        cursor.position = PuntoActual - ray.direction * .005f;
        cursor.rotation = Quaternion.LookRotation(ray.direction);
        cursor.localScale = Vector3.one * Mathf.Clamp(Vector3.Distance(ray.origin, PuntoActual) * .009f, .009f, .09f);
        if (rendererCursor != null)
        {
            propiedadesCursor.SetColor("_BaseColor", Color.white);
            rendererCursor.SetPropertyBlock(propiedadesCursor);
        }
    }

    private void CambiarControl(Selectable siguiente)
    {
        if (control == siguiente) return;
        if (EventSystem.current != null)
        {
            if (datos == null) datos = new PointerEventData(EventSystem.current);
            if (control != null) ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerExitHandler);
            if (siguiente != null) ExecuteEvents.Execute(siguiente.gameObject, datos, ExecuteEvents.pointerEnterHandler);
        }
        control = siguiente;
    }

    public void ProcesarX(bool presionado)
    {
        bool radialActivo = radial != null && radial.isActiveAndEnabled && radial.MenuAbierto && radial.PuntoEnMenu;
        if (presionado && !xAnterior && !radialActivo && control != null && control.IsInteractable() && datos != null)
        {
            datos.button = PointerEventData.InputButton.Left;
            datos.pressPosition = datos.position;
            datos.pointerPressRaycast = datos.pointerCurrentRaycast;
            ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerDownHandler);
            if (control is Slider || control is Scrollbar) arrastrando = control;
            else
            {
                ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(control.gameObject, datos, ExecuteEvents.pointerClickHandler);
            }
            pulsoHasta = Time.unscaledTime + .2f;
        }
        if (presionado && arrastrando != null && datos != null)
        {
            datos.dragging = true;
            ExecuteEvents.Execute(arrastrando.gameObject, datos, ExecuteEvents.dragHandler);
        }
        if (!presionado && arrastrando != null && datos != null)
        {
            ExecuteEvents.Execute(arrastrando.gameObject, datos, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(arrastrando.gameObject, datos, ExecuteEvents.endDragHandler);
            datos.dragging = false;
            arrastrando = null;
        }
        xAnterior = presionado;
    }

    private void OnDisable()
    {
        ProcesarX(false);
        CambiarControl(null);
        if (rayo != null) rayo.enabled = false;
        if (cursor != null) cursor.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (materialCreado != null) Destroy(materialCreado);
        if (materialCentro != null) Destroy(materialCentro);
    }
}
