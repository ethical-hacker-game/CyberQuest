using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    public class NetworkMinigame_GreenArea : MonoBehaviour
    {
        [Range(0.5f, 1.0f)]
        public float cutoff = 0.6f;
        public float textureSpeed = 1;
        public float scaleSpeed = 0.5f;

        private Material material;

        void Start()
        {
            material = GetComponent<MeshRenderer>().PrepareTemporaryMaterial();
        }

        void Update()
        {
            material.SetFloat("_Cutoff", cutoff);
            material.mainTextureOffset = new Vector2(material.mainTextureOffset.x, material.mainTextureOffset.y + (textureSpeed * Time.deltaTime));
            
            if (NetworkMinigame.MinigameInstance.MaxGreenAreaSize > transform.localScale.x)
                transform.localScale += new Vector3(Time.deltaTime * scaleSpeed, 0, Time.deltaTime * scaleSpeed);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<NetworkMinigame_Node>() == null) return;

            NetworkMinigame_Node tempNode = other.GetComponent<NetworkMinigame_Node>();

            if (tempNode.IsDestroying == false && tempNode.NodeIndex != -1)
            {
                NetworkMinigame.MinigameInstance.GreenAreaWin();
            }
        }
    }
}
