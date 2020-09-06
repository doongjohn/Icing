using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangePlayer : MonoBehaviour
{
    // Character Data
    private BPCC_BodyData bodyData = new BPCC_BodyData();
    [SerializeField] private BPCC_Gravity gravity = new BPCC_Gravity();
    [SerializeField] private BPCC_GroundDetection groundDetection = new BPCC_GroundDetection();
    [SerializeField] private BPCC_Walk walk = new BPCC_Walk();
    [SerializeField] private BPCC_Jump jump = new BPCC_Jump();

    // Movement Vector
    private Vector2 controlVector;
    private Vector2 externalVector;

    private void Awake()
    {
        bodyData.Init(
            transform     : transform,
            rb2D          : GetComponent<Rigidbody2D>(),
            collider      : GetComponent<BoxCollider2D>(),
            oneWayCollider: transform.GetChild(0).GetComponent<BoxCollider2D>()
        );
        gravity.Init(
            bodyData,
            gravityAccel: -40,
            maxFallSpeed: -30
        );
        groundDetection.Init(
            bodyData,
            maxDetectCount: 50,
            maxWalkAngle  : 89,
            snapLength    : 0.1f,
            innerGap      : 0.1f
        );
        walk.Init(
            bodyData,
            walkSpeed: 15
        );
        jump.Init(
            bodyData,
            airJumpCount: 1
        );

        StartCoroutine(LateFixedUpdate());
    }
    private void Update()
    {
        groundDetection.GetInput_FallThrough(KeyCode.S);
        walk.GetInput(KeyCode.D, KeyCode.A);
        jump.GetInput(KeyCode.Space);
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
        }
        // Not On Ground
        else
        {
            // Air Jump
            if (jump.InputPressed && jump.CanAirJump)
            {
                jump.StartAirJump();
                groundDetection.ResetData();
            }
            // Apply Gravity
            else
            {
                gravity.CalcGravity();
                controlVector.y = gravity.value;
            }
        }

        // Reset Jump Input
        if (jump.InputPressed)
            jump.ResetInput();

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
        }

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

            // Note:
            // In order to snap to the ground correctly
            // DetectGround() method needs to run after internal physics update.

            // Detect Ground
            groundDetection.DetectGround(
                detectCondition: !jump.IsJumping,
                slideAccel     : gravity.gravityAccel,
                maxSlideSpeed  : gravity.maxFallSpeed);
        }
    }
}
