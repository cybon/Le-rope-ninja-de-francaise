using UnityEngine;
using System.Collections;

//  NOTES  //

//Cooldown method timestamp not final
//Work on cooldowns

public class Player : MonoBehaviour
{
    #region Attributes

        #region Attr player actions
        // The way the character is facing on x axis
        private bool facingRight = true;

        // Colliders
        private Vector2 origColSize;
        private BoxCollider2D col;
        private Vector2 slideColSize;
        private Vector2 crouchColSize;

        // Run/walk
        public float walkSpeed = 12f;
        public float runSpeed = 30f;
        public bool running;

        // Exponential running attr
        private float runningLStart = 0.03f;
        private float runningLRange;
        public float runningLAccel = 0.05f;

        // Slide
        private bool sliding;
        private float slideForce = 25f;
        private float slideLengthStamp;
        private float slideLength = 0.4f;

        // Jumps
        public float jumpForce = 15f;
        public float currJumps = 2;
        public float jumps = 2;
        private bool jumping;

        // Crouch
        private bool crouch;
        public float crouchSpeed = .5f;

        // Dash
        public float dashForce = 25f;

        // Dash cooldown
        private float dashCooldownStamp;
        public float dashCooldown = 1f;

        // Dash length timer
        private float dashLengthStamp;
        public float dashLengthTime = 0.1f;

        // Dash charges
        public float currDashes = 2;
        public float dashes = 2;
        private bool dashing = false;


    #endregion

    #region Attr checks
        // A position marking where to check if the player is grounded.
        private Transform groundCheck;

        // Radius of the overlap circle to determine if grounded
        private float groundedRadius = .2f;

        // Whether or not the player is grounded.
        public bool grounded;

        // A position marking where to check for ceilings
        private Transform ceilingCheck;

        // Radius of the overlap circle to determine if the player can stand up
        private float ceilingRadius = .01f;

        // Reference to the player's animator component.
        private Animator anim;

        // Whether or not a player can steer while jumping;
        //[SerializeField]
        //private bool airControl = false;

        // A mask determining what is ground to the character
        [SerializeField]
        private LayerMask whatIsGround; 

        //Player vector & rigidbody (vars)
        private Rigidbody2D rb;
        private Vector2 movement;
    #endregion

    #endregion

    #region Awake/Start/FixedUpdate/Update
        private void Awake()
        {
            // Setting up references.
            groundCheck = transform.Find("GroundCheck");
            ceilingCheck = transform.Find("CeilingCheck");
            anim = GetComponent<Animator>();
        }

	    // Initialization
	    void Start () 
        {
            //Geting the properties of my character
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            origColSize = col.size;
            runningLRange = runningLStart;
	    }
	
	    //(Time based update checks) Update is called once per frame
	    void FixedUpdate () 
        {
            //Collision
            RenderGroundCollider();

            // Set the vertical animation/running animation
            anim.SetFloat("vSpeed", rb.velocity.y);

            //Basic horizontal movement based on where the player is facing and sprinting
            Movement();

            //Player actions/abilities with or without cooldowns
            PlayerActions();

            //Time based functions like cooldowns 
            PlayerCooldowns();

            //Check information regarding player status
            PlayerChecks();
        }

        void Update ()
        {
            //Input by the player
            PlayerInput();

            //Player animations
            PlayerAnimations();
        }
    #endregion

    #region Render/detection - enviroment/player relations
        private void RenderGroundCollider()
    {
        // The player is grounded(var) if a circlecast to the groundcheck(var) position hits anything designated as ground
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
        anim.SetBool("Ground", grounded);
    }
    #endregion

    #region Update collections
        private void PlayerAnimations()
        {
            anim.SetBool("Crouch", crouch);
        }
       
        private void PlayerInput()
        {
            //Jump - Key(Space)
            if (Input.GetKeyDown(KeyCode.Space) && currJumps > 0)
            {
                currJumps -= 1;
                jumping = true;
            }

            //Dash - Key(Left Control)
            if (Input.GetKeyDown(KeyCode.LeftControl) && currDashes > 0 && !dashing && !running && !crouch)
            {
                dashing = true;
                currDashes -= 1;
                //Dash length timer started started
                dashLengthStamp = Time.time + dashLengthTime;
            }

            //Slide - Key(S) + Key(Shift)
            if (Input.GetKeyDown(KeyCode.S) && running && !dashing)
            {
                if (IsSpeedX(runSpeed))
                {
                    sliding = true;
                    slideLengthStamp = Time.time + slideLength;
                }
            }

            //Crouch - Key(S)
            if (Input.GetKey(KeyCode.S) && grounded && !running && !sliding )
            {
                crouch = true;
            }
            else
            {
                crouch = false;
                col.size = origColSize;
            }
    }
    #endregion

    #region FixedUpdate collections
        private void PlayerActions()
        {
            if (dashing)
            {
                //Dash method
                Dash();
            }

            if (sliding)
            {
                //Slide method
                Slide();
            }

            if (crouch)
            {
                //Crouch method
                Crouch();
            }

            if (jumping)
            {
                //Jump method
                Jump();
            }
        }

        private void PlayerChecks()
        {
            //Checking if player is grounded
            IsGrounded();

            //Number of jumps(attr) available and when
            CanJump();
            
            //Checking if there is available space for the player to stand instead of crouching
            CanStand();
        }

        private void PlayerCooldowns()
        {
            //Refills dashes(attr) if cooldown is finished
            DashCooldown();
        }

        private void Movement()
        {
            float currentVelocityX = rb.velocity.x;

            //Getting horizontal input (A,D or lArrow && rArrow)
            float move = Input.GetAxisRaw("Horizontal");
            float speed = walkSpeed;
            //Running function
            bool run = Input.GetKey("left shift");

            if (run && !crouch && grounded && rb.velocity.x != 0)
            {
                speed = Mathf.Lerp(walkSpeed, runSpeed, runningLRange);

                running = true;
                runningLRange = runningLRange + runningLAccel;
            }
            else
            {
                runningLRange = runningLStart;
                running = false;
            }

            //Setting movement
            movement = new Vector2(move * speed, rb.velocity.y);
            anim.SetFloat("Speed", Mathf.Abs(move));

            //Flipping player
            if (move > 0 && !facingRight)
            {
                // ... flip the player.
                Flip();
                runningLRange = 0;
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && facingRight)
            {
                // ... flip the player.
                Flip();
                runningLRange = 0;
            }
            // Sets the rigidbodys velocity to a vector 2 movement
            rb.velocity = movement;

        }

        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            facingRight = !facingRight;

            // Multiply the player's x local scale by -1.da
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    #endregion

    #region PlayerChecks
        private void CanJump()
        {
            // If the player is grounded(attr) and currJumps(attr) is less than Jumps(attr) then refill currJumps(attr)
            if (grounded && currJumps < jumps)
            {
                currJumps = jumps;
            }
        }

        private void IsGrounded()
        {
            //If there is player movement on the y axis then grounded(attr) is false
            if (rb.velocity.y > 0)
            {
                grounded = false;
            }
        }

        private void CanStand()
        {
        if (!crouch)
        {
            if (Physics2D.OverlapCircle(ceilingCheck.position, ceilingRadius, whatIsGround))
            {
                Crouch();
            }

        }
    }
    #endregion

    #region Player actions
        private void Slide()
        {
            float currRbV = GetComponent<Rigidbody2D>().velocity.x;
        
            if (slideLengthStamp > Time.time)
            {
                slideColSize = new Vector2(0.3036469f, 0.2463926f);
                col.size = slideColSize;

                if (facingRight)
                {
                    GetComponent<Rigidbody2D>().velocity = new Vector2(1 * slideForce, 0);
                }
                else if (!facingRight)
                {
                    GetComponent<Rigidbody2D>().velocity = new Vector2(-1 * slideForce, 0);
                }
                //New colider so you're able to slide under various objects

            }
            else
            {
                //Sliding reset of both the action and temp colider
                sliding = false;
                col.size = origColSize;
            }
        }

        private void Jump()
        {
            //Adding y axis velocity to player if jumping is avialable
            rb.velocity = new Vector2(0, jumpForce);

            jumping = false;
        }

        private void Dash()
        {
            float currRbV = GetComponent<Rigidbody2D>().velocity.x;

            if (dashLengthStamp > Time.time)
            {     
                if (facingRight)
                {
                    Debug.Log("Right force");
                    GetComponent<Rigidbody2D>().velocity = new Vector2(1 * (dashForce + currRbV), 0);
                }
                else if (!facingRight)
                {
                    Debug.Log("Left force");
                    GetComponent<Rigidbody2D>().velocity = new Vector2(-1 * (dashForce + -currRbV), 0);
                }
            }
            else
            {
                dashing = false;
            }
            //Dash cooldown started
            dashCooldownStamp = Time.time + dashCooldown;
        }

        private void Crouch()
        {
            crouchColSize = new Vector2(0.3036469f, 0.2463926f);
            col.size = crouchColSize;

            rb.velocity = new Vector2(rb.velocity.x * crouchSpeed, rb.velocity.y);
    }
    #endregion

    #region Cooldowns
        private void DashCooldown()
        {
            if (!dashing)
            {
                if (dashCooldownStamp <= Time.time && currDashes < dashes)
                {
                    currDashes = dashes;
                }
            }

        }
    #endregion

    #region Value checks
        private bool IsSpeedX(float SpeedVar)
        {
            if (rb.velocity.x == SpeedVar || rb.velocity.x == -SpeedVar) return true; else return false;
        }
        private bool IsSpeedY(float SpeedVar)
        {
            if (rb.velocity.y == SpeedVar || rb.velocity.y == -SpeedVar) return true; else return false;
        }
    #endregion
}
