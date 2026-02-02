/*
 * CameraBackgroundFollower.cs
 * ===========================
 * 
 * Camera Background Follower System
 * 
 * Makes a background plane always follow the camera and stay centered in view.
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// Makes a background plane follow the camera at fixed distance
    /// </summary>
    public class CameraBackgroundFollower : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Distance from camera")]
        public float distanceFromCamera = 50f;
        
        [Tooltip("Size of background plane")]
        public float planeSize = 100f;
        
        private Camera mainCamera;
        
        void Start()
        {
            mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return;
            }
            
            // Set initial size
            transform.localScale = new Vector3(planeSize, planeSize, 1f);
        }
        
        void LateUpdate()
        {
            if (mainCamera == null) return;
            
            // Position: behind camera at fixed distance
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
            transform.position = targetPosition;
            
            // Rotation: face the camera
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Flip to face camera (Quad faces backward by default)
        }
    }
}

