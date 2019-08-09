using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RippleEffect : MonoBehaviour
{
    public Renderer waterRenderer;
    public GameObject rippleParticlesPrefab;

    // Referencies
    Camera rippleCamera;
    Bounds waterBounds;
    RenderTexture rippleTexture;

    void Awake()
    {
        InitReferences();
        InitCameraPosition();
        InitCameraSettings();
        InitShader();
        InitRippleTrigger();
    }

    void InitReferences()
    {
        rippleCamera = GetComponent<Camera>();
        waterBounds = waterRenderer.bounds;
        rippleTexture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGBHalf);
        rippleTexture.Create();
    }

    void InitShader()
    {
        waterRenderer.material.SetTexture("_MaskInt", rippleTexture);
        waterRenderer.material.SetTexture("_GlobalEffectRT", rippleTexture);
        waterRenderer.material.SetFloat("_OrthographicCamSize", rippleCamera.orthographicSize);
    }

    void InitCameraPosition()
    {
        transform.SetParent(waterRenderer.transform);
        transform.position = waterBounds.center + Vector3.up;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void InitCameraSettings()
    {
        Camera c = GetComponent<Camera>();
        c.orthographicSize = GetMaxWaterExtent(waterBounds);
        c.targetTexture = rippleTexture;
        c.farClipPlane = 2f;
    }

    void InitRippleTrigger()
    {
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;

        Vector3 waterBoundsExtentsAdaptedToRotation = new Vector3(2f * waterBounds.extents.z, 2f * waterBounds.extents.x, 0.05f);
        box.size = waterBoundsExtentsAdaptedToRotation;

        Vector3 boxCenterAdaptedToRotation = Vector3.forward;
        box.center = boxCenterAdaptedToRotation;
    }

    float GetMaxWaterExtent(Bounds waterBounds)
    {
        float maxExtent = waterBounds.extents.x;
        if (waterBounds.extents.z > maxExtent)
        {
            maxExtent = waterBounds.extents.z;
        }
        return maxExtent;
    }

    void OnTriggerEnter(Collider other)
    {
        ParticleSystem particles;
        particles = FindRippleParticles(other.transform);

        if (particles)
        {
            particles.Play();
        }
        else
        {
            particles = Instantiate(rippleParticlesPrefab, other.transform).GetComponent<ParticleSystem>();
            particles.gameObject.name = rippleParticlesPrefab.name;
        }
    }

    void OnTriggerExit(Collider other)
    {
        ParticleSystem particles;
        particles = FindRippleParticles(other.transform);

        if (particles)
        {
            particles.Stop();
        }
    }

    ParticleSystem FindRippleParticles(Transform t)
    {
        ParticleSystem found;

        Transform transformFound = t.Find(rippleParticlesPrefab.name);

        if (transformFound)
        {
            found = transformFound.GetComponent<ParticleSystem>();
        }
        else
        {
            found = null;
        }

        return found;
    }
}