using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerMovement : NetworkBehaviour
{
    
    [field: SerializeField] public InputReader InputReader { get; private set; }
    [field: SerializeField] public CharacterController CharacterController { get; private set; }
    [field: SerializeField] public float Speed { get; private set; }
    [field: SerializeField] public float RotationSmoothValue { get; private set; }
    private Transform _mainCamera;
    [SerializeField] private Transform cameraOffset;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            if (Camera.main != null) _mainCamera = Camera.main.transform;

            _mainCamera.position = cameraOffset.position;
            _mainCamera.rotation = cameraOffset.rotation;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return; // Solo
        
        Vector3 movement = CalculateMovement();
        CharacterController.Move(movement * (Speed * Time.deltaTime));
        FaceMovementDirection(movement, Time.deltaTime);
        _mainCamera.position = cameraOffset.position;
    }

    private Vector3 CalculateMovement()
    {
        Vector3 forward = _mainCamera.transform.forward;
        Vector3 right = _mainCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        SetNetworkedPosition(transform.position);
        return forward * InputReader.MovementValue.y + right * InputReader.MovementValue.x;
    }
    
    private void FaceMovementDirection(Vector3 movement, float deltaTime)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, 
            Quaternion.LookRotation(_mainCamera.transform.forward), deltaTime * RotationSmoothValue);
        SetNetworkedRotation(transform.rotation);
        
        transform.position += transform.forward * (movement.z * deltaTime * Speed);
    }
    
    [ObserversRpc]
    private void SetNetworkedPosition(Vector3 position)
    {
        // Actualizar la posición en todos los clientes
        if (!IsOwner)
            transform.position = position;
    }
    
    [ObserversRpc]
    private void SetNetworkedRotation(Quaternion rotation)
    {
        // Actualizar la rotación en todos los clientes
        if (!IsOwner)
            transform.rotation = rotation;
    }

}
