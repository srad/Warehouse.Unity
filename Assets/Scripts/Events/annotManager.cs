using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class annotManager : MonoBehaviour
{
    public float move_speed = 1.0f;
    public float seconds = 1.0f;

    public float annot_num = 1.0f;
    public float annotation_counter = 0.0f;

    public Vector3 moveVector;

    void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
