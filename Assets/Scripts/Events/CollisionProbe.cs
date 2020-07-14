using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class CollisionProbe : MonoBehaviour
{
    public readonly HashSet<GameObject> CollidedPallets = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (CollidedPallets.Contains(other.gameObject))
        {
            return;
        }

        if (other.gameObject.tag.StartsWith("pallet"))
        {
            /*
            var type = other.transform.Find(TagTypes.Objects.TagType).tag;
            var height = other.transform.Find(TagTypes.Objects.TagLayers).tag;
            var hasDamage = other.transform.Find(TagTypes.Objects.TagDamage).tag;
            */
            CollidedPallets.Add(other.gameObject);
            Debug.Log($"Entering: {CollidedPallets.Count}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.StartsWith("pallet") && CollidedPallets.Contains(other.gameObject))
        {
            CollidedPallets.Remove(other.gameObject);
            Debug.Log($"Leaving: {CollidedPallets.Count}");
        }
    }
}