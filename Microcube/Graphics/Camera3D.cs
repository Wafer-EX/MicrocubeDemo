﻿using Silk.NET.Maths;

namespace Microcube.Graphics
{
    /// <summary>
    /// Represents a 3D camera that will look at target. Can move slowly.
    /// </summary>
    public class Camera3D
    {
        private Vector3D<float> position;
        private Vector3D<float> target;
        private Vector3D<float> intermediatePosition;
        private Vector3D<float> intermediateTarget;

        /// <summary>
        /// Position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get => intermediatePosition;
            set => position = value;
        }

        /// <summary>
        /// Position of the target.
        /// </summary>
        public Vector3D<float> Target
        {
            get => intermediateTarget;
            set => target = value;
        }

        /// <summary>
        /// Field of view of the camera.
        /// </summary>
        public float FieldOfView { get; set; }

        /// <summary>
        /// Aspect ratio of the camera.
        /// </summary>
        public float AspectRatio { get; set; }

        /// <summary>
        /// Moving speed of the camera. If zero, camera moves instantly.
        /// </summary>
        public float MovingSpeed { get; set; }

        public Camera3D(Vector3D<float> position, Vector3D<float> target, float fieldOfView, float aspectRatio, float movingSpeed = 0.0f)
        {
            Position = position;
            Target = target;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
            MovingSpeed = movingSpeed;
        }

        /// <summary>
        /// Get projection matrix to use it in a shader.
        /// </summary>
        /// <returns>Projection matrix</returns>
        public Matrix4X4<float> GetProjectionMatrix()
        {
            return Matrix4X4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, 0.1f, 100.0f);
        }

        /// <summary>
        /// Get view matrix to use it in a shader.
        /// </summary>
        /// <returns>View matrix</returns>
        public Matrix4X4<float> GetViewMatrix()
        {
            return Matrix4X4.CreateLookAt(Position, Target, Vector3D<float>.UnitY);
        }

        /// <summary>
        /// Updates the camera using moving speed.
        /// </summary>
        /// <param name="deltaTime">Time of the frame.</param>
        public void Update(float deltaTime)
        {
            if (MovingSpeed > 0.0f)
            {
                intermediatePosition += (position - intermediatePosition) * MovingSpeed * deltaTime;
                intermediateTarget += (target - intermediateTarget) * MovingSpeed * deltaTime;
            }
            else
            {
                intermediatePosition = position;
                intermediateTarget = target;
            }
        }
    }
}