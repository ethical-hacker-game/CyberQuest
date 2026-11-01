using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    public class NetworkMinigame_BackgroundArea : MonoBehaviour
    {
        [Range(0.5f, 1.0f)]
        [SerializeField] private float cutoff = 0.6f;

        private Material material;

        private Gradient ColorGradient => NetworkMinigame.MinigameInstance.BackgroundElementsColorGradient;
        private MinMaxRange SizeRange => NetworkMinigame.MinigameInstance.BackgroundElementsSizeRange;
        private float TextureSpeed => NetworkMinigame.MinigameInstance.BackgroundElementsTextureSpeed;


        private void Start()
        {
            material = GetComponent<MeshRenderer>().PrepareTemporaryMaterial();

            float colorValue = Random.Range(0.0f, 1.0f);
            material.color = ColorGradient.Evaluate(colorValue);

            float sizeValue = SizeRange.Random;
            transform.localScale = new Vector3(sizeValue, transform.localScale.y, sizeValue);

            material.SetFloat("_Cutoff", cutoff);
            SphereCollider sphereCollider = GetComponent<SphereCollider>();

            if (sphereCollider != null)
            {
                transform.localScale = new Vector3(0, transform.localScale.y, 0);
                sphereCollider.enabled = true;
            }
        }

        private void Update()
        {
            material.mainTextureOffset = new Vector2(material.mainTextureOffset.x, material.mainTextureOffset.y + (TextureSpeed * Time.deltaTime));
        }
    }
}
