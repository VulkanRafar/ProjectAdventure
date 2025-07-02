using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Damegable : MonoBehaviour
{

    public UnityEvent<int, Vector2> damageableHit;
    Animator animator;

    [SerializeField]
    private int _maxHealth = 100;
    public int MaxHealth
    {
        get
        {
            return _maxHealth;
        }
        set
        {
            _maxHealth = value;
        }
    }

    [SerializeField]
    private int _health = 100;

    public int Health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;

            // If health drops below 0, die
            if (_health <= 0)
            {
                IsAlive = false;
            }
        }
    }

    [SerializeField]
    private bool _isAlive = true;
    [SerializeField]
    private bool isInvincible = false;

    private float timeSinceHit = 0;
    private float invincibilityTimer = 0.25f;


    public bool IsAlive
    {
        get
        {
            return _isAlive;
        }
        set
        {
            _isAlive = value;
            animator.SetBool(AnimationStrings.isAlive, value);
            Debug.Log("IsAlive set " + value);
        }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isInvincible)
        {
            if (timeSinceHit > invincibilityTimer)
            {
                // Remove invincibility
                isInvincible = false;
                timeSinceHit = 0;

            }

            timeSinceHit += Time.deltaTime;
        }

    }

    public void Hit(int damage, GameObject attacker, Vector2 knockback)
    {
        if (!IsAlive || isInvincible)
            return;

        // Tambahan perlindungan agar tidak terkena serangan sendiri
        if (attacker != null && attacker == gameObject)
            return;

        Health -= damage;
        isInvincible = true;

        //Notify other subscribed components that the damageable was hit to handle the knockback and such
        animator.SetTrigger(AnimationStrings.hit);
        LockVelocity = true;
        damageableHit.Invoke(damage, knockback);

    }

    public bool LockVelocity
    {
        get
        {
            return animator.GetBool(AnimationStrings.lockVelocity);
        }
        set
        {
            animator.SetBool(AnimationStrings.lockVelocity, value);
        }
    }

}
