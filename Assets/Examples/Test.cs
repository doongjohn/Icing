using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    Vector3 Change1(Vector3 vec, float? x = null, float? y = null, float? z = null)
    {
        if (x.HasValue) vec.x = x.Value;
        if (y.HasValue) vec.y = y.Value;
        if (z.HasValue) vec.z = z.Value;
        return vec;
    }

    Vector3 Change2(Vector3 vec, float? x = null, float? y = null, float? z = null)
    {
        vec.x = x ?? vec.x;
        vec.y = y ?? vec.y;
        vec.z = z ?? vec.z;
        return vec;
    }

    struct TestCodeObj
    {
        public string testName;
        public Action code;
    }

    void TimeCode(System.Diagnostics.Stopwatch stopwatch, int runCount, params TestCodeObj[] testCodeObjs)
    {
        foreach (var testCodeObj in testCodeObjs)
        {
            Debug.Log($"[{testCodeObj.testName}] Started!");
            stopwatch.Start();
            for (int i = 0; i < runCount; i++)
            {
                testCodeObj.code();
            }
            stopwatch.Stop();
            Debug.Log($"[{testCodeObj.testName}] Ended! Execution time: {stopwatch.ElapsedMilliseconds} (ms)");
        }
    }

    void Awake()
    {
        Vector3 vec = Vector3.zero;

        TimeCode(
            new System.Diagnostics.Stopwatch(),
            100000,
            new TestCodeObj()
            {
                testName = "Using if",
                code = () =>
                {
                    Change1(vec, x: 10);
                }
            },
            new TestCodeObj()
            {
                testName = "Not Using if",
                code = () =>
                {
                    Change2(vec, x: 10);
                }
            }
        );
    }
}
