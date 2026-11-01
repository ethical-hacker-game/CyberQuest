using Minigames;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMinigame_Camera : MonoBehaviour
{
    [SerializeField] private float cameraMoveTime = 0.5f;
    private Transform cameraTarget;

    private void Start()
    {
        float initalRotation = UnityEngine.Random.Range(0f, 360f);
        transform.eulerAngles = new Vector3(0, initalRotation, 0);
    }

    private void Update()
    {
        try
        {
            cameraTarget = NetworkMinigame.MinigameInstance.MainNode.transform;
            transform.position = Vector3.MoveTowards(transform.position, cameraTarget.position, Time.deltaTime / cameraMoveTime);
        }
        catch (Exception e) { }
    }
}
