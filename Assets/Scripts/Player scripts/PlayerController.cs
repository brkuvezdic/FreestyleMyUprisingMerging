using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float horizontal;
    private float speed = 0.7f;  
    private float runningMultiplier = 1.3f;  
    private float jumpingPower = 2f;
    private bool isFacingRight = true;
    private bool isDashing = false;
    private float dashSpeed = 1.2f;
    private float dashCooldown = 0f;
    private float lastDashTime = -10f;
    private IInteractable interactable;
    private bool isHeavyAttacking = false;
    private bool isGunAttacking = false;
    private bool isMagicAttacking = false;
    private Animator animator;
    private bool isAttacking = false;
    private bool isJumpAttacking = false;
    private bool isClimbing = false;
    private bool isSleeping = false;
    private bool isBlocking = false;
    private PlayerStats playerStats;
    private AudioSource audioSource;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundlayer;
    [SerializeField] private KeyCode heavyAttackKey = KeyCode.F;
    [SerializeField] private KeyCode gunAttackKey = KeyCode.B;
    [SerializeField] private KeyCode magicAttackKey = KeyCode.R;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode blockKey = KeyCode.Mouse1;
    [SerializeField] private float teleportDistance = 3f;
    [SerializeField] private AudioClip walkingSound;
    [SerializeField] private AudioClip runningSound;


    //attack Damage values mogu se mijenjat kasnije
    [SerializeField] private float baseHeavyAttackDamage = 20f;
    [SerializeField] private float baseGunAttackDamage = 15f;
    [SerializeField] private float baseMagicAttackDamage = 25f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource component not found on the player! Adding one.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on the player!");
        }
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats component not found on the player!");
        }
    }

    void Update()
    {
        HandleInput();
        HeavyAttack();
        GunAttack();
        MagicAttack();
        Attack();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }
    private void HandleInput()
    {
        horizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time > lastDashTime + dashCooldown && !isDashing)
        {
            animator.SetTrigger("Dash");
            StartCoroutine(Dash());
        }

        if (Input.GetButtonDown("Jump") && IsGrounded() && !isClimbing) // Ensure climbing doesn't block jumping
        {
            Jump();
        }

        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            CutJumpShort();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractWithObject();
        }
        if (Input.GetKeyDown(blockKey))
        {
            StartBlock();
        }
        if (Input.GetKeyUp(blockKey))
        {
            EndBlock();
        }
        if (Input.GetKeyDown(KeyCode.J) && !IsGrounded())
        {
            PerformJumpAttack();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            PerformTeleport();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleSleep();
        }
    }

    private void HandleMovement()
    {
        if (isDashing)  // Skip normal movement processing while dashing
        {
            // Optionally reduce movement speed or prevent movement altogether while blocking
            return;
        }

        float move = horizontal * speed;
        bool isMoving = Mathf.Abs(move) > 0;
        bool isRunning = isMoving && Input.GetKey(KeyCode.LeftControl);

        // Apply the running multiplier if the character is running
        if (isRunning)
        {
            move *= runningMultiplier;
        }

        // Update the speed in the animator to handle walking/running animations.
        rb.velocity = new Vector2(move, rb.velocity.y);
        animator.SetFloat("Speed", Mathf.Abs(move));
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsWalking", isMoving && !isRunning);

        // Flip the character to face the direction of movement.
        Flip();
        // Handle grounded state for jumping and falling animations.
        animator.SetBool("Grounded", IsGrounded());

        // Play walking sound if moving and grounded, not already playing the walking sound, and not running
        if (isMoving && IsGrounded() && !isRunning && audioSource.clip == walkingSound && !audioSource.isPlaying)
        {
            audioSource.pitch = 1.0f; // Normal pitch for walking
            audioSource.Play();
        }
        else if (isRunning && IsGrounded() && (audioSource.clip != runningSound || !audioSource.isPlaying))
        {
            audioSource.clip = runningSound;
            audioSource.pitch = 1.5f; // Increased pitch for faster playback
            audioSource.Play();
        }
        else if (!isMoving || !IsGrounded())
        {
            audioSource.Stop();
        }
        // If the state changes (from walking to running or vice versa), change the clip and play it
        else if (isMoving && IsGrounded() && ((isRunning && audioSource.clip != runningSound) || (!isRunning && audioSource.clip != walkingSound)))
        {
            audioSource.clip = isRunning ? runningSound : walkingSound;
            audioSource.Play();
        }
    }


    private IEnumerator Dash()
    {
        float dashTime = 0.3f;  // Duration of the dash
        isDashing = true;
        animator.SetBool("IsDashing", true); // Set the IsDashing parameter to true when starting the dash.

        float dashDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(dashDirection * dashSpeed, rb.velocity.y);
        lastDashTime = Time.time;

        yield return new WaitForSeconds(dashTime);

        isDashing = false;
        animator.SetBool("IsDashing", false); // Set the IsDashing parameter to false when the dash ends.
    }

    private void Jump()
    {
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            animator.SetTrigger("Jump");  // Activate the Jump trigger in the Animator
        }
    }

    private void CutJumpShort()
    {
        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
    }

    private void Flip()
    {
        if ((isFacingRight && horizontal < 0) || (!isFacingRight && horizontal > 0))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundlayer);
    }
    public interface IInteractable
    {
        void Interact();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
        {
            interactable = collision.GetComponent<IInteractable>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable") && interactable != null)
        {
            interactable = null;
        }
    }
    private void InteractWithObject()
    {
        if (interactable != null)
        {
            interactable.Interact();
        }
        else
        {
            Debug.Log("No interactable object found.");
        }
    }
    private void HeavyAttack()
    {
        if (Input.GetKeyDown(heavyAttackKey) && !isDashing && !isHeavyAttacking) // Add other conditions as needed
        {
            isHeavyAttacking = true;
            animator.SetTrigger("HeavyAttack");
            // Notice how we're not dealing damage here anymore
        }
    }

    public void ResetHeavyAttack()
    {
        isHeavyAttacking = false;
    }
    private void GunAttack()
    {
        if (Input.GetKeyDown(gunAttackKey) && !isDashing && !isGunAttacking)
        {
            isGunAttacking = true;
            animator.SetTrigger("GunAttack");
            float totalDamage = baseGunAttackDamage + playerStats.physicalDamage;
            DealDamage(totalDamage);
        }
    }

    public void ResetGunAttack()
    {
        isGunAttacking = false;
    }
    private void MagicAttack()
    {
        if (Input.GetKeyDown(magicAttackKey) && !isDashing && !isMagicAttacking)
        {
            isMagicAttacking = true;
            animator.SetTrigger("MagicAttack");
            float totalDamage = baseMagicAttackDamage + playerStats.magicDamage;
            DealDamage(totalDamage);
        }
    }

    public void ResetMagicAttack()
    {
        isMagicAttacking = false;
    }
    private void Attack()
    {
        if (Input.GetKeyDown(attackKey) && !isDashing && !isAttacking && !isJumpAttacking)
        {
            isAttacking = true;
            animator.SetTrigger("Attack");
        }
    }

    public void ResetAttack()
    {
        isAttacking = false;
    }
    private void StartBlock()
    {
        isBlocking = true;
        animator.SetBool("Block", true); // Assume you have an "IsBlocking" bool parameter in your animator
                                              // Additional logic to reduce damage or prevent movement, if needed
    }

    private void EndBlock()
    {
        isBlocking = false;
        animator.SetBool("Block", false);
        // Reset any modified states or effects from blocking
    }
    private void PerformJumpAttack()
    {
        animator.SetTrigger("JumpAttack");
    }

    private void PerformTeleport()
    {
        // Calculate new position in front of the character
        Vector3 teleportDirection = isFacingRight ? Vector3.right : Vector3.left;
        Vector3 newPosition = transform.position + teleportDirection * teleportDistance;

        // Trigger teleport animation (make sure to create this in your Animator)
        animator.SetTrigger("Teleport");

        // Optionally, you could wait for the animation to finish before moving the character
        // This can be done using a Coroutine if the timing needs to be precise with the animation
        StartCoroutine(TeleportAfterDelay(newPosition, 0.5f)); // 0.5 seconds for example
    }
    IEnumerator TeleportAfterDelay(Vector3 newPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.position = newPosition;
    }
    private void ToggleSleep()
    {
        isSleeping = !isSleeping; // Toggle the state
        animator.SetBool("Sleeping", isSleeping); // Tell the animator about the new state
    }
    private void DealDamage(float damage)
    {
        // Assume a radius and forward distance where the attack occurs
        float attackRadius = 1f;
        Vector2 attackPosition = (Vector2)transform.position + (Vector2.right * transform.localScale.x * 0.5f);

        // Using OverlapCircleAll to detect enemies around the attack point
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, attackRadius);
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))  // Ensure enemies are tagged correctly
            {
                EnemyStats enemyStats = enemy.GetComponent<EnemyStats>();
                if (enemyStats != null)
                {
                    // Cast the damage to int since the TakeDamage method in EnemyStats expects an int
                    // The attack source is the player's current position
                    enemyStats.TakeDamage((int)damage, transform.position);
                    Debug.Log($"Dealt {damage} damage to {enemy.name}.");
                }
            }
        }
    }
    public void ExecuteAttackDamage()
    {
        float totalDamage;
        if (isHeavyAttacking)
        {
            totalDamage = baseHeavyAttackDamage + playerStats.physicalDamage;
        }
        else if (isGunAttacking)
        {
            totalDamage = baseGunAttackDamage + playerStats.physicalDamage;
        }
        else if (isMagicAttacking)
        {
            totalDamage = baseMagicAttackDamage + playerStats.magicDamage;
        }
        else
        {
            return; // No attack is being executed
        }

        DealDamage(totalDamage);
    }

    public void CancelActions()
    {
        // Cancel all attacks and movement-related actions
        isHeavyAttacking = false;
        isGunAttacking = false;
        isMagicAttacking = false;
        isAttacking = false;
        isJumpAttacking = false;
        isDashing = false;

        // Reset animator triggers or states if necessary
        animator.ResetTrigger("Dash");
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("GunAttack");
        animator.ResetTrigger("MagicAttack");
        animator.ResetTrigger("Attack");
        animator.SetBool("IsDashing", false);

        // Stop the current movement
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
    // Example when an enemy hits the player
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyStats enemyStats = collision.gameObject.GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                float damage = enemyStats.attackDamage;
                Vector3 attackSource = collision.transform.position;
                playerStats.TakeDamage((int)damage, attackSource);
            }
        }
    }
}