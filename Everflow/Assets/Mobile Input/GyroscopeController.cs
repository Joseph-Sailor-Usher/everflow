using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public class GyroscopeController : MonoBehaviour
{
    public Gyroscope gyro;
    public float speedRatio = 10.0f;
    public float leftFlickThreshold = 10.0f, rightFlickThreshold = 10.0f, upFlickThreshold = 10.0f, downFlickThreshold = 10.0f;
    public int leftFlickTimeToRegister = 4, rightFlickTimeToRegister = 4, upFlickTimeToRegister = 4, downFlickTimeToRegister = 4;
    private float fiftiethOfASecondFlickingLeft, fiftiethOfASecondFlickingRight, fiftiethOfASecondFlickingUp, fiftiethOfASecondFlickingDown;
    public int fiftiethsOfASecondBetweenFlicks = 20, fiftiethsOfASecondSinceLastFlick = 0;
    private bool initiatingUpFlick = false, initiatingDownFlick = false, initiatingLeftFlick = false, initiatingRightFlick = false;
    public bool debug = true;

    //functions to call when we register various events
    public UnityEvent OnFlickUp, OnFlickDown, OnFlickLeft, OnFlickRight;
    private void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
        }
        else
            gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        //Clock
        fiftiethsOfASecondSinceLastFlick++;

        //detect flicks
        if (-gyro.rotationRate.y * speedRatio < -leftFlickThreshold)
            RegisterRightFlick();
        if (-gyro.rotationRate.y * speedRatio > rightFlickThreshold)
            RegisterLeftFlick();
        if (gyro.rotationRate.x * speedRatio > upFlickThreshold)
            RegisterUpFlick();
        if (gyro.rotationRate.x * speedRatio < -downFlickThreshold)
            RegisterDownFlick();
    }
    private void RegisterLeftFlick()
    {
        //if cooldown has passed
        if (fiftiethsOfASecondSinceLastFlick > fiftiethsOfASecondBetweenFlicks)
        {
            fiftiethOfASecondFlickingLeft++;
            if (fiftiethOfASecondFlickingLeft > leftFlickTimeToRegister)
            {
                ResetFlickTimers();
                fiftiethsOfASecondSinceLastFlick = 0;
                OnFlickLeft.Invoke();
                if (debug)
                    Debug.Log("Left");
            }
        }

    }
    private void RegisterRightFlick()
    {
        //if cooldown has passed
        if (fiftiethsOfASecondSinceLastFlick > fiftiethsOfASecondBetweenFlicks)
        {
            fiftiethOfASecondFlickingRight++;
            if (fiftiethOfASecondFlickingRight > rightFlickTimeToRegister)
            {
                ResetFlickTimers();
                fiftiethsOfASecondSinceLastFlick = 0;
                OnFlickRight.Invoke();
                if (debug)
                    Debug.Log("Right");
            }
        }
    }
    private void RegisterUpFlick()
    {
        //if cooldown has passed
        if (fiftiethsOfASecondSinceLastFlick > fiftiethsOfASecondBetweenFlicks)
        {
            fiftiethOfASecondFlickingUp++;
            if (fiftiethOfASecondFlickingUp > upFlickTimeToRegister)
            {
                ResetFlickTimers();
                fiftiethsOfASecondSinceLastFlick = 0;
                OnFlickUp.Invoke();
                if (debug)
                    Debug.Log("Up");
            }
        }
    }
    private void RegisterDownFlick()
    {
        //if cooldown has passed
        if (fiftiethsOfASecondSinceLastFlick > fiftiethsOfASecondBetweenFlicks)
        {
            fiftiethOfASecondFlickingDown++;
            if (fiftiethOfASecondFlickingDown > downFlickTimeToRegister)
            {
                ResetFlickTimers();
                fiftiethsOfASecondSinceLastFlick = 0;
                OnFlickDown.Invoke();
                if (debug)
                    Debug.Log("Down");
            }
        }
    }
    private void ResetFlickTimers()
    {
        fiftiethOfASecondFlickingLeft = 0;
        fiftiethOfASecondFlickingRight = 0;
        fiftiethOfASecondFlickingUp = 0;
        fiftiethOfASecondFlickingDown = 0;
        initiatingUpFlick = false;
        initiatingDownFlick = false;
        initiatingLeftFlick = false;
        initiatingRightFlick = false;
    }
}


