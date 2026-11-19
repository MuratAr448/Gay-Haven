using System.Collections.Generic;
using UnityEngine;

public class MinigameCrates : MonoBehaviour
{
    public bool miniGameWon = false;
    public float cratePlacement = 1;
    [SerializeField] private List<OrderCrates> crates;
    private void Start()
    {
        for (int i = 0; i < crates.Count; i++)
        {
            int repos = (int)(crates.Count * 0.5f);
            float placement = cratePlacement * i- repos*cratePlacement;
            crates[i].transform.position = transform.position + Vector3.forward * placement;
        }
    }
    private void Update()
    {
        if (!miniGameWon)
        {
            WinCheck();
        }
    }
    private void WinCheck()
    {
        int totalCrates = 0;
        for (int i = 0; i < crates.Count; i++)
        {
            totalCrates += crates[i].amountCrates;
        }
        if (totalCrates >= 5)
        {
            miniGameWon = true;
        }
    }
}
