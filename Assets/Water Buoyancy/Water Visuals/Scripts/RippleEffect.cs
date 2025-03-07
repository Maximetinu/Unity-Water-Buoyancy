using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class RippleEffect : MonoBehaviour
{
    public WaterVolume waterVolume;
    public GameObject rippleParticlesPrefab;
    public GameObject splashParticlesPrefab;

    List<ParticleSystem> ripplesInsideWater = new List<ParticleSystem>();

    // Referencies
    BoxCollider waterTrigger;
    Renderer waterRenderer;
    Camera rippleCamera;
    Bounds waterBounds;
    RenderTexture rippleTexture;

    void Start()
    {
        InitReferences();
        InitCameraPosition();
        InitCameraSettings();
        InitTriggerCallbacks();
        InitShader();
    }

    void InitReferences()
    {
        waterTrigger = waterVolume.GetComponent<BoxCollider>();
        waterRenderer = waterVolume.GetComponent<Renderer>();
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
        transform.position = waterBounds.center + Vector3.up * 5; // 5 as Max Wave Height, more will surpass camera near plane 
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void InitCameraSettings()
    {
        Camera c = GetComponent<Camera>();
        c.orthographicSize = GetMaxWaterExtent(waterBounds);
        c.targetTexture = rippleTexture;
        c.farClipPlane = 10f;
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

    void InitTriggerCallbacks()
    {
        var callbacks = waterTrigger.gameObject.AddComponent<TriggerCallbacks>();
        callbacks.onTriggerEnter += OnTriggerEnter;
        callbacks.onTriggerExit += OnTriggerExit;
    }

    void Update()
    {
        waterRenderer.material.SetVector("_Position", transform.position);

        foreach (ParticleSystem ripple in ripplesInsideWater)
        {
            Vector3 nearestSurfacePoint = ripple.transform.position;
            nearestSurfacePoint.y = waterVolume.GetWaterLevel(nearestSurfacePoint);
            if (ripple.GetComponentInParent<Collider>().bounds.Contains(nearestSurfacePoint))
            {
                if (!ripple.isPlaying)
                {
                    ripple.Play();
                }
            }
            else
            {
                if (ripple.isPlaying)
                {
                    ripple.Stop();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
            return;

        ParticleSystem particles;
        particles = FindRippleParticles(other.transform);

        if (other.attachedRigidbody.velocity.magnitude >= 0.5f)
        {
            Instantiate(splashParticlesPrefab, other.transform.position, Quaternion.identity);
        }

        if (!particles)
        {
            particles = Instantiate(rippleParticlesPrefab, other.transform).GetComponent<ParticleSystem>();
            particles.gameObject.name = rippleParticlesPrefab.name;
            particles.Stop();
        }

        ripplesInsideWater.Add(particles);

    }

    void OnTriggerExit(Collider other)
    {
        if (other.isTrigger)
            return;

        ParticleSystem particles;
        particles = FindRippleParticles(other.transform);

        if (particles && particles.isPlaying)
        {
            particles.Stop();
        }

        ripplesInsideWater.Remove(particles);
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