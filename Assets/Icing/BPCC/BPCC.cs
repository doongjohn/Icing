// --------------------------------------------------------
// BPCC: Box Platformer Character Controller
// --------------------------------------------------------
// #1. It uses Rigidbody2D.
// #2. Only supports BoxCollider2D for a character collider.
// #3. Ground detection will not work if a character is rotated.
// #4. Ground detection doesn't work well on a high angle slope
//     if the innerGap is too small.
// #5. Ground detection is not meant to handle "wall climbing". (See #4)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public class BPCC_BodyData
    {
        public Transform transform;
        public Rigidbody2D rb2D;
        public BoxCollider2D collider;
        public BoxCollider2D oneWayCollider;
        public Vector2 colliderSize;

        // World Space size
        public Vector2 Size => transform.lossyScale * colliderSize;

        public void Init(
            Transform transform,
            Rigidbody2D rb2D,
            BoxCollider2D collider,
            BoxCollider2D oneWayCollider)
        {
            this.transform = transform;
            this.rb2D = rb2D;
            this.collider = collider;
            this.oneWayCollider = oneWayCollider;
            this.colliderSize = collider.size;
        }
    }

    [Serializable]
    public class BPCC_Gravity
    {
        public float gravityAccel;
        public float maxFallSpeed;
        [HideInInspector] public float value;
        [HideInInspector] public BPCC_BodyData bodyData;
        [HideInInspector] public CountedBool UseGravity = new CountedBool();

        public void Init(BPCC_BodyData bodyData, bool useGravity = true)
        {
            this.bodyData = bodyData;
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

        [SerializeField, Range(0, int.MaxValue)]
        private int maxDetectCount = 50;

        [SerializeField, Range(1, 89)]
        private float maxWalkAngle = 70f;

        [SerializeField, Range(0, int.MaxValue)]
        private float innerGap = 0.1f;

        [SerializeField, Range(0, int.MaxValue)]
        private float snapLength = 0.1f;

        [HideInInspector] public BPCC_BodyData bodyData;
        [SerializeField] private LayerMask solidLayer;
        [SerializeField] private LayerMask oneWayLayer;
        private CircleCollider2D ccol;

        private float contactOffset;
        private Vector2 prevPos;
        private LayerMask groundLayer;
        private RaycastHit2D[] hitBuffer;
        private HashSet<Collider2D> ignoreGrounds = new HashSet<Collider2D>();
        private BPCC_GroundData groundData;
        private bool inputFallThrough = false;

        public Vector2 slideDownVector;

        public int MaxDetectCount => maxDetectCount;
        public BPCC_GroundData GroundData => groundData;
        public bool OnGround { get; private set; } = false;
        public bool OnSteepSlope { get; private set; } = false;

        /// <summary>
        /// Initialize Ground Detection
        /// </summary>
        /// <param name="bodyData">BodyData of the character.</param>
        /// <param name="ccol">This Circle Collider will prevent the character from phasing through the ceiling.</param>
        public void Init(BPCC_BodyData bodyData, CircleCollider2D ccol)
        {
            this.bodyData = bodyData;
            this.ccol = ccol;
            maxDetectCount = Mathf.Max(maxDetectCount, 0);
            maxWalkAngle = Mathf.Clamp(maxWalkAngle, 0, 89);
            innerGap = Mathf.Max(innerGap, 0);
            snapLength = Mathf.Max(snapLength, 0);

            contactOffset = Physics2D.defaultContactOffset;
            prevPos = bodyData.transform.position;
            groundLayer = LayerMaskHelper.Create(solidLayer, oneWayLayer);
            hitBuffer = new RaycastHit2D[maxDetectCount];

            ApplyInnerGap();

            // TODO: set one way collider surface arc depending on the maxWalkAngle
        }

        public void ApplyInnerGap()
        {
            float innerGap = this.innerGap / bodyData.transform.lossyScale.y;
            bodyData.collider.size = bodyData.oneWayCollider.size = bodyData.colliderSize.Add(y: -innerGap);
            bodyData.collider.offset = bodyData.oneWayCollider.offset = new Vector2(0, innerGap * 0.5f);
        }
        public void ResetInnerGap()
        {
            bodyData.collider.size = bodyData.oneWayCollider.size = bodyData.colliderSize;
            bodyData.collider.offset = bodyData.oneWayCollider.offset = Vector2.zero;
        }

        public void ResetData()
        {
            prevPos = bodyData.transform.position;
            // Array.Clear(hitBuffer, 0, hitBuffer.Length);
            groundData.Reset();
            slideDownVector = Vector2.zero;
            OnGround = false;
            OnSteepSlope = false;
        }
        public void DetectGround(bool detectCondition, BPCC_Gravity gravity)
        {
            // TODO: 단방향 플랫폼이 몸에 겹쳐져 있을 경우 무시함. (어떻게 하지??)

            if (!detectCondition)
            {
                ResetData();
                return;
            }

            #region Current Data

            RaycastHit2D finalHitData = new RaycastHit2D();
            Vector2 pos = bodyData.transform.position;
            Vector2 size = bodyData.Size.Add(amount: contactOffset);
            Vector2 halfSize = size * 0.5f;
            Vector2 vel = bodyData.rb2D.velocity;
            bool inValley = false;

            #endregion

            #region Previous Frame Data

            Vector2 prevVector = pos - prevPos;
            float prevDistY = Mathf.Abs(prevVector.y);
            float prevDistX = Mathf.Abs(prevVector.x);
            int prevDirX = prevVector.x.Sign0();

            #endregion

            #region Get Ground Method

            RaycastHit2D BoxCastGetHighestGround(BoxCastData castData, bool skipInngerGapUp = true)
            {
                RaycastHit2D hitData = finalHitData;
                int hitCount = Physics2D.BoxCastNonAlloc(castData.pos, castData.size, 0f, castData.dir, hitBuffer, castData.dist, groundLayer);
                for (int i = 0; i < hitCount; i++)
                {
                    if (ignoreGrounds.Contains(hitBuffer[i].collider))
                        continue;

                    if (hitBuffer[i].normal.y <= 0)
                        continue;

                    if (skipInngerGapUp && hitBuffer[i].point.y > pos.y - halfSize.y + innerGap)
                        continue;

                    // NOTE: 떨어질 때 단방향 플랫폼 검사 안함.
                    // FIXME: 빠르게 움직이면 단방향 플랫폼을 뚫고 가네...
                    if (oneWayLayer.ContainsLayer(hitBuffer[i].collider.gameObject.layer))
                    {
                        if (hitBuffer[i].point.y > pos.y - halfSize.y + innerGap)
                        {
                            if (ignoreGrounds.Contains(hitBuffer[i].collider))
                                continue;

                            Physics2D.IgnoreCollision(bodyData.oneWayCollider, hitBuffer[i].collider, true);
                            ignoreGrounds.Add(hitBuffer[i].collider);
                            continue;
                        }
                    }

                    if (hitData.collider == null || (hitData.collider != null && hitBuffer[i].point.y >= hitData.point.y))
                        hitData = hitBuffer[i];
                }
                return hitData;
            }

            void GetGround_StraightDown()
            {
                var dist = innerGap + snapLength + (
                    OnGround
                    ? prevVector.y > 0
                        ? prevDistY
                        : 0
                    : prevVector.y < 0
                        ? prevDistY
                        : 0
                );

                var hitData = BoxCastGetHighestGround(
                    new()
                    {
                        pos = pos.Add(y: innerGap),
                        size = size,
                        dir = Vector2.down,
                        dist = dist
                    }
                );

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
            }

            void GetGround_DownSlope()
            {
                //  ---->
                //  ┗┛
                //  /\└┘
                // /  \
                // detect the slope that is steeper than the current ground

                float downDist = Mathf.Max(vel.y * Time.deltaTime, contactOffset);

                var hitData = BoxCastGetHighestGround(
                    new()
                    {
                        pos = pos.Add(y: -downDist * 0.5f),
                        size = size.Add(y: downDist),
                        dir = Vector2.left * prevVector.x,
                        dist = prevDistX + contactOffset
                    }
                );

                if (hitData.collider == null)
                    return;

                // 낮은 경사의 오차를 해결하기 위함.
                if (hitData.normal.x == 0)
                    hitData.normal = hitData.normal.Change(x: Mathf.Sign(prevVector.x) * float.Epsilon);

                finalHitData = hitData;
            }

            void GetGround_Cross()
            {
                //  ----->
                //  ┗┛/\└┘
                //   /  \
                // prevents shooting off from the slope while walking up the slope

                var hitData = BoxCastGetHighestGround(
                    new()
                    {
                        pos = pos.Change(y: prevPos.y),
                        size = size,
                        dir = Vector2.left * prevVector.x,
                        dist = prevDistX
                    }
                );

                if (hitData.collider == null)
                    return;

                finalHitData = hitData;
            }

            #endregion

            #region Get Final hit data

            // apparently this exact order is very important
            if (prevDistX != 0)
                GetGround_DownSlope();

            GetGround_StraightDown();

            if (OnGround && finalHitData.collider == null && prevVector.y > 0)
                GetGround_Cross();

            // \    /
            //  \┗┛/
            //   \/
            // if the character is in a valley
            if (finalHitData.collider != null && finalHitData.normal.y != 0 && finalHitData.normal.y > 0)
            {
                var colR = Physics2D.Raycast(pos.Add(x: halfSize.x), Vector2.down, halfSize.y, groundLayer).collider;
                var colL = Physics2D.Raycast(pos.Add(x: -halfSize.x), Vector2.down, halfSize.y, groundLayer).collider;
                inValley =
                    colR && !ignoreGrounds.Contains(colR) &&
                    colL && !ignoreGrounds.Contains(colL);
            }

            #endregion

            #region Set Ground Data

            // Not on Ground
            if (finalHitData.collider == null)
            {
                ResetData();
            }
            // On Ground
            else
            {
                #region Set Ground Data

                OnGround = true;
                groundData.collider = finalHitData.collider;
                groundData.gameObject = finalHitData.collider.gameObject;
                groundData.hitPoint = finalHitData.point;
                groundData.normal = inValley ? Vector2.up : finalHitData.normal;

                #endregion

                #region Snap To Ground

                Vector2 snapPos = bodyData.transform.position;

                if (prevDistY != 0)
                    snapPos.y = groundData.hitPoint.Value.y + halfSize.y;

                if (prevDirX != 0 && groundData.normal.Value.x != 0)
                    snapPos.x = groundData.hitPoint.Value.x + (halfSize.x * Mathf.Sign(groundData.normal.Value.x));

                bodyData.transform.position = snapPos;

                #endregion

                #region Calc Slide Down Vector

                OnSteepSlope = false;
                slideDownVector = Vector2.zero;

                if (groundData.normal.Value.x != 0)
                {
                    float groundAngle = groundData.normal.Value.GetSurfaceAngle2D();
                    if (groundAngle > maxWalkAngle && !Mathf.Approximately(groundAngle, maxWalkAngle))
                    {
                        OnSteepSlope = true;

                        Vector3 crossDir = groundData.normal.Value.x > 0
                            ? Vector3.forward
                            : Vector3.back;

                        slideDownVector =
                            Vector3.Cross(crossDir, groundData.normal.Value).normalized *
                            Mathf.Max(vel.y + (gravity.gravityAccel * Time.deltaTime), gravity.maxFallSpeed);
                    }
                }

                #endregion
            }

            #endregion

            #region Circle Collider

            // FIXME: this can collide with one way grounds
            // so... I need to rework one way ground handling
            ccol.offset = ccol.offset.Change(y: 0);
            if (OnGround)
            {
                pos = bodyData.transform.position;
                var r = ccol.radius;
                var hit = Physics2D.CircleCast(pos, r + contactOffset * 2, Vector2.down, halfSize.y, groundLayer);
                if (hit.collider != null)
                {
                    var colMoveDist = pos.y - (hit.point.y + (hit.normal.y * r));
                    ccol.offset = ccol.offset.Change(y: -colMoveDist);
                }
            }

            #endregion

            // Update Previous Position
            prevPos = bodyData.transform.position;
        }

        public void GetInput_FallThrough(KeyCode keyCode)
        {
            if (Input.GetKeyDown(keyCode))
                inputFallThrough = true;
        }
        public void FallThrough()
        {
            #region Full Overlap Box

            Collider2D[] fullOverlap =
                Physics2D.OverlapBoxAll(
                    bodyData.transform.position,
                    bodyData.Size,
                    0f,
                    oneWayLayer);

            #endregion

            #region Enable Collsion

            if (fullOverlap.Length != 0)
            {
                for (int i = 0; i < fullOverlap.Length; i++)
                {
                    if (ignoreGrounds.Contains(fullOverlap[i]))
                        continue;

                    Physics2D.IgnoreCollision(bodyData.oneWayCollider, fullOverlap[i], false);
                    ignoreGrounds.Remove(fullOverlap[i]);
                }
            }
            else
            {
                foreach (var item in ignoreGrounds)
                    Physics2D.IgnoreCollision(bodyData.oneWayCollider, item, false);
                ignoreGrounds.Clear();
            }

            #endregion

            #region Disable Collision (Fall Through)

            if (!inputFallThrough)
                return;

            inputFallThrough = false;

            if (!OnGround)
                return;

            for (int i = 0; i < fullOverlap.Length; i++)
            {
                if (ignoreGrounds.Contains(fullOverlap[i]))
                    continue;

                Physics2D.IgnoreCollision(bodyData.oneWayCollider, fullOverlap[i], true);
                ignoreGrounds.Add(fullOverlap[i]);
            }

            #endregion
        }
    }

    [Serializable]
    public class BPCC_Walk
    {
        public CountedBool CanWalk = new();
        [SerializeField] public float maxSpeed;
        [SerializeField] public float minSpeed;
        [SerializeField] public float accel;
        [SerializeField] public float decel;
        [SerializeField, Range(0, 1)]
        private float changeDirPreserveSpeed;

        private int inputDir = 0;
        private int moveDir = 0;
        [HideInInspector] public float curWalkSpeed;

        public int InputDir => inputDir;
        public Vector2 WalkDir
        { get; private set; } = Vector2.zero;
        public Vector2 WalkVector
        { get; private set; } = Vector2.zero;
        public int MoveDir => moveDir;

        public void Init(bool canWalk = true)
        {
            CanWalk.Set(canWalk);
        }

        public void GetInput(KeyCode plusKey, KeyCode minusKey)
        {
            if (!CanWalk.Value || (!Input.GetKey(plusKey) && !Input.GetKey(minusKey)))
            {
                inputDir = 0;
                return;
            }

            if (Input.GetKeyDown(plusKey) || (Input.GetKey(plusKey) && Input.GetKeyUp(minusKey)))
                inputDir = 1;
            if (Input.GetKeyDown(minusKey) || (Input.GetKey(minusKey) && Input.GetKeyUp(plusKey)))
                inputDir = -1;
        }
        public void ResetInput()
        {
            inputDir = 0;
        }

        public void CalcWalkVector(BPCC_GroundData groundData)
        {
            if (inputDir != 0)
            {
                if (moveDir != inputDir)
                    curWalkSpeed *= changeDirPreserveSpeed;

                moveDir = inputDir;
            }

            if (groundData.collider == null || (groundData.normal.HasValue && groundData.normal.Value == Vector2.up))
            {
                WalkDir = Vector2.right * moveDir;
            }
            else
            {
                Vector3 crossDir;
                if (moveDir == 1)
                    crossDir = Vector3.back;
                else
                    crossDir = Vector3.forward;
                WalkDir = Vector3.Cross(crossDir, groundData.normal.Value).normalized;
            }

            if (inputDir != 0)
            {
                curWalkSpeed += accel * Time.deltaTime;
                curWalkSpeed = Mathf.Clamp(curWalkSpeed, minSpeed, maxSpeed);
            }
            else
            {
                curWalkSpeed -= decel * Time.deltaTime;
                curWalkSpeed = Mathf.Max(curWalkSpeed, 0);
            }

            WalkVector = WalkDir * curWalkSpeed;
        }
    }

    [Serializable]
    public class BPCC_Jump
    {
        public CountedBool CanJump = new();
        [SerializeField] private MovementCurve jumpCurve;
        [SerializeField] private BPCC_BodyData bodyData;
        [SerializeField, Range(0, int.MaxValue)] public int airJumpCount;
        [HideInInspector] public int curAirJumpCount;

        private bool inputPressed = false;
        private float? startPosY = null;
        private float? jumpVelocity = null;

        public bool IsJumping
        { get; private set; } = false;
        public MovementCurve JumpCurve => jumpCurve;
        public bool InputPressed => inputPressed;
        public float? JumpVelocity => jumpVelocity;
        public bool CanAirJump => curAirJumpCount < airJumpCount;

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

        public void EndJump()
        {
            IsJumping = false;
            jumpCurve.curTime = 0;
            startPosY = null;
            jumpVelocity = null;
        }
        public void StartJump()
        {
            EndJump();
            IsJumping = true;
            startPosY = bodyData.transform.position.y;
        }
        public void StartAirJump()
        {
            StartJump();
            curAirJumpCount++;
        }
        public void ResetAirJumpCount()
        {
            curAirJumpCount = 0;
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
