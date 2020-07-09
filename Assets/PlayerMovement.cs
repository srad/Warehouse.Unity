using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  public CharacterController controller;
  public float Speed = 12f;
  public float RotateSpeed = 3.0F;

  // Start is called before the first frame update
  void Start()
  {
        
  }

  // Update is called once per frame
  void Update()
  {
    /*
    float x = Input.GetAxis("Horizontal");
    float z = Input.GetAxis("Vertical");

    var move = transform.right * x + transform.forward * z;
    controller.Move(move * Speed * Time.deltaTime);
    */
    transform.Rotate(0, Input.GetAxis("Horizontal") * RotateSpeed, 0);
    var forward = transform.TransformDirection(Vector3.forward);
    float curSpeed = Speed * Input.GetAxis("Vertical");
    controller.SimpleMove(forward * curSpeed);
  }
}
