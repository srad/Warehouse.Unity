using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionProbe : MonoBehaviour
{
    private readonly List<GameObject> _collidedPallets = new List<GameObject>();

    public int PalletCount => _collidedPallets.Count();

    public GameObject[] Pallets => _collidedPallets.ToArray();

    public void Clear()
    {
        _collidedPallets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("pallet"))
        {
            _collidedPallets.Add(other.gameObject);
            Debug.Log("Add:" + _collidedPallets.Count());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("pallet"))
        {
            _collidedPallets.Remove(other.gameObject);
            Debug.Log("Remove:" + _collidedPallets.Count());
        }
    }
}