using System.Collections.Generic;
using System;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public static class PlayModeAutomationRunner
{
	private sealed class ResultCallback : ICallbacks
	{
		private readonly List<string> failures = new List<string>();

		public void RunStarted(ITestAdaptor testsToRun)
		{
		}

		public void RunFinished(ITestResultAdaptor result)
		{
			SessionState.EraseBool("Eidren.PlayModeAutomationRunner.Active");
			int exitCode = ((result.FailCount > 0) ? 1 : 0);
			if (exitCode == 0)
			{
				Debug.Log("Eidren: PlayMode tests passed. " + $"Passed={result.PassCount}, " + $"Skipped={result.SkipCount}.");
			}
			else
			{
				Debug.LogError($"Eidren: PlayMode tests failed: {result.FailCount}. " + string.Join(Environment.NewLine, failures));
			}
			api.UnregisterCallbacks(this);
			EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, (EditorApplication.CallbackFunction)delegate
			{
				UnityEngine.Object.DestroyImmediate(api);
				api = null;
				callback = null;
				EditorApplication.Exit(exitCode);
			});
		}

		public void TestStarted(ITestAdaptor test)
		{
		}

		public void TestFinished(ITestResultAdaptor result)
		{
			if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
			{
				failures.Add(result.FullName + ": " + result.Message);
			}
		}
	}

	private const string SessionKey = "Eidren.PlayModeAutomationRunner.Active";

	private static TestRunnerApi api;

	private static ResultCallback callback;

	[InitializeOnLoadMethod]
	private static void ResumeAfterDomainReload()
	{
		if (SessionState.GetBool("Eidren.PlayModeAutomationRunner.Active", defaultValue: false))
		{
			EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, new EditorApplication.CallbackFunction(RegisterCallback));
		}
	}

	public static void RunAll()
	{
		if (api != null)
		{
			throw new InvalidOperationException("A PlayMode automation run is already active.");
		}
		SessionState.SetBool("Eidren.PlayModeAutomationRunner.Active", value: true);
		RegisterCallback();
		api.Execute(new ExecutionSettings(new Filter
		{
			testMode = TestMode.PlayMode,
			assemblyNames = new string[1] { "Eidren.PlayMode.Tests" }
		}));
		Debug.Log("Eidren: PlayMode test run started.");
	}

	private static void RegisterCallback()
	{
		if (!(api != null))
		{
			callback = new ResultCallback();
			api = ScriptableObject.CreateInstance<TestRunnerApi>();
			api.RegisterCallbacks(callback);
		}
	}
}
}
