using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    Collider2D attackCollider;

    public int attackDamage = 10;
    public float knockbackStrength = 6f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damegable damageable = collision.GetComponent<Damegable>();

        if (damageable != null && collision.gameObject != gameObject)
        {
            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            Vector2 knockbackForce = knockbackDir * knockbackStrength; // atur kekuatan knockback sesuai keinginan

            damageable.Hit(attackDamage, gameObject, knockbackForce);

            Debug.Log(collision.name + " hit for " + attackDamage);
        }
    }
    
}
