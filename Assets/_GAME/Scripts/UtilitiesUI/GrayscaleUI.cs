using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrayscaleUI : MonoBehaviour
{
    [SerializeField] private bool greyOnStart = true;
    [SerializeField] private bool includeChildren = true;
    [SerializeField] private bool includeInactive = true;

    private Material imageGrayscaleMaterial;
    private List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();
    private List<Image> images = new List<Image>();

    private List<Tuple<TextMeshProUGUI, Color>> oldTextColors = new List<Tuple<TextMeshProUGUI, Color>>();
    private List<Tuple<Image, Color>> oldImageColors = new List<Tuple<Image, Color>>();

    public bool Active { get; private set; }


    [Button("To Grayscale"), ShowIf("@UnityEngine.Application.isPlaying")]
    public void ToGrayscale()
    {
        if (Active)
            return;

        Active = true;
        oldTextColors.Clear();
        oldImageColors.Clear();

        for (int i = 0; i < texts.Count; i++)
        {
            oldTextColors.Add(Tuple.Create(texts[i], texts[i].color));
            texts[i].color = texts[i].color.ToGrayscale();
        }

        if (imageGrayscaleMaterial == null && images.IsNullOrEmpty() == false)
            return;

        for (int i = 0; i < images.Count; i++)
        {
            oldImageColors.Add(Tuple.Create(images[i], images[i].color));
            images[i].color = images[i].color.ToGrayscale();
            images[i].material = imageGrayscaleMaterial;
        }
    }

    [Button("To Default"), ShowIf("@UnityEngine.Application.isPlaying")]
    public void ToDefault()
    {
        if (Active == false)
            return;

        Active = false;

        for (int i = 0; i < oldTextColors.Count; i++)
        {
            oldTextColors[i].Item1.color = oldTextColors[i].Item2;
        }

        for (int i = 0; i < oldImageColors.Count; i++)
        {
            oldImageColors[i].Item1.color = oldImageColors[i].Item2;
            oldImageColors[i].Item1.material = null;
        }
    }

    private void Awake()
    {
        imageGrayscaleMaterial = Resources.Load<Material>("Materials/GrayscaleSpriteMaterial");

        List<TextMeshProUGUI> textComponents;
        List<Image> imageComponents;


        if (includeChildren)
        {
            textComponents = GetComponentsInChildren<TextMeshProUGUI>(includeInactive).ToList();
            imageComponents = GetComponentsInChildren<Image>(includeInactive).ToList();
        }
        else
        {
            textComponents = GetComponents<TextMeshProUGUI>().ToList();
            imageComponents = GetComponents<Image>().ToList();
        }

        if (textComponents.IsNullOrEmpty() == false)
            texts.AddRange(textComponents);

        if (imageComponents.IsNullOrEmpty() == false)
            images.AddRange(imageComponents);

        for (int i = images.Count - 1; i >= 0; i--)
        {
            if (images[i].gameObject.GetComponent<Mask>() != null)
                images.RemoveAt(i);
        }
    }

    private void Start()
    {
        Active = false;

        if (greyOnStart == false)
            return;

        ToGrayscale();
    }
}
