using UnityEngine;

namespace WaterBuoyancy
{
    public class WaterWaves : MonoBehaviour
    {
        [SerializeField]
        private float speed = 1f;

        [SerializeField]
        private float height = 0.2f;

        private Mesh mesh;
        private Vector3[] baseVertices;
        private Vector3[] vertices;

        protected virtual void Awake()
        {
            mesh = GetComponent<MeshFilter>().mesh;
            baseVertices = mesh.vertices;
            vertices = new Vector3[baseVertices.Length];
        }

        protected virtual void Start()
        {
            ResizeBoxCollider();
        }

        protected virtual void Update()
        {
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = baseVertices[i];
                vertex.y +=
                    Mathf.Sin(Time.timeSinceLevelLoad * speed + baseVertices[i].x + baseVertices[i].y + baseVertices[i].z) *
                    (height);

                //vertex.y += Mathf.PerlinNoise(baseVertices[i].x, baseVertices[i].y);

                vertices[i] = vertex;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }

        private void ResizeBoxCollider()
        {
            var boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Vector3 center = boxCollider.center;
                center.y = boxCollider.size.y / -2f;
                center.y += height / transform.localScale.y;

                boxCollider.center = center;
            }
        }
    }
}
