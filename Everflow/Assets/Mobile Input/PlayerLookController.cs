using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using Unity.VisualScripting;

public class PlayerLookController : MonoBehaviour
{
    private Mouse mouse;
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;


    public Transform neck, character;
    private float rotY = 0.0f, mouseX; // rotation around the up/y axis
    private float rotX = 0.0f, mouseY; // rotation around the right/x axis

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mouse = InputSystem.GetDevice<Mouse>();
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
    }

    
    void Update()
    {
        mouseX = mouse.delta.ReadValue().x;
        mouseY = -mouse.delta.ReadValue().y;

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        character.localRotation = Quaternion.Euler(0.0f, rotY, 0.0f);
        neck.localRotation = Quaternion.Euler(rotX, 0.0f, 0.0f);
    }
}