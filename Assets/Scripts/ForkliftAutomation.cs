using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForkliftAutomation : MonoBehaviour
{
    public int count = 6;
    private int i = 0;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(Capture());
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            Debug.Log("Q");
            transform.Translate(new Vector3(-1.5f * Time.deltaTime, 0, 0), Space.Self);
        }

        if (Input.GetKey(KeyCode.E))
        {
            Debug.Log("Q");
            transform.Translate(new Vector3(1.5f * Time.deltaTime, 0, 0), Space.Self);
        }
    }

    private void Capture()
    {
        /*
        var start = GameObject.FindWithTag("pallet");
        var distance = start.gameObject.GetComponent<Renderer>().bounds.size.x / 2;

        while (true)
        {
            yield return new WaitForEndOfFrame();
            //var move = transform.forward * 1.5f;
            transform.Translate(new Vector3(-distance, 0, 0), Space.Self);
            yield return new WaitForEndOfFrame();

            if (i++ > count)
            {
                break;
            }

            yield return new WaitForSeconds(1f);
        }
        */
    }
}