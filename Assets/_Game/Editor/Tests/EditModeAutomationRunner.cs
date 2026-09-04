using System.Collections.Generic;
using System;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public static class EditModeAutomationRunner
{
	private sealed class ResultCallback : ICallbacks
	{
		public bool Finished { get; private set; }

		public int FailCount { get; private set; }

		public int PassCount { get; private set; }

		public int SkipCount { get; private set; }

		public List<string> Failures { get; } = new List<string>();

		public void RunStarted(ITestAdaptor testsToRun)
		{
		}

		public void RunFinished(ITestResultAdaptor result)
		{
			Finished = true;
			FailCount = result.FailCount;
			PassCount = result.PassCount;
			SkipCount = result.SkipCount;
		}

		public void TestStarted(ITestAdaptor test)
		{
		}

		public void TestFinished(ITestResultAdaptor result)
		{
			if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
			{
				Failures.Add(result.FullName + ": " + result.Message);
			}
		}
	}

	public static void RunAll()
	{
		Run(new Filter
		{
			testMode = TestMode.EditMode,
			assemblyNames = new string[1] { "Eidren.Tests" }
		});
	}

	private static void Run(params Filter[] filters)
	{
		ResultCallback callback = new ResultCallback();
		TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
		testRunnerApi.RegisterCallbacks(callback);
		testRunnerApi.Execute(new ExecutionSettings(filters)
		{
			runSynchronously = true
		});
		if (!callback.Finished)
		{
			throw new InvalidOperationException("The synchronous EditMode test run did not finish.");
		}
		if (callback.FailCount > 0)
		{
			throw new InvalidOperationException($"EditMode tests failed: {callback.FailCount}. " + string.Join(Environment.NewLine, callback.Failures));
		}
		Debug.Log("Eidren: EditMode tests passed. " + $"Passed={callback.PassCount}, " + $"Skipped={callback.SkipCount}.");
	}
}
}
