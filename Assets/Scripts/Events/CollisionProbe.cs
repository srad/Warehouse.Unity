using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class CollisionProbe : MonoBehaviour
{
    private readonly List<GameObject> _collidedPallets = new List<GameObject>();
    public bool hasLoad = false;

    public GameObject[] Pallets => _collidedPallets.ToArray();

    public void Clear()
    {
        _collidedPallets.Clear();
        hasLoad = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("pallet"))
        {
            Debug.Log(_collidedPallets.Count());
            _collidedPallets.Add(other.gameObject);
            hasLoad = other.transform.Find(PalletTags.Types.Load).CompareTag("1");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("pallet"))
        {
            Debug.Log(_collidedPallets.Count());
            _collidedPallets.Remove(other.gameObject);
        }
    }
}