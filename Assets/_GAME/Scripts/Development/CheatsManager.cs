using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatsManager : GameSystemComponent
{
    private float cheatingTimer = 0;
    private float cheatingActivationDelay = 2;

    public static CheatsManager Instance { get; private set; }
    public static bool CheatingActivated => Instance == null ? false : Instance.cheatingTimer > Instance.cheatingActivationDelay;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
            cheatingTimer += Time.deltaTime;

        if(Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftCommand))
            cheatingTimer = 0;
    }
}
