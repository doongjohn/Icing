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
        float x = 0;

        TimeCode(
            new System.Diagnostics.Stopwatch(),
            100000,
            new TestCodeObj()
            {
                testName = "4 if",
                code = () =>
                {
                    if (x == 100)
                        return;

                    if (x != 100)
                        return;

                    if (x != 1000)
                        return;

                    if (x != 3000)
                        return;

                    x = 10;
                }
            },
            new TestCodeObj()
            {
                testName = "1 if",
                code = () =>
                {
                    bool yay = x == 100 || x != 100 || x != 1000 || x != 1000;

                    if (yay)
                        return;

                    x = 10;
                }
            }
        );
    }
}
