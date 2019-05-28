using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateNewObjectCollider : MonoBehaviour
{
    public GameObject objectCollider;

    float throwTime;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            throwTime = Time.time;
        }

        else if (Input.GetMouseButtonUp(2))
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.position = transform.position;
            Rigidbody r = s.AddComponent<Rigidbody>();
            s.AddComponent<ObjectCollider>();
            TerrainColliderInteraction interactor = s.AddComponent<TerrainColliderInteraction>();
            interactor.consideOnlyInsideCell = true;
            interactor.type = TerrainColliderInteractionShape.ShapeType.Sphere;

            r.AddForce(Camera.main.transform.forward * 600f * Mathf.Clamp01((Time.time - throwTime) / 3f), ForceMode.VelocityChange);
        }
    }
}
