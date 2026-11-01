using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionUtility : MonoBehaviour
{
    [SerializeField] private bool disableAllOnStart;

    #region Constant Rotation
    [Header("Constant Rotation")]
    [SerializeField] private bool constantRotation;
    [SerializeField, EnableIf("constantRotation")] private float constantRotationActivationTime = 5;
    [SerializeField, EnableIf("constantRotation")] private Vector3 constantRotationSpeed;
    private float constantRotationTemp;
    public bool ConstantRotation { get => constantRotation; set => constantRotation = value; }

    private void Update_ConstantRotation()
    {
        if (constantRotationTemp < constantRotationActivationTime)
            constantRotationTemp += Time.deltaTime;
        else if (constantRotationTemp > constantRotationActivationTime)
            constantRotationTemp = constantRotationActivationTime;

        float activationMultiplier = 1;

        if (constantRotationActivationTime != 0)
            activationMultiplier = constantRotationTemp / constantRotationActivationTime;

        transform.Rotate(constantRotationSpeed * Time.deltaTime * activationMultiplier);
    }
    #endregion

    #region Lerp Rotation
    [Header("Lerp Rotation")]
    [SerializeField] private bool lerpRotation;
    [SerializeField, EnableIf("lerpRotation")] private float lerpRotationActivationTime = 5;
    [SerializeField, EnableIf("lerpRotation")] private Vector3 lerpRotationMin;
    [SerializeField, EnableIf("lerpRotation")] private Vector3 lerpRotationMax;
    [SerializeField, EnableIf("lerpRotation")] private float lerpRotationSpeed = 1;
    private float lerpRotationTemp;
    public bool LerpRotation { get => lerpRotation; set => lerpRotation = value; }

    private void Update_LerpRotation()
    {
        if (lerpRotationTemp < lerpRotationActivationTime)
            lerpRotationTemp += Time.deltaTime;
        else if (lerpRotationTemp > lerpRotationActivationTime)
            lerpRotationTemp = lerpRotationActivationTime;

        float activationMultiplier = 1;

        if (lerpRotationActivationTime != 0)
            activationMultiplier = lerpRotationTemp / lerpRotationActivationTime;

        float t = Mathf.PingPong(Time.time * lerpRotationSpeed * activationMultiplier, 1f);
        t = EasyInOut(t);
        transform.rotation = Quaternion.Slerp(Quaternion.Euler(lerpRotationMin), Quaternion.Euler(lerpRotationMax), t);
    }
    #endregion

    #region Constant Motion
    [Header("Constant Motion")]
    [SerializeField] private bool constantMotion;
    [SerializeField, EnableIf("constantMotion")] private float constantMotionActivationTime = 5;
    [SerializeField, EnableIf("constantMotion")] private Vector3 constantMotionSpeed;
    private float constantMotionTemp;
    public bool ConstantMotion { get => constantMotion; set => constantMotion = value; }

    private void Update_ConstantMotion()
    {
        if (constantRotationTemp < constantMotionActivationTime)
            constantMotionTemp += Time.deltaTime;
        else if (constantMotionTemp > constantMotionActivationTime)
            constantMotionTemp = constantMotionActivationTime;

        float activationMultiplier = 1;

        if (constantMotionActivationTime != 0)
            activationMultiplier = constantMotionTemp / constantMotionActivationTime;

        transform.Translate(constantMotionSpeed * Time.deltaTime * activationMultiplier);
    }
    #endregion


    private float EasyInOut(float t)
    {
        return t * t * (3.0f - 2.0f * t);
    }

    private void Start()
    {
        if (disableAllOnStart)
        {
            constantRotation = false;
            lerpRotation = false;
            constantMotion = false;
        }
    }

    private void Update()
    {
        if (constantRotation)
            Update_ConstantRotation();

        if (lerpRotation)
            Update_LerpRotation();

        if (constantMotion)
            Update_ConstantMotion();
    }
}