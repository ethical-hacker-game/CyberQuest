using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
[CanEditMultipleObjects]
#endif
public class ObjectsHider : MonoBehaviour
{
    public static List<ObjectsHider> objectHiders = new List<ObjectsHider>();

    [SerializeField] private string hiderId;
    [SerializeField] private bool startVisible = true;

    public static void SetVisibility(string id, bool visible)
    {
        for (int i = 0; i < objectHiders.Count; i++)
        {
            if (objectHiders[i].hiderId.ToLower() == id.ToLower())
                objectHiders[i].SetVisibility(visible);
        }
    }

    public void SetVisibility(bool visible)
    {
        gameObject.SetActiveOptimized(visible);
    }

    private void Awake()
    {
        objectHiders.Add(this);
    }

    private void Start()
    {
        SetVisibility(startVisible);
    }

    private void OnDestroy()
    {
        objectHiders.Remove(this);
    }
}
