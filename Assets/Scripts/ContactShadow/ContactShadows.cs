using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    [Serializable]
    public class ContactShadows
    {
        /// <summary>
        /// When enabled, HDRP processes Contact Shadows for this Volume.
        /// </summary>
        public bool enable = true;
        /// <summary>
        /// Controls the length of the rays HDRP uses to calculate Contact Shadows. It is in meters, but it gets scaled by a factor depending on Distance Scale Factor
        /// and the depth of the point from where the contact shadow ray is traced.
        /// </summary>
        [Range(0f, 1f)]
        public float length = 0.15f;
        /// <summary>
        /// Controls the opacity of the contact shadows.
        /// </summary>
        [Range(0f, 1f)]
        public float opacity = 1.0f;
        /// <summary>
        /// Scales the length of the contact shadow ray based on the linear depth value at the origin of the ray.
        /// </summary>
        [Range(0f, 1f)]
        public float distanceScaleFactor = 0.5f;
        /// <summary>
        /// The distance from the camera, in meters, at which HDRP begins to fade out Contact Shadows.
        /// </summary>
        [Min(0f)]
        public float maxDistance = 50.0f;
        /// <summary>
        /// The distance from the camera, in meters, at which HDRP begins to fade in Contact Shadows.
        /// </summary>
        [Min(0f)]
        public float minDistance = 0.0f;
        /// <summary>
        /// The distance, in meters, over which HDRP fades Contact Shadows out when past the Max Distance.
        /// </summary>
        [Min(0f)]
        public float fadeDistance = 5.0f;
        /// <summary>
        /// The distance, in meters, over which HDRP fades Contact Shadows in when past the Min Distance.
        /// </summary>
        [Min(0f)]
        public float fadeInDistance = 0;
        /// <summary>
        /// Controls the bias applied to the screen space ray cast to get contact shadows.
        /// </summary>
        [Range(0f, 1f)]
        public float rayBias = 0.2f;
        /// <summary>
        /// Controls the thickness of the objects found along the ray, essentially thickening the contact shadows.
        /// </summary>
        [Range(0.02f, 1f)]
        public float thicknessScale = 0.15f;
        /// <summary>
        /// Controls the numbers of samples taken during the ray-marching process for shadows. Increasing this might lead to higher quality at the expenses of performance.
        /// </summary>
        [Range(8, 64)]
        public int sampleCount = 8;

    }
}
