using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    private InputManager _inputManager;

    private void Start()
    {
        _inputManager.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOV -= SwitchCamera;
    }

    [SerializeField]
    public CameraState CameraState;
    public Action OnChangePerspective;
    [SerializeField]
    private CinemachineCamera _tpsCamera;
    [SerializeField]
    private CinemachineCamera _fpsCamera;

    private void SwitchCamera()
    {
        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _tpsCamera.gameObject.SetActive(false);
            _fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            CameraState = CameraState.ThirdPerson;
            _tpsCamera.gameObject.SetActive(true);
            _fpsCamera.gameObject.SetActive(false);
        }
        OnChangePerspective();
    }

    public void SetTPSFieldOfView(float fieldOfView)
    {
        _tpsCamera.Lens.FieldOfView = fieldOfView;
    }

    public void SetFPSCameraLookXInputControllerEnabled(bool enabled)
    {
        // Debug.Log("SetFPSCameraLookXInputControllerEnabled: " + enabled);
        foreach (var controller in _fpsCamera.GetComponent<CinemachineInputAxisController>().Controllers)
        {
            // Debug.Log("Controller.Name: " + controller.Name + ", Enabled: " + controller.Enabled);
            if (controller.Name == "Look X (Pan)")
            {
                if (controller.Enabled != enabled)
                {
                    controller.Enabled = enabled;
                }
                break;
            }
        }
    }
}
