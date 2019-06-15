using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateNewObjectCollider : MonoBehaviour
{
    public GameObject objectCollider;

    float throwTime;

    void Spawn(float speed = 100f)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.transform.position = transform.position;
        Rigidbody r = s.AddComponent<Rigidbody>();
        s.AddComponent<ObjectCollider>();
        TerrainColliderInteraction interactor = s.AddComponent<TerrainColliderInteraction>();
        interactor.consideOnlyInsideCell = true;
        interactor.type = TerrainColliderInteractionShape.ShapeType.Sphere;
        interactor.transform.localScale = Vector3.one * Random.Range(1f, 4f);
        r.AddForce(Camera.main.transform.forward * speed * Mathf.Clamp01((Time.time - throwTime)), ForceMode.VelocityChange);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            throwTime = Time.time;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            Spawn();
        }

        if (Input.GetMouseButton(3))
        {
            if ((Time.time - throwTime) > .15f)
            {
                Spawn();
                throwTime = Time.time;
            }
        }
    }
}
