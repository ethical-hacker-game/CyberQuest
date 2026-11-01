using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SD_ObjectScaler : MonoBehaviour
{
    public static List<SD_ObjectScaler> objectScalers = new List<SD_ObjectScaler>();

    [SerializeField] private string scallerId;
    [SerializeField] private float startScale = 0;

    public static void ChangeScale(string id, float scale)
    {
        for (int i = 0; i < objectScalers.Count; i++)
        {
            if (objectScalers[i].scallerId.ToLower() == id.ToLower())
                objectScalers[i].ChangeScale(scale);
        }
    }

    public void ChangeScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
    }

    private void Awake()
    {
        objectScalers.Add(this);
    }

    private void Start()
    {
        ChangeScale(startScale);
    }

    private void OnDestroy()
    {
        objectScalers.Remove(this);
    }
}
