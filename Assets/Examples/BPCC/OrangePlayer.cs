using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangePlayer : MonoBehaviour
{
    [SerializeField] private BPCC_BodyData bodyData = new BPCC_BodyData();
    [SerializeField] private BPCC_Gravity gravity = new BPCC_Gravity();
    [SerializeField] private BPCC_GroundDetection groundDetection = new BPCC_GroundDetection();
    [SerializeField] private BPCC_Walk walk = new BPCC_Walk();
    [SerializeField] private BPCC_Jump jump = new BPCC_Jump();

    private Vector2 controlVector;
    private Vector2 externalVector;

    private void Awake()
    {
        bodyData.transform = transform;
        bodyData.collider = GetComponent<BoxCollider2D>();
        bodyData.colliderSize = bodyData.collider.size;
        bodyData.rb2D = GetComponent<Rigidbody2D>();

        gravity.Init(
            bodyData,
            gravityAccel: -40,
            maxFallSpeed: -30);

        groundDetection.Init(
            bodyData,
            maxDetectCount: 50,
            maxWalkAngle: 89,
            snapLength: 0.1f,
            innerGap: 0.1f);

        walk.Init(walkSpeed: 15);

        jump.Init(bodyData);

        StartCoroutine(LateFixedUpdate());
    }
    private void Update()
    {
        walk.GetInput(KeyCode.D, KeyCode.A);
        jump.GetInput(KeyCode.Space);
    }
    private void FixedUpdate()
    {
        controlVector = Vector2.zero;
        externalVector = Vector2.zero;

        if (!jump.IsJumping)
        {
            // Apply Gravity
            if (!groundDetection.OnGround)
            {
                gravity.CalcGravity();
                controlVector.y = gravity.value;
            }
            // Check Jump Input
            else if (jump.InputPressed)
            {
                jump.StartJump();
                groundDetection.ResetData();
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

        //// Check Slide Down
        //if (groundDetection.OnGround)
        //    groundDetection.SlideDown(gravity.gravityAccel, gravity.maxFallSpeed);

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

            // Detect Ground
            groundDetection.DetectGround(
                !jump.IsJumping,
                gravity.gravityAccel,
                gravity.maxFallSpeed,
                new List<Collider2D>());
        }
    }
}
