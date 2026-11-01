using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minigames
{
    public class NetworkMinigame_RedArea : MonoBehaviour
    {
        [Range(0.5f, 1.0f)]
        [SerializeField] private float cutoff = 0.6f;
        [SerializeField] private float textureSpeed = 1;
        [SerializeField] private float scaleSpeed = 0.5f;
        [SerializeField] private bool canBeDestroyed = true;

        private Material material;
        private bool isDestroing = false;

        private List<NetworkMinigame_Node> affectedNodes = new List<NetworkMinigame_Node>();

        public void Destroy()
        {
            isDestroing = true;
            scaleSpeed = transform.localScale.x;
            Destroy(gameObject, 1);
        }

        private void Start()
        {
            material = GetComponent<MeshRenderer>().PrepareTemporaryMaterial();
            SphereCollider sphereCollider = GetComponent<SphereCollider>();

            if (sphereCollider != null)
            {
                transform.localScale = new Vector3(0, transform.localScale.y, 0);
                sphereCollider.enabled = true;
            }
        }

        private void Update()
        {
            material.SetFloat("_Cutoff", cutoff);
            material.mainTextureOffset = new Vector2(material.mainTextureOffset.x, material.mainTextureOffset.y + (textureSpeed * Time.deltaTime));

            if (isDestroing && canBeDestroyed)
            {
                transform.localScale -= new Vector3(Time.deltaTime * scaleSpeed, 0, Time.deltaTime * scaleSpeed);
            }
            else if(NetworkMinigame.MinigameInstance.MaxRedAreaSize > transform.localScale.x)
            {
                transform.localScale += new Vector3(Time.deltaTime * scaleSpeed, 0, Time.deltaTime * scaleSpeed);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            NetworkMinigame_Node tempNode = other.GetComponent<NetworkMinigame_Node>();

            if (tempNode == null) 
                return;

            if (NetworkMinigame.MinigameInstance.AttackingRedAreas.Contains(this)) 
                return;

            if (tempNode.IsDestroying == false && tempNode.NodeIndex != -1)
            {
                NetworkMinigame.MinigameInstance.AttackingRedAreas.Add(this);
                affectedNodes.Add(tempNode);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            NetworkMinigame_Node tempNode = other.GetComponent<NetworkMinigame_Node>();

            if (tempNode == null) 
                return;

            if (affectedNodes.Contains(tempNode) == false)
                return;

            if (tempNode.IsDestroying || tempNode.NodeIndex < -1)
            {
                affectedNodes.Remove(tempNode);

                if (NetworkMinigame.MinigameInstance.AttackingRedAreas.Contains(this) == false) 
                    return;

                if (affectedNodes.Count == 0)
                    NetworkMinigame.MinigameInstance.AttackingRedAreas.Remove(this);
            }
        }
    }
}
