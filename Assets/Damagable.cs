using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Damagable : MonoBehaviour
{

    public UnityEvent<int, Vector2> damagableHit;
    public UnityEvent damagableDeath;
    public UnityEvent<int,int> healthChanged;

    Animator animator;
    [SerializeField]
    private int _maxHealth = 100;
    
        public int MaxHealth
    {
        get{
            return _maxHealth;
        }
        set{
            _maxHealth = value;
        }
    }

[SerializeField]
    private int _health = 100;

    public int Health
    {
        get{
            return _health;
        }
        set 
        { 
            _health = value; 

            healthChanged?.Invoke(_health, MaxHealth);

        if(_health <= 0)
        {
            IsAlive = false;
        }
        }
    }

[SerializeField]
    private bool _isAlive = true;

    [SerializeField]
    private  bool isInvincible = false;

    private float timeSinceHit = 0;


    [SerializeField]
    private float invicibilityTimer = 0.25f;

    public bool IsAlive { get{
        return _isAlive;
    } set{
        _isAlive = value;
        animator.SetBool(AnimationStrings.isAlive, value);
        Debug.Log("IsAlive set " + value);

    } }

        public bool LockVelocity { get {
        return animator.GetBool(AnimationStrings.lockVelocity);
    } 
    set{
        animator.SetBool(AnimationStrings.lockVelocity, value);
    }
     }
     

    private void Awake(){
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        if(isInvincible)
        {
            if(timeSinceHit > invicibilityTimer)
            {
                isInvincible = false;
                timeSinceHit = 0;

            }
            timeSinceHit += Time.deltaTime;
        }
    
    }

    public bool Hit(int damage, Vector2 knockback)
    {
        if(IsAlive && !isInvincible)
        {
            Health -= damage;
            isInvincible = true;

            animator.SetTrigger(AnimationStrings.hitTrigger);
            damagableHit?.Invoke(damage, knockback);

            return true;
        }

        return false;

    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

   

}
