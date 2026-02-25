using System;
using UnityEngine;

namespace Reloader.Player
{
    [Serializable]
    public class PlayerMovementSettings
    {
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _sprintSpeed = 7.2f;
        [SerializeField] private float _acceleration = 28f;
        [SerializeField] private float _gravity = -25f;
        [SerializeField] private float _jumpHeight = 1.25f;
        [SerializeField] private float _jumpBufferTime = 0.15f;
        [SerializeField] private float _groundedSnapVelocity = -2f;

        public float WalkSpeed
        {
            get => _walkSpeed;
            set => _walkSpeed = Mathf.Max(0f, value);
        }

        public float SprintSpeed
        {
            get => _sprintSpeed;
            set => _sprintSpeed = Mathf.Max(0f, value);
        }

        public float Acceleration
        {
            get => _acceleration;
            set => _acceleration = Mathf.Max(0f, value);
        }

        public float Gravity
        {
            get => _gravity;
            set => _gravity = Mathf.Min(0f, value);
        }

        public float JumpHeight
        {
            get => _jumpHeight;
            set => _jumpHeight = Mathf.Max(0f, value);
        }

        public float JumpBufferTime
        {
            get => _jumpBufferTime;
            set => _jumpBufferTime = Mathf.Max(0f, value);
        }

        public float GroundedSnapVelocity
        {
            get => _groundedSnapVelocity;
            set => _groundedSnapVelocity = Mathf.Min(0f, value);
        }
    }
}
