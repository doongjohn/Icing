using System;
using UnityEngine;

namespace Icing
{
    public static class TestCode
    {
        private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        public static void TimeCode(int runCount, params (string testName, Action code)[] testCodeData)
        {
            foreach (var testCode in testCodeData)
            {
                Debug.Log($"[TestCode] Start: \"{testCode.testName}\"");
                stopwatch.Start();
                for (int i = 0; i < runCount; i++)
                {
                    testCode.code();
                }
                stopwatch.Stop();
                Debug.Log($"[TestCode] End: \"{testCode.testName}\" Execution time: {stopwatch.ElapsedMilliseconds} (ms)");
            }
        }
    }
}