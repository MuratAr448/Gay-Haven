using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class OrderCrates : MonoBehaviour
{
    [SerializeField] private List<XRSocketInteractor> socets;
    [SerializeField] private InteractionLayerMask layerMask;
    public int amountCrates = 0;
    private void Start()
    {
        for (int i = 0; i < socets.Count; i++)
        {
            socets[i].socketActive = false;
        }
        socets[0].socketActive = true;
    }
    private void Update()
    {

    }
    public void IncreaseCrates(SelectEnterEventArgs e)
    {
        XRBaseInteractable interact = e.interactableObject.transform.GetComponent<XRBaseInteractable>();
        interact.interactionLayers = layerMask.value;
        amountCrates += 1;
        if (socets.Count> amountCrates)
        {
            socets[amountCrates].socketActive = true;
        }
        
    }
}
