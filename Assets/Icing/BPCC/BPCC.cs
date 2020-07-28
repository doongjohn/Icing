// BPCC: Box Platformer Character Controller
// (only supports box collider)

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;

namespace Icing
{
    [Serializable]
    public class MovementCurve
    {
        public AnimationCurve curve;
        public float curTime;

        public float Value => curve.Evaluate(curTime);
        public float EndTime => curve.keys[curve.length - 1].time;
        public bool IsEnded => curve.keys[curve.length - 1].time <= curTime;
    }

    public class BPCC_BodyData
    {
        public Transform transform;
        public Rigidbody2D rb2D;
        public BoxCollider2D collider;
        public Vector2 colliderSize;

        // Wrold space size
        public Vector2 Size => transform.lossyScale * colliderSize;
    }

    [Serializable]
    public class BPCC_Gravity
    {
        public float gravityAccel;
        public float maxFallSpeed;
        public float value;
        public BPCC_BodyData bodyData;
        public BoolCount UseGravity = new BoolCount();

        public void Init(BPCC_BodyData bodyData, float gravityAccel, float maxFallSpeed, bool useGravity = true)
        {
            UseGravity.Set(useGravity);
            this.bodyData = bodyData;
            this.gravityAccel = gravityAccel;
            this.maxFallSpeed = maxFallSpeed;
        }

        public void CalcGravity()
        {
            value = Mathf.Max(bodyData.rb2D.velocity.y + (gravityAccel * Time.fixedDeltaTime), maxFallSpeed);
        }
    }

    public struct BPCC_GroundData
    {
        public Collider2D collider;
        public GameObject gameObject;
        public Vector2? normal;
        public Vector2? hitPoint;

        public void Reset()
        {
            collider = null;
            gameObject = null;
            normal = null;
            hitPoint = null;
        }
    }

    [Serializable]
    public class BPCC_GroundDetection
    {
        struct BoxCastData
        {
            public Vector2 pos;
            public Vector2 size;
            public Vector2 dir;
            public float dist;
        }

        [SerializeField] private int maxDetectCount = 50;
        [SerializeField] private float maxWalkAngle = 70f;
        [SerializeField] private float snapLength = 0.1f;
        [SerializeField] private float innerGap = 0.1f;
        [SerializeField] private BPCC_BodyData bodyData;
        [SerializeField] private LayerMask groundLayer;

        private RaycastHit2D[] hitArray;
        private BPCC_GroundData groundData;
        private Transform tf;
        private Rigidbody2D rb2D;
        private Vector2 prevPos;

        public Vector2 slideDownVector;

        public int MaxDetectCount => maxDetectCount;
        public BPCC_GroundData GroundData => groundData;
        public bool OnGround
        { get; private set; } = false;
        public bool OnSteepSlope
        { get; private set; } = false;

        public void Init(
            BPCC_BodyData bodyData,
            int maxDetectCount,
            float maxWalkAngle,
            float snapLength,
            float innerGap)
        {
            this.bodyData = bodyData;
            this.maxDetectCount = Mathf.Max(maxDetectCount, 0);
            this.maxWalkAngle = Mathf.Clamp(maxWalkAngle, 0, 89);
            this.snapLength = Mathf.Max(snapLength, 0);
            this.innerGap = Mathf.Max(innerGap, 0);

            hitArray = new RaycastHit2D[maxDetectCount];
            tf = bodyData.transform;
            rb2D = bodyData.rb2D;

            bodyData.collider.size = bodyData.collider.size.Add(y: -innerGap);
            bodyData.collider.offset = bodyData.collider.offset.Change(y: innerGap * 0.5f);

            prevPos = tf.position;
        }

        public void ResetData()
        {
            OnGround = false;
            OnSteepSlope = false;
            slideDownVector = Vector2.zero;

            groundData.Reset();
            Array.Clear(hitArray, 0, hitArray.Length);
        }
        public void DetectGround(bool detectCondition, float slideAccel, float maxSlideSpeed, List<Collider2D> ignoreGrounds)
        {
            // FIXME
            // 1. 60의 속도로 원 위를 지나가면 에어본 됨. (땅감지는 DownSlope)
            // 2. 벽이 진행 방향의 위에 있으면 머리가 벽에 낌.
            // 3. 벽이 진행 방향의 앞에 있으면 에어본 됨.
            // 4. 최고 경사 보다 높은 경사의 위에 있을 때 경사를 내려갈 수 없음.

            RaycastHit2D finalHitData = new RaycastHit2D();
            Vector2 halfSize = bodyData.Size * 0.5f;
            Vector2 vel = rb2D.velocity;
            bool inValley = false;

            if (!detectCondition)
                goto SET_GROUND_DATA;

            #region Current Data

            Vector2 pos = tf.position;
            Vector2 size = bodyData.Size;
            Vector2 velDir = rb2D.velocity.normalized;
            float moveDirX = vel.x.Sign0();

            #endregion

            #region Next Frame Data

            RaycastHit2D nextHit = Physics2D.BoxCast(pos, size, 0f, velDir, vel.magnitude * Time.deltaTime, groundLayer);
            Vector2 nextVector = nextHit.collider == null ? vel * Time.deltaTime : velDir * nextHit.distance;
            Vector2 nextPos = pos + nextVector;
            float nextDistY = Mathf.Abs(nextVector.y);
            float nextDistX = Mathf.Abs(nextVector.x);

            #endregion

            #region Previous Frame Data

            Vector2 prevVector = pos - prevPos;
            float prevDistY = Mathf.Abs(prevVector.y);
            float prevDistX = Mathf.Abs(prevVector.x);

            #endregion

            #region Get Ground Data

            RaycastHit2D GetHighestGround(BoxCastData castData, Func<RaycastHit2D, bool> skipCondition)
            {
                RaycastHit2D hitData = finalHitData;
                for (int i = 0; i < Physics2D.BoxCastNonAlloc(castData.pos, castData.size, 0f, castData.dir, hitArray, castData.dist, groundLayer); i++)
                {
                    if (ignoreGrounds.Contains(hitArray[i].collider) || hitArray[i].normal.y <= 0 || skipCondition(hitArray[i]))
                        continue;

                    if (hitArray[i].point.y >= hitData.point.y || hitData.collider == null)
                        hitData = hitArray[i];

                    //if (hitArray[i].distance < hitData.distance || hitData.collider == null)
                    //    hitData = hitArray[i];
                }
                return hitData;
            }
            void GetGround_StraightDown()
            {
                RaycastHit2D hitData = GetHighestGround(
                    new BoxCastData()
                    {
                        pos = pos.Add(y: innerGap),
                        size = size,
                        dir = Vector2.down,
                        dist = snapLength + innerGap + nextDistY
                    },
                    (hit) => hit.point.y > pos.y - halfSize.y + innerGap);

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
                //Debug.Log("(Ground Detection) StraightDown");
            }
            void GetGround_DownSlope()
            {
                float downDist = 
                    prevDistY != 0 
                    ? prevDistY
                    : float.Epsilon;

                RaycastHit2D hitData = GetHighestGround(
                    new BoxCastData()
                    {
                        pos = pos.Add(y: -downDist * 0.5f),
                        size = size.Add(y: downDist),
                        dir = Vector2.left * moveDirX,
                        dist = Mathf.Max(prevDistX, nextDistX)
                    },
                    (hit) => hit.point.y > pos.y - halfSize.y + innerGap);

                if (hitData.collider == null)
                    return;

                if (OnGround && hitData.normal == groundData.normal)
                    return;

                finalHitData = hitData;
                //Debug.Log("(Ground Detection) DownSlope");
            }
            void GetGround_Cross()
            {
                BoxCastData castData = new BoxCastData()
                {
                    pos = pos.Change(y: Mathf.Min(nextPos.y, prevPos.y)),
                    size = size,
                    dir = Vector2.left * moveDirX,
                    dist = nextDistX
                };

                Debug.DrawRay(pos.Change(y: prevPos.y), Vector2.left * moveDirX * nextDistX, Color.red);

                RaycastHit2D hitData = GetHighestGround(
                    castData,
                    (hit) => false);

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
                //Debug.Log("(Ground Detection) Cross\n" + hitData.collider.name);
            }
            void CheckValley()
            {
                var hitR = Physics2D.Raycast(pos.Add(x: halfSize.x), Vector2.down, halfSize.y, groundLayer);
                var hitL = Physics2D.Raycast(pos.Add(x: -halfSize.x), Vector2.down, halfSize.y, groundLayer);
                inValley = hitR.collider != null && hitL.collider != null;
            }

            #region Detect Ground

            CheckValley();
            if (vel.x != 0)
            {
                if (OnGround && groundData.normal.Value.x * moveDirX < 0)
                {
                    GetGround_Cross();
                }
                else
                {
                    GetGround_StraightDown();
                    if (finalHitData.collider == null)
                        GetGround_DownSlope();
                }
            }
            if (finalHitData.collider == null)
                GetGround_StraightDown();

            #endregion

            #endregion

            #region Set Ground Data

        SET_GROUND_DATA:
            if (finalHitData.collider == null)
            {
                ResetData();
                Debug.LogWarning("(Ground Detection) Airborne!!!");
            }
            else
            {
                OnGround = true;
                groundData.collider = finalHitData.collider;
                groundData.gameObject = finalHitData.collider.gameObject;
                groundData.normal = inValley ? Vector2.up : finalHitData.normal;
                groundData.hitPoint = finalHitData.point;

                #region Slide Down

                OnSteepSlope = false;
                slideDownVector = Vector2.zero;

                if (groundData.normal.Value.x != 0)
                {
                    float groundAngle = groundData.normal.Value.GetSurfaceAngle2D();

                    if (groundAngle > maxWalkAngle && !Mathf.Approximately(groundAngle, maxWalkAngle))
                    {
                        OnSteepSlope = true;
                        Vector2 slideDir = Vector3.Cross(groundData.normal.Value.x > 0 ? Vector3.forward : Vector3.back, (Vector3)groundData.normal).normalized;
                        slideDownVector = slideDir * Mathf.Max(vel.y + (slideAccel * Time.deltaTime), maxSlideSpeed);
                    }
                }

                #endregion

                #region Snap To Ground

                // Snap Y
                tf.position = tf.position.Change(y: halfSize.y + groundData.hitPoint.Value.y);

                // Snap X
                if (groundData.normal.Value.x != 0 && vel.x != 0)
                {
                    tf.position = tf.position.Change(x: groundData.hitPoint.Value.x + (halfSize.x * Mathf.Sign(groundData.normal.Value.x)));
                }

                #endregion
            }
            
            // Update Previous Position
            prevPos = tf.position;

            #endregion
        }
    }

    [Serializable]
    public class BPCC_Walk
    {
        [SerializeField] private float walkSpeed;

        private int inputDir = 0;

        public BoolCount CanWalk = new BoolCount();

        public bool IsWalking
        { get; private set; } = false;
        public int InputDir => inputDir;
        public Vector2 WalkDir
        { get; private set; } = Vector2.zero;
        public Vector2 WalkVector
        { get; private set; } = Vector2.zero;

        public void Init(float walkSpeed, bool canWalk = true)
        {
            CanWalk.Set(canWalk);
            this.walkSpeed = walkSpeed;
        }

        public void GetInput(KeyCode plusKey, KeyCode minusKey)
        {
            if (!CanWalk.Value)
            {
                inputDir = 0;
                return;
            }

            void GetDir(ref int dir, KeyCode _plusKey, KeyCode _minusKey)
            {
                if (Input.GetKeyDown(_plusKey))
                    dir = 1;
                if (Input.GetKeyDown(_minusKey))
                    dir = -1;

                if (Input.GetKey(_minusKey) && Input.GetKeyUp(_plusKey))
                    dir = -1;
                if (Input.GetKey(_plusKey) && Input.GetKeyUp(_minusKey))
                    dir = 1;

                if (!Input.GetKey(_plusKey) && !Input.GetKey(_minusKey))
                    dir = 0;
            }

            GetDir(ref inputDir, plusKey, minusKey);
        }
        public void ResetInput()
        {
            inputDir = 0;
        }

        public void CalcWalkVector(BPCC_GroundData groundData)
        {
            WalkDir = Vector2.zero;
            WalkVector = Vector2.zero;

            if (inputDir != 0)
            {
                if (groundData.collider == null || (groundData.normal.HasValue && groundData.normal.Value == Vector2.up))
                {
                    WalkDir = Vector2.right * inputDir;
                }
                else
                {
                    WalkDir = Vector3.Cross(inputDir == 1 ? Vector3.back : Vector3.forward, (Vector3)groundData.normal).normalized;
                }
            }

            // Do accel, decel, etc... calculation here
            WalkVector = WalkDir * walkSpeed;
        }
    }

    [Serializable]
    public class BPCC_Jump
    {
        [SerializeField] private MovementCurve jumpCurve;
        [SerializeField] private BPCC_BodyData bodyData;

        private bool inputPressed = false;
        private float? startPosY = null;
        private float? jumpVelocity = null;

        public BoolCount CanJump = new BoolCount();

        public bool IsJumping
        { get; private set; } = false;
        public bool InputPressed => inputPressed;
        public float? JumpVelocity => jumpVelocity;

        public void Init(BPCC_BodyData bodyData, bool canJump = true)
        {
            CanJump.Set(canJump);
            this.bodyData = bodyData;
        }

        public void GetInput(KeyCode key)
        {
            if (!CanJump.Value)
            {
                inputPressed = false;
                return;
            }

            if (Input.GetKeyDown(key))
                inputPressed = true;
        }
        public void ResetInput()
        {
            inputPressed = false;
        }

        public void StartJump()
        {
            IsJumping = true;
            startPosY = bodyData.transform.position.y;
        }
        public void EndJump()
        {
            IsJumping = false;
            jumpCurve.curTime = 0;
            startPosY = null;
            jumpVelocity = null;
        }
        public void CalcJumpVelocity()
        {
            // Jump Done
            if (jumpCurve.IsEnded || (jumpCurve.curTime > 0 && bodyData.rb2D.velocity.y <= 0))
            {
                EndJump();
                return;
            }

            jumpCurve.curTime += Time.fixedDeltaTime;
            jumpVelocity = (startPosY.Value + jumpCurve.Value - bodyData.transform.position.y) / Time.fixedDeltaTime;
        }
    }
}
