using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Icing
{
    public static class EnumHelper
    {
        public static int Count<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Length;
        }
    }

    public static class PathHelper
    {
        public static bool IsValidFilePath(this string path)
        {
            try
            {
                string testPath = path + ".~~icing~~tmp";
                File.Create(testPath).Dispose();
                File.Delete(testPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class MathHelper
    {
        public static int Sign0(this int num)
        {
            if (num == 0) return 0;
            return (int)Mathf.Sign(num);
        }
        public static int Sign0(this float num)
        {
            if (num == 0) return 0;
            return (int)Mathf.Sign(num);
        }
    }

    public static class VectorHelper
    {
        public static Vector2 Change(this Vector2 vec, float? x = null, float? y = null)
        {
            if (x.HasValue) vec.x = x.Value;
            if (y.HasValue) vec.y = y.Value;
            return vec;
        }
        public static Vector3 Change(this Vector3 vec, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) vec.x = x.Value;
            if (y.HasValue) vec.y = y.Value;
            if (z.HasValue) vec.z = z.Value;
            return vec;
        }

        public static Vector2 Add(this Vector2 vec, float amount)
        {
            vec.x += amount;
            vec.y += amount;
            return vec;
        }
        public static Vector2 Add(this Vector2 vec, float? x = null, float? y = null)
        {
            if (x.HasValue) vec.x += x.Value;
            if (y.HasValue) vec.y += y.Value;
            return vec;
        }
        public static Vector3 Add(this Vector3 vec, float amount)
        {
            vec.x += amount;
            vec.y += amount;
            vec.z += amount;
            return vec;
        }
        public static Vector3 Add(this Vector3 vec, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) vec.x += x.Value;
            if (y.HasValue) vec.y += y.Value;
            if (z.HasValue) vec.z += z.Value;
            return vec;
        }

        public static Vector2 Mul(this Vector2 vec, float? x = null, float? y = null)
        {
            if (x.HasValue) vec.x *= x.Value;
            if (y.HasValue) vec.y *= y.Value;
            return vec;
        }
        public static Vector2 Mul(this Vector3 vec, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) vec.x *= x.Value;
            if (y.HasValue) vec.y *= y.Value;
            if (z.HasValue) vec.z *= z.Value;
            return vec;
        }

        public static Vector2 Div(this Vector2 vec, float? x = null, float? y = null)
        {
            if (x.HasValue) vec.x /= x.Value;
            if (y.HasValue) vec.y /= y.Value;
            return vec;
        }
        public static Vector2 Div(this Vector3 vec, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) vec.x /= x.Value;
            if (y.HasValue) vec.y /= y.Value;
            if (z.HasValue) vec.z /= z.Value;
            return vec;
        }

        public static Vector2 Clamp(this Vector2 vec, Vector2? min, Vector2? max)
        {
            vec.x = Mathf.Clamp(vec.x, min == null ? float.MinValue : min.Value.x, max == null ? float.MinValue : max.Value.x);
            vec.y = Mathf.Clamp(vec.y, min == null ? float.MinValue : min.Value.y, max == null ? float.MinValue : max.Value.y);
            return vec;
        }
        public static Vector3 Clamp(this Vector3 vec, Vector3? min, Vector3? max)
        {
            vec.x = Mathf.Clamp(vec.x, min == null ? float.MinValue : min.Value.x, max == null ? float.MinValue : max.Value.x);
            vec.y = Mathf.Clamp(vec.y, min == null ? float.MinValue : min.Value.y, max == null ? float.MinValue : max.Value.y);
            vec.z = Mathf.Clamp(vec.z, min == null ? float.MinValue : min.Value.z, max == null ? float.MinValue : max.Value.z);
            return vec;
        }
        public static Vector2 Clamp(this Vector2 vec,
            float? minX = null,
            float? maxX = null,
            float? minY = null,
            float? maxY = null)
        {
            vec.x = Mathf.Clamp(vec.x, minX ?? float.MinValue, maxX ?? float.MaxValue);
            vec.y = Mathf.Clamp(vec.y, minY ?? float.MinValue, maxY ?? float.MaxValue);
            return vec;
        }
        public static Vector3 Clamp(this Vector3 vec,
            float? minX = null,
            float? maxX = null,
            float? minY = null,
            float? maxY = null,
            float? minZ = null,
            float? maxZ = null)
        {
            vec.x = Mathf.Clamp(vec.x, minX ?? float.MinValue, maxX ?? float.MaxValue);
            vec.y = Mathf.Clamp(vec.y, minY ?? float.MinValue, maxY ?? float.MaxValue);
            vec.y = Mathf.Clamp(vec.z, minZ ?? float.MinValue, maxZ ?? float.MaxValue);
            return vec;
        }

        public static Vector2 Sign(this Vector2 vec)
        {
            vec.x = Mathf.Sign(vec.x);
            vec.y = Mathf.Sign(vec.y);
            return vec;
        }
        public static Vector3 Sign(this Vector3 vec)
        {
            vec.x = Mathf.Sign(vec.x);
            vec.y = Mathf.Sign(vec.y);
            vec.z = Mathf.Sign(vec.z);
            return vec;
        }
        public static Vector2 Sign0(this Vector2 vec)
        {
            vec.x = vec.x.Sign0();
            vec.y = vec.y.Sign0();
            return vec;
        }
        public static Vector3 Sign0(this Vector3 vec)
        {
            vec.x = vec.x.Sign0();
            vec.y = vec.y.Sign0();
            vec.z = vec.z.Sign0();
            return vec;
        }

        public static Vector2 Abs(this Vector2 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            return vec;
        }
        public static Vector3 Abs(this Vector3 vec)
        {
            vec.x = Mathf.Abs(vec.x);
            vec.y = Mathf.Abs(vec.y);
            vec.z = Mathf.Abs(vec.z);
            return vec;
        }

        public static float GetSurfaceAngle2D(this Vector2 surfaceNormal)
        {
            Vector2 slopeDir = Vector3.Cross(Vector3.forward, surfaceNormal);
            float rawAngle = Vector2.Angle(Vector2.right, slopeDir);
            return (rawAngle > 90) ? 180 - rawAngle : rawAngle;
        }
    }

    public static class LayerMaskHelper
    {
        public static LayerMask Create(params string[] layerNames)
        {
            return NamesToMask(layerNames);
        }
        public static LayerMask Create(params LayerMask[] layerNumbers)
        {
            return LayerNumbersToMask(layerNumbers);
        }

        public static LayerMask NamesToMask(params string[] layerNames)
        {
            LayerMask result = 0;
            foreach (var name in layerNames)
            {
                result |= LayerMask.NameToLayer(name);
            }
            return result;
        }
        public static LayerMask LayerNumbersToMask(params LayerMask[] layerNumbers)
        {
            LayerMask result = 0;
            foreach (var layer in layerNumbers)
            {
                result |= layer;
            }
            return result;
        }

        public static LayerMask Inverse(this LayerMask original)
        {
            return ~original;
        }
        public static LayerMask AddMask(this LayerMask original, params LayerMask[] layerNumbers)
        {
            return original | LayerNumbersToMask(layerNumbers);
        }
        public static LayerMask AddMask(this LayerMask original, params string[] layerNames)
        {
            return original | NamesToMask(layerNames);
        }
        public static LayerMask RemoveMask(this LayerMask original, params LayerMask[] layerNumbers)
        {
            LayerMask invertedOriginal = ~original;
            return ~(invertedOriginal | LayerNumbersToMask(layerNumbers));
        }
        public static LayerMask RemoveMask(this LayerMask original, params string[] layerNames)
        {
            LayerMask invertedOriginal = ~original;
            return ~(invertedOriginal | NamesToMask(layerNames));
        }

        public static string[] MaskToNames(this LayerMask original)
        {
            var output = new List<string>();

            for (int i = 0; i < 32; ++i)
            {
                int shifted = 1 << i;
                if ((original & shifted) == shifted)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        output.Add(layerName);
                    }
                }
            }
            return output.ToArray();
        }
        public static string MaskToString(this LayerMask original)
        {
            return MaskToString(original, ", ");
        }
        public static string MaskToString(this LayerMask original, string delimiter)
        {
            return string.Join(delimiter, MaskToNames(original));
        }

        public static bool ContainsLayer(this LayerMask layermMask, LayerMask layer)
        {
            return layermMask == (layermMask | (1 << layer));
        }
    }

    public static class SortHelper
    {
        public static T GetClosest<T>(this Collider2D[] cols, Transform pivot) where T : class
        {
            if (cols == null || cols.Length == 0)
                return null;

            T result = null;

            float dist = -1;
            for (int i = 0; i < cols.Length; i++)
            {
                float curDist = Vector2.Distance(cols[i].transform.position, pivot.position);

                if (dist == -1 || dist > curDist)
                {
                    T cur = cols[i].GetComponent<T>();
                    if (cur == null)
                        continue;

                    dist = curDist;
                    result = cur;
                }
            }

            return result;
        }
        public static GameObject GetClosest(this Collider2D[] cols, Transform pivot)
        {
            if (cols == null || cols.Length == 0)
                return null;

            GameObject result = null;

            float dist = -1;
            for (int i = 0; i < cols.Length; i++)
            {
                float curDist = Vector2.Distance(cols[i].transform.position, pivot.position);

                if (dist == -1 || dist > curDist)
                {
                    GameObject cur = cols[i].gameObject;
                    if (cur == null)
                        continue;

                    dist = curDist;
                    result = cur;
                }
            }

            return result;
        }
    }

    public static class GameObjectHelper
    {
        public static bool IsPrefab(this GameObject go)
        {
            return !go.scene.IsValid();
        }
        public static void GetComponent<T>(this GameObject go, out T variable) where T : Component
        {
            variable = go.GetComponent<T>();
        }

        public static void ClearAllChildren(this GameObject target)
        {
            for (int i = 0; i < target.transform.childCount; i++)
            {
                GameObject.Destroy(target.transform.GetChild(i).gameObject);
            }
        }
        public static void ClearAllChildren(this Transform target)
        {
            for (int i = 0; i < target.childCount; i++)
            {
                GameObject.Destroy(target.GetChild(i).gameObject);
            }
        }
    }

    public static class AnimatorHelper
    {
        public static float CurrentClipTime(this Animator animator)
        {
            return animator.GetCurrentAnimatorStateInfo(0).length * (1 - animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        public static float CurrentClipLength(this Animator animator)
        {
            return animator.GetCurrentAnimatorStateInfo(0).length;
        }

        public static bool CheckCurrentClipName(this Animator animator, string nameToCompare)
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsName(nameToCompare);
        }
        public static float GetNormalizedTime(this Animator animator)
        {
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public static void SetDuration(this Animator animator, float duration, string animName = null)
        {
            if ((animName != null && !animator.CheckCurrentClipName(animName)) || duration <= 0)
            {
                animator.speed = 1;
                return;
            }

            animator.speed = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / duration;
        }
        public static void SetDuration(this Animator animator, float duration, float maxDuration, string animName = null)
        {
            if ((animName != null && !animator.CheckCurrentClipName(animName)) || duration <= 0)
            {
                animator.speed = 1;
                return;
            }

            duration = maxDuration <= 0 || duration < maxDuration ? duration : maxDuration;
            animator.speed = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / duration;
        }
        public static void ResetSpeed(this Animator animator)
        {
            animator.speed = 1;
        }
    }

    public static class CameraHelper
    {
        public static Camera MainCam { private set; get; }

        public static void Init(Camera mainCam)
        {
            MainCam = mainCam;
        }
    }

    public static class FlipHelper
    {
        // dir:  1 => Right, Up
        // dir: -1 => Left, Down

        public static void FlipXTo(Transform target, int dir)
        {
            //target.localRotation = Quaternion.Euler(target.localEulerAngles.Change(y: dir == 1 ? 0f : 180f));
            target.localEulerAngles = target.localEulerAngles.Change(y: dir == 1 ? 0f : 180f);
        }
        public static void FlipYTo(Transform target, int dir)
        {
            //target.localRotation = Quaternion.Euler(target.localEulerAngles.Change(x: dir == 1 ? 0f : 180f));
            target.localEulerAngles = target.localEulerAngles.Change(x: dir == 1 ? 0f : 180f);
        }

        public static void FlipXTo(SpriteRenderer target, int dir)
        {
            target.flipX = dir != 1;
        }
        public static void FlipYTo(SpriteRenderer target, int dir)
        {
            target.flipY = dir != 1;
        }
    }

    public static class MouseHelper
    {
        public static Vector2 GetMouseDir2D(this Vector2 pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;
            return ((Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - pivot).normalized;
        }
        public static Vector2 GetMouseDir2D(this Vector3 pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;
            return ((Vector2)(cam.ScreenToWorldPoint(Input.mousePosition) - pivot)).normalized;
        }

        public static void FlipXToMouse(this SpriteRenderer target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;
            target.flipX = cam.ScreenToWorldPoint(Input.mousePosition).x - pivot.position.x <= 0;
        }
        public static void FlipYToMouse(this SpriteRenderer target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;
            target.flipY = cam.ScreenToWorldPoint(Input.mousePosition).y - pivot.position.y <= 0;
        }

        public static void FlipXToMouse(this Transform target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;

            target.localEulerAngles = target.localEulerAngles.
                Change(y: cam.ScreenToWorldPoint(Input.mousePosition).x - pivot.position.x > 0 ? 0f : 180f);
        }
        public static void FlipYToMouse(this Transform target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;

            target.localEulerAngles = target.localEulerAngles.
                Change(x: cam.ScreenToWorldPoint(Input.mousePosition).y - pivot.position.y > 0 ? 0f : 180f);
        }

        public static void LookAtMouse2D(this Transform target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;

            target.right = pivot.position.GetMouseDir2D(cam);

            if ((Vector2)target.right == Vector2.left)
                target.rotation = Quaternion.Euler(0f, 0f, 180f);
        }

        public static void AimMouse2D(this Transform target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;

            // Rotate To Mouse Dir (Absolute X)
            Vector2 mouseDir = pivot.position.GetMouseDir2D(cam);
            target.right = mouseDir.Change(x: Mathf.Abs(mouseDir.x));

            // Flip X
            target.FlipXToMouse(pivot, cam);
        }
        public static void AimMouse2D(this GameObject target, Transform pivot, Camera cam = null)
        {
            if (cam == null) cam = CameraHelper.MainCam;

            // Rotate To Mouse Dir (Absolute X)
            Vector2 mouseDir = pivot.position.GetMouseDir2D(cam);
            target.transform.right = mouseDir.Change(x: Mathf.Abs(mouseDir.x));

            // Flip X
            target.transform.FlipXToMouse(pivot, cam);
        }
    }
}
