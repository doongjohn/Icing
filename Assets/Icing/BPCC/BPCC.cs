// --------------------------------------------------------
// BPCC: Box Platformer Character Controller
// --------------------------------------------------------
// #1. It uses Rigidbody2D.
// #2. Only supports BoxCollider2D for a character collider.
// #3. This Ground detection will not work if a character is rotated.
// #4. This ground detection doesn't work well on a high angle slope if the innerGap is too small.
// #5. This ground detection is not meant to handle "wall climbing". (See #4)

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace Icing
{
    public class BPCC_BodyData
    {
        public Transform transform;
        public Rigidbody2D rb2D;
        public BoxCollider2D collider;
        public Vector2 colliderSize;

        // Wrold space size
        public Vector2 Size => transform.lossyScale * colliderSize;

        public void Init(Transform transform, Rigidbody2D rb2D, BoxCollider2D collider)
        {
            this.transform = transform;
            this.rb2D = rb2D;
            this.collider = collider;
            this.colliderSize = collider.size;
        }
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
            this.bodyData = bodyData;
            this.gravityAccel = gravityAccel;
            this.maxFallSpeed = maxFallSpeed;
            UseGravity.Set(useGravity);
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
        private Vector2 prevPos;

        public Vector2 slideDownVector;

        public int MaxDetectCount => maxDetectCount;
        public BPCC_GroundData GroundData => groundData;
        public bool OnGround
        { get; private set; } = false;
        public bool OnSteepSlope
        { get; private set; } = false;

        /// <summary>
        /// Initialize Ground Detection
        /// </summary>
        /// <param name="bodyData">BodyData of the character.</param>
        /// <param name="maxDetectCount">Capacity of the BoxCast hit result array.</param>
        /// <param name="maxWalkAngle">If a ground angle is bigger than maxWalkAngle, character will slide down.</param>
        /// <param name="snapLength">If the character and ground are closer than or equal to snapLength, character will snap to the ground.</param>
        /// <param name="innerGap">BoxCollider's size.y will shrink. (Increase this if the character acts weird on slope.)</param>
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

            prevPos = tf.position;
        }

        public void ApplyInnerGap()
        {
            float innerGap = this.innerGap / tf.lossyScale.y;
            bodyData.collider.size = bodyData.colliderSize.Add(y: -innerGap);
            bodyData.collider.offset = new Vector2(0, innerGap * 0.5f);
        }
        public void ResetInnerGap()
        {
            bodyData.collider.size = bodyData.colliderSize;
            bodyData.collider.offset = Vector2.zero;
        }

        public void ResetData()
        {
            OnGround = false;
            OnSteepSlope = false;
            slideDownVector = Vector2.zero;

            groundData.Reset();
            Array.Clear(hitArray, 0, hitArray.Length);

            ResetInnerGap();
        }
        public void DetectGround(bool detectCondition, float slideAccel, float maxSlideSpeed, List<Collider2D> ignoreGrounds)
        {
            // FIXME
            // 1. 벽이 진행 방향의 위에 있으면 innerGap 만큼 천장을 무시함.

            if (!detectCondition)
            {
                ResetData();
                goto END;
            }

            ApplyInnerGap();

            #region Current Data

            RaycastHit2D finalHitData = new RaycastHit2D();

            float contactOffset = Physics2D.defaultContactOffset;
            Vector2 pos = tf.position;
            Vector2 size = bodyData.Size.Add(amount: contactOffset);
            Vector2 halfSize = size * 0.5f;
            Vector2 vel = bodyData.rb2D.velocity;
            Vector2 velDir = bodyData.rb2D.velocity.normalized;
            bool inValley = false;

            #endregion

            #region Previous Frame Data

            Vector2 prevVector = pos - prevPos;
            float prevDistY = Mathf.Abs(prevVector.y);
            float prevDistX = Mathf.Abs(prevVector.x);
            int prevDirX = prevVector.x.Sign0();

            #endregion

            #region Get Ground Method

            RaycastHit2D GetHighestGround(BoxCastData castData, Func<RaycastHit2D, bool> skipCondition = null)
            {
                RaycastHit2D hitData = finalHitData;
                for (int i = 0; i < Physics2D.BoxCastNonAlloc(castData.pos, castData.size, 0f, castData.dir, hitArray, castData.dist, groundLayer); i++)
                {
                    if (ignoreGrounds.Contains(hitArray[i].collider)
                    ||  hitArray[i].normal.y <= 0
                    ||  (skipCondition?.Invoke(hitArray[i]) ?? false))
                        continue;

                    if (hitArray[i].point.y >= hitData.point.y
                    ||  hitData.collider == null)
                        hitData = hitArray[i];
                }
                return hitData;
            }
            void GetGround_StraightDown()
            {
                Debug.Log("(GroundDetection) Try StraightDown");

                RaycastHit2D hitData = GetHighestGround(
                    new BoxCastData()
                    {
                        pos = pos.Add(y: size.y),
                        size = size,
                        dir = Vector2.down,
                        dist = size.y + snapLength + prevDistY
                    },
                    (hit) => hit.point.y > pos.y - halfSize.y + innerGap);

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
                Debug.Log("(GroundDetection) StraightDown");
            }
            void GetGround_DownSlope()
            {
                // NOTE:
                // 많은 발전이 이루어질 수 있는 부분이다.
                // 지금으로서는 그냥 쓸만하다.

                float downDist = Mathf.Max(vel.y * Time.deltaTime, contactOffset);

                RaycastHit2D hitData = GetHighestGround(
                    new BoxCastData()
                    {
                        pos = pos.Add(y: -downDist * 0.5f),
                        size = size.Add(y: downDist),
                        dir = Vector2.left * prevVector.x,
                        dist = prevDistX + contactOffset
                    });

                if (hitData.collider == null)
                    return;

                // 낮은 경사의 오차를 해결하기 위함.
                if (hitData.normal.x == 0)
                    hitData.normal = hitData.normal.Change(x: Mathf.Sign(prevVector.x) * float.Epsilon);

                finalHitData = hitData;
                Debug.Log("(GroundDetection) DownSlope\n" + hitData.normal.x);
                Debug.DrawRay(hitData.point, hitData.normal, Color.red);
            }
            void GetGround_Cross()
            {
                Debug.Log("(GroundDetection) Try Cross");

                RaycastHit2D hitData = GetHighestGround(
                    new BoxCastData()
                    {
                        pos = pos.Change(y: prevPos.y),
                        size = size,
                        dir = Vector2.left * prevVector.x,
                        dist = prevDistX
                    });

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
                Debug.Log("(GroundDetection) Cross\n" + hitData.collider.name);
            }
            void CheckValley()
            {
                // 캐릭터의 양 끝에 경사가 있을 때
                // 평평한 땅 위에 있는 것과 같은 걸로 처리하기 위함.

                if (finalHitData.collider == null)
                    return;

                if (finalHitData.normal.x == 0)
                    return;

                var hitR = Physics2D.Raycast(pos.Add(x: halfSize.x), Vector2.down, halfSize.y, groundLayer);
                var hitL = Physics2D.Raycast(pos.Add(x: -halfSize.x), Vector2.down, halfSize.y, groundLayer);
                inValley = hitR.collider != null && hitL.collider != null;
            }

            #endregion

            #region Get Ground Data

            if (prevDistX != 0)
                GetGround_DownSlope();

            GetGround_StraightDown();

            if (finalHitData.collider == null && OnGround && prevVector.y > 0)
                GetGround_Cross();

            CheckValley();

            #endregion

            #region Set Ground Data

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
                        slideDownVector = 
                            Vector3.Cross(groundData.normal.Value.x > 0 ? Vector3.forward : Vector3.back, groundData.normal.Value).normalized *
                            Mathf.Max(vel.y + (slideAccel * Time.deltaTime), maxSlideSpeed);
                    }
                }

                #endregion

                #region Snap To Ground

                Vector3 snapPos = tf.position;

                // Snap Y
                snapPos.y = groundData.hitPoint.Value.y + halfSize.y;

                // Snap X
                if (groundData.normal.Value.x != 0 && prevDirX != 0)
                {
                    snapPos.x = groundData.hitPoint.Value.x + ((halfSize.x + contactOffset) * Mathf.Sign(groundData.normal.Value.x));
                    Debug.DrawRay(groundData.hitPoint.Value, groundData.normal.Value * 2f, Color.cyan);
                }

                // Apply Snap
                tf.position = snapPos;

                #endregion
            }

            #endregion

        END:
            // Update Previous Position
            prevPos = tf.position;
        }
    }

    [Serializable]
    public class BPCC_Walk
    {
        [SerializeField] private float walkSpeed;

        private int inputDir = 0;
        private BPCC_BodyData bodyData;

        public BoolCount CanWalk = new BoolCount();

        public bool IsWalking
        { get; private set; } = false;
        public int InputDir => inputDir;
        public Vector2 WalkDir
        { get; private set; } = Vector2.zero;
        public Vector2 WalkVector
        { get; private set; } = Vector2.zero;

        public void Init(BPCC_BodyData bodyData, float walkSpeed, bool canWalk = true)
        {
            this.bodyData = bodyData;
            this.walkSpeed = walkSpeed;
            CanWalk.Set(canWalk);
        }

        public void GetInput(KeyCode plusKey, KeyCode minusKey)
        {
            if (!CanWalk.Value)
            {
                inputDir = 0;
                return;
            }

            if (Input.GetKeyDown(plusKey))
                inputDir = 1;
            if (Input.GetKeyDown(minusKey))
                inputDir = -1;

            if (Input.GetKey(minusKey) && Input.GetKeyUp(plusKey))
                inputDir = -1;
            if (Input.GetKey(plusKey) && Input.GetKeyUp(minusKey))
                inputDir = 1;

            if (!Input.GetKey(plusKey) && !Input.GetKey(minusKey))
                inputDir = 0;
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
                    WalkDir = Vector3.Cross(inputDir == 1 ? Vector3.back : Vector3.forward, groundData.normal.Value).normalized;
                }
            }

            // TODO
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
            this.bodyData = bodyData;
            CanJump.Set(canJump);
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
