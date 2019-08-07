using System.Collections.Generic;
using UnityEngine;

namespace WaterBuoyancy
{
    public class BuoyancyMaterial : MonoBehaviour
    {
        [Range(0.1f, 2f)] public float buoyancy = 0.95f;

        [SerializeField] [Range(0f, 1f)] private float normalizedVoxelSize = 0.5f;

        [SerializeField] private float dragInWater = 1f;

        [SerializeField] private float angularDragInWater = 1f;

        private WaterVolume water;
        private new Collider collider;
        private new Rigidbody rigidbody;
        private float initialDrag;
        private float initialAngularDrag;
        private Vector3 voxelSize;
        private Vector3[] voxels;

        private float percentSubmerged = 0f;

        protected virtual void Awake()
        {
            collider = GetComponent<Collider>();

            if (collider == null)
            {
                gameObject.AddComponent<BoxCollider>();
                Debug.LogWarning(string.Format("Buoyancy:Object \"{0}\" had no coll. BoxCollider has been added.", name));
            }

            rigidbody = collider.attachedRigidbody;

            // If true, this object is part of a compound collider
            if (rigidbody.gameObject != collider.gameObject)
            {
                // It won't receive callbacks because has no Rigidbody...
                TriggerCallbacks triggerCallbacks = rigidbody.gameObject.GetComponent<TriggerCallbacks>();
                if (triggerCallbacks == null) triggerCallbacks = rigidbody.gameObject.AddComponent<TriggerCallbacks>();
                triggerCallbacks.onTriggerEnter += OnTriggerEnter;
                triggerCallbacks.onTriggerExit += OnTriggerExit;

                // And it has as much buoyancy as child floating colliders sum, so reduce it if it has been added automatically (done at WaterVolume.cs) ...
            }

            initialDrag = rigidbody.drag;
            initialAngularDrag = rigidbody.angularDrag;
        }

        protected virtual void FixedUpdate()
        {
            if (water != null && voxels.Length > 0)
            {
                Vector3 forceAtSingleVoxel = GetBuoyancyForce() / voxels.Length;
                Bounds bounds = collider.bounds;
                float voxelHeight = bounds.size.y * normalizedVoxelSize;

                for (int i = 0; i < voxels.Length; i++)
                {
                    Vector3 worldPoint = transform.TransformPoint(voxels[i]);

                    float waterLevel = water.GetWaterLevel(worldPoint);
                    float deepLevel = waterLevel - worldPoint.y + (voxelHeight / 2f); // How deep is the voxel                    
                    float submergedFactor = Mathf.Clamp(deepLevel / voxelHeight, 0f, 1f); // 0 - voxel is fully out of the water, 1 - voxel is fully submerged
                    percentSubmerged += submergedFactor;

                    Vector3 surfaceNormal = water.GetSurfaceNormal(worldPoint);
                    Quaternion surfaceRotation = Quaternion.FromToRotation(water.transform.up, surfaceNormal);
                    surfaceRotation = Quaternion.Slerp(surfaceRotation, Quaternion.identity, submergedFactor);

                    Vector3 finalVoxelForce = surfaceRotation * (forceAtSingleVoxel * submergedFactor);
                    rigidbody.AddForceAtPosition(finalVoxelForce, worldPoint);

                    Debug.DrawLine(worldPoint, worldPoint + finalVoxelForce.normalized, Color.blue);
                }

                percentSubmerged /= voxels.Length; // 0 - object is fully out of the water, 1 - object is fully submerged

                rigidbody.drag = Mathf.Lerp(initialDrag, dragInWater, percentSubmerged);
                rigidbody.angularDrag = Mathf.Lerp(initialAngularDrag, angularDragInWater, percentSubmerged);
            }
        }

        // void SetupColliders()
        // {
        //     // The object must have a Collider
        //     colliders = GetComponentsInChildren<Collider>();
        //     if (colliders.Length == 0)
        //     {
        //         colliders = new Collider[1];
        //         colliders[0] = gameObject.AddComponent<BoxCollider>();
        //         Debug.LogError(string.Format("Buoyancy: Object \"{0}\" had no coll. BoxCollider has been added.", name));
        //     }
        // }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<WaterVolume>())
            {
                water = other.GetComponent<WaterVolume>();
                if (voxels == null)
                {
                    voxels = CutIntoVoxels();
                }
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (water && water.gameObject == other.gameObject)
            {
                water = null;
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (voxels != null)
            {
                for (int i = 0; i < voxels.Length; i++)
                {
                    Gizmos.color = Color.magenta - new Color(0f, 0f, 0f, 0.75f);
                    Gizmos.DrawCube(transform.TransformPoint(voxels[i]), voxelSize * 0.8f);
                }
            }
        }

        private Vector3 GetBuoyancyForce()
        {
            return Vector3.up * Mathf.Abs(Physics.gravity.y) * rigidbody.mass * buoyancy;
        }

        private Vector3[] CutIntoVoxels()
        {
            Quaternion initialRotation = transform.rotation;
            transform.rotation = Quaternion.identity;

            Bounds bounds = collider.bounds;
            voxelSize.x = bounds.size.x * normalizedVoxelSize;
            voxelSize.y = bounds.size.y * normalizedVoxelSize;
            voxelSize.z = bounds.size.z * normalizedVoxelSize;
            int voxelsCountForEachAxis = Mathf.RoundToInt(1f / normalizedVoxelSize);
            List<Vector3> voxels = new List<Vector3>(voxelsCountForEachAxis * voxelsCountForEachAxis * voxelsCountForEachAxis);

            for (int i = 0; i < voxelsCountForEachAxis; i++)
            {
                for (int j = 0; j < voxelsCountForEachAxis; j++)
                {
                    for (int k = 0; k < voxelsCountForEachAxis; k++)
                    {
                        float pX = bounds.min.x + voxelSize.x * (0.5f + i);
                        float pY = bounds.min.y + voxelSize.y * (0.5f + j);
                        float pZ = bounds.min.z + voxelSize.z * (0.5f + k);

                        Vector3 point = new Vector3(pX, pY, pZ);
                        if (IsPointInsideCollider(point, collider, ref bounds))
                        {
                            voxels.Add(transform.InverseTransformPoint(point));
                        }
                    }
                }
            }

            transform.rotation = initialRotation;

            return voxels.ToArray();
        }

        // private Bounds ComputeBounds()
        // {
        //     Bounds bounds = new Bounds();
        //     foreach (Collider c in colliders)
        //     {
        //         bounds.Encapsulate(c.bounds);
        //     }
        //     return bounds;
        // }

        private bool IsPointInsideCollider(Vector3 point, Collider collider, ref Bounds colliderBounds)
        {
            float rayLength = colliderBounds.size.magnitude;
            Ray ray = new Ray(point, collider.transform.position - point);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayLength))
            {
                if (hit.collider == collider)
                {
                    return false;
                }
            }

            return true;
        }

        // private bool IsPointInsideAnyCollider(Vector3 point, Collider[] colliders, ref Bounds colliderBounds)
        // {
        //     foreach (Collider c in colliders)
        //     {
        //         if (IsPointInsideCollider(point, c, ref colliderBounds))
        //         {
        //             return true;
        //         }
        //     }

        //     return false;
        // }
    }
}
