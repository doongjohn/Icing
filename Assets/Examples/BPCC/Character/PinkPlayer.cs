﻿using Icing;
using System.Collections;
using UnityEngine;

public class PinkPlayer : MonoBehaviour
{
    // Character Data
    private BPCC_BodyData bodyData = new();
    [SerializeField] private BPCC_Gravity gravity = new();
    [SerializeField] private BPCC_GroundDetection groundDetection = new();
    [SerializeField] private BPCC_Walk walk = new();
    [SerializeField] private BPCC_Jump jump = new();
    private float jumpAllowTime = 0.12f; // coyote time
    private float jumpAllowTimer = 0;

    // Movement Vector
    private Vector2 controlVector;
    private Vector2 externalVector;

    // Visual
    private Animator animator;
    private SpriteRenderer sr;

    private void OnValidate()
    {
        if (groundDetection != null && groundDetection.bodyData != null)
            groundDetection?.ApplyInnerGap();
    }

    private void Awake()
    {
        bodyData.Init(
            transform: transform,
            rb2D: GetComponent<Rigidbody2D>(),
            collider: GetComponent<BoxCollider2D>(),
            oneWayCollider: transform.GetChild(0).GetComponent<BoxCollider2D>()
        );
        gravity.Init(bodyData);
        groundDetection.Init(
            bodyData: bodyData,
            ccol: GetComponent<CircleCollider2D>()
        );
        walk.Init();
        jump.Init(bodyData);

        // Get Visual
        gameObject.GetComponent(out animator);
        transform.GetChild(1).gameObject.GetComponent(out sr);

        // Late Fixed Update
        StartCoroutine(LateFixedUpdate());
    }
    private void Update()
    {
        groundDetection.GetInput_FallThrough(KeyCode.S);
        walk.GetInput(KeyCode.D, KeyCode.A);
        jump.GetInput(KeyCode.Space);
    }
    private void LateUpdate()
    {
        // Animation and Visual
        if (jump.IsJumping)
        {
            if (jump.InputPressed)
            {
                // restart animation from the start
                animator.SetDuration(jump.JumpCurve.EndTime, "Jump");
                animator.Play("Jump", 0, 0);
            }
            else
            {
                // continue animation
                animator.Play("Jump");
            }
        }
        else
        {
            animator.ResetSpeed();
            if (groundDetection.OnGround)
            {
                if (walk.InputDir == 0 || groundDetection.slideDownVector != Vector2.zero)
                {
                    animator.Play("Idle");
                }
                else if (walk.InputDir != 0)
                {
                    if (walk.curWalkSpeed <= walk.maxSpeed * 0.6)
                    {
                        animator.Play("Walk");
                    }
                    else
                    {
                        animator.Play("Run");
                    }
                }
            }
            else
            {
                jumpAllowTimer += Time.deltaTime;
                animator.Play("Airborne");
            }
        }
    }
    private void FixedUpdate()
    {
        // Reset Movement Vector
        controlVector = Vector2.zero;
        externalVector = Vector2.zero;

        // On Ground
        if (groundDetection.OnGround)
        {
            // Ground Jump
            if (jump.InputPressed)
            {
                jump.StartJump();
                groundDetection.ResetData();
            }

            // Reset Air Jump Count
            jump.ResetAirJumpCount();

            jumpAllowTimer = 0;
        }
        // Not On Ground
        else
        {
            // Air Jump
            if (jump.InputPressed)
            {
                if (jumpAllowTimer <= jumpAllowTime)
                {
                    jumpAllowTimer = jumpAllowTime + 1;
                    jump.StartJump();
                }
                else if (jump.CanAirJump)
                {
                    jump.StartAirJump();
                }
                groundDetection.ResetData();
            }
            // Apply Gravity
            else
            {
                gravity.CalcGravity();
                controlVector.y = gravity.value;
            }
        }

        // Apply Jump Velocity
        if (jump.IsJumping)
        {
            jump.CalcJumpVelocity();
            if (jump.IsJumping)
                controlVector.y = jump.JumpVelocity.Value;
        }

        // Apply Slide Vector
        if (groundDetection.OnSteepSlope)
        {
            controlVector = groundDetection.slideDownVector;
        }
        // Apply Walk Vector
        else
        {
            walk.CalcWalkVector(groundDetection.GroundData);
            controlVector += walk.WalkVector;

            // Flip Sprite
            sr.flipX = walk.MoveDir != 1;
        }

        // Reset Jump Input
        if (jump.InputPressed)
            jump.ResetInput();

        // Apply Velocity
        bodyData.rb2D.velocity = controlVector + externalVector;
    }

    IEnumerator LateFixedUpdate()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();

            // Fall Through One Way Platform
            groundDetection.FallThrough();

            // Detect Ground
            groundDetection.DetectGround(
                detectCondition: !jump.IsJumping,
                gravity: gravity
            );
        }
    }
}
