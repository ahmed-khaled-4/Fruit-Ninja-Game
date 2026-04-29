using System;
using UnityEngine;

public class Cutting : MonoBehaviour
{
    private Vector3 touchPosition;

    void Update()
    {
        Vector3 world = Vector3.zero;
        bool hasPoint = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            world = Camera.main.ScreenToWorldPoint(touch.position);
            hasPoint = true;
        }
        else if (Input.GetMouseButton(0))
        {
            world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hasPoint = true;
        }

        if (!hasPoint)
            return;

        world.z = 0;
        touchPosition = world;
        transform.position = touchPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.Damage();
        }
    }
}
