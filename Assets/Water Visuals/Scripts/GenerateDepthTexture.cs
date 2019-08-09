using UnityEngine;

namespace WaterBuoyancy
{
    [RequireComponent(typeof(Camera))]
    public class GenerateDepthTexture : MonoBehaviour
    {
        public DepthTextureMode mode;
        private void OnValidate() => GetComponent<Camera>().depthTextureMode = mode;
    }
}

