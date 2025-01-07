﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using net.raitichan.avatar.bulk_uploader.Editor.Window;
using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
using net.raitichan.avatar.bulk_uploader.Runtime.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3A.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor {
	internal static class BulkUploadProcess {
		private static VRCSdkControlPanelAvatarBuilder _builder { get; } = new();
		private static UploadProgressWindow _window = null!;
		private static string _processingSceneName = "";
		private static string _processingBlueprintId = "";

		private static CancellationTokenSource? _allCancellationTokenSource;
		private static CancellationTokenSource? _sceneCancellationTokenSource;
		private static CancellationTokenSource? _avatarCancellationTokenSource;

		public static void CancelAllProcess() {
			_allCancellationTokenSource?.Cancel();
		}

		public static void CancelCurrentScene() {
			_sceneCancellationTokenSource?.Cancel();
		}

		public static void CancelCurrentAvatar() {
			_avatarCancellationTokenSource?.Cancel();
		}

		public static async Task StartUpload(TargetScenesDefine scenesDefine) {
			SceneDefine[] sceneDefines = scenesDefine.Scenes.Where(define => define.Enable && define.Scene != null).ToArray();
			int sceneCount = sceneDefines.Length;
			int avatarCount = sceneDefines.SelectMany(define => define.Avatars).Count();
			bool result = EditorUtility.DisplayDialog("Bulk Uploader", $"{sceneCount}シーン、計{avatarCount}アバターの一括アップロードを行います。", "Upload", "Cancel");
			if (!result) return;

			if (VRCSdkControlPanel.window == null) {
				EditorUtility.DisplayDialog("Bulk Uploader", "VRCSDKコントロールパネルを開いてください。", "OK");
				return;
			}

			if (!APIUser.IsLoggedIn) {
				VRCSdkControlPanel.window.Focus();
				EditorUtility.DisplayDialog("Bulk Uploader", "VRCSDKコントロールパネルからログインしてください。", "OK");
				return;
			}

			_builder.RegisterBuilder(VRCSdkControlPanel.window);
			_window = UploadProgressWindow.ShowWindow();

			foreach (SceneDefine sceneDefine in sceneDefines) {
				if (sceneDefine.Scene == null) continue;
				_window.RegisterScene(sceneDefine.Scene.name);
				foreach (AvatarDefine avatarDefine in sceneDefine.Avatars.Where(avatarDefine => !string.IsNullOrEmpty(avatarDefine.BlueprintID))) {
					_window.RegisterAvatar(sceneDefine.Scene.name, avatarDefine.BlueprintID ?? "???", avatarDefine.ObjectName ?? "???");
				}
			}

			_allCancellationTokenSource = new CancellationTokenSource();
			try {
				await Upload(sceneDefines, _allCancellationTokenSource.Token);
			} finally {
				EditorUtility.DisplayDialog("Bulk Uploader", "アバターのアップロードが完了しました。", "OK");
				_allCancellationTokenSource.Dispose();
			}
		}

		public static async Task StartUpload(TargetAvatarsDefine avatarsDefine) {
			AvatarDefine[] avatarDefines = avatarsDefine.Avatars.Where(define => define.Enable && define.Avatar != null).ToArray();
			int avatarCount = avatarDefines.Length;

			bool result = EditorUtility.DisplayDialog("Bulk Uploader", $"計{avatarCount}アバターの一括アップロードを行います。", "Upload", "Cancel");
			if (!result) return;
			if (VRCSdkControlPanel.window == null) {
				EditorUtility.DisplayDialog("Bulk Uploader", "VRCSDKコントロールパネルを開いて、ログインしてください。", "OK");
				return;
			}

			if (!APIUser.IsLoggedIn) {
				VRCSdkControlPanel.window.Focus();
				EditorUtility.DisplayDialog("Bulk Uploader", "VRCSDKコントロールパネルからログインしてください。", "OK");
				return;
			}

			_builder.RegisterBuilder(VRCSdkControlPanel.window);
			_window = UploadProgressWindow.ShowWindow();

			_window.RegisterScene(avatarsDefine.gameObject.scene.name);

			foreach (AvatarDefine avatarDefine in avatarDefines) {
				if (avatarDefine.Avatar == null) continue;
				_window.RegisterAvatar(avatarsDefine.gameObject.scene.name, avatarDefine.BlueprintID ?? "???", avatarDefine.ObjectName ?? "???");
			}

			_allCancellationTokenSource = new CancellationTokenSource();
			_sceneCancellationTokenSource = new CancellationTokenSource();
			CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_allCancellationTokenSource.Token, _sceneCancellationTokenSource.Token);
			try {
				await Upload(avatarDefines, avatarsDefine.gameObject.scene, linkedTokenSource.Token);
			} finally {
				EditorUtility.DisplayDialog("Bulk Uploader", "アバターのアップロードが完了しました。", "OK");
				_allCancellationTokenSource.Dispose();
				_sceneCancellationTokenSource.Dispose();
			}
		}


		private static async Task Upload(SceneDefine[] sceneDefines, CancellationToken allCancellationToken) {
			Debug.Log($"Start upload scene sequence. Total {sceneDefines.Length} scenes.");

			foreach (SceneDefine sceneDefine in sceneDefines) {
				if (sceneDefine.Scene == null) continue;
				bool isAdditionalLoadedScene = false;

				string scenePath = AssetDatabase.GetAssetPath(sceneDefine.Scene);
				Debug.Log($"Iteration : {scenePath}");
				Scene scene = SceneManager.GetSceneByPath(scenePath);
				try {
					if (!scene.isLoaded) {
						Debug.Log($"Open Scene : {scenePath}");
						scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
						isAdditionalLoadedScene = true;
					}

					if (!scene.isLoaded) {
						Debug.LogError($"Can not open Scene : {scenePath}");
						continue;
					}


					_sceneCancellationTokenSource = new CancellationTokenSource();
					CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(allCancellationToken, _sceneCancellationTokenSource.Token);
					try {
						await Upload(sceneDefine.Avatars.ToArray(), scene, linkedTokenSource.Token);
					} catch (OperationCanceledException e) {
						Debug.Log(e);
						if (allCancellationToken.IsCancellationRequested) {
							throw;
						}
					} catch (Exception e) {
						Debug.Log(e);
						if (allCancellationToken.IsCancellationRequested) {
							throw new TaskCanceledException();
						}
					} finally {
						{
							linkedTokenSource.Dispose();
							_sceneCancellationTokenSource.Dispose();
						}
					}
				} finally {
					if (isAdditionalLoadedScene) {
						Debug.Log($"Close Scene : {scenePath}");
						EditorSceneManager.CloseScene(scene, true);
					}
				}
			}
		}

		private static async Task Upload(AvatarDefine[] avatarsDefines, Scene scene, CancellationToken sceneCancellationToken) {
			Debug.Log("Start Upload Avatar Sequence.");
			int avatarCount = avatarsDefines.Length;
			int currentCount = -1;
			foreach (AvatarDefine avatarDefine in avatarsDefines) {
				currentCount++;
				if (avatarDefine.BlueprintID == null) continue;
				_window.SetSceneProgress(scene.name, $"Upload : {avatarDefine.ObjectName ?? avatarDefine.BlueprintID}", (float)currentCount / avatarCount * 100.0f);
				_avatarCancellationTokenSource = new CancellationTokenSource();
				CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(sceneCancellationToken, _avatarCancellationTokenSource.Token);
				try {
					await Upload(avatarDefine, scene, linkedTokenSource.Token);
				} catch (OperationCanceledException e) {
					Debug.Log(e);
					if (sceneCancellationToken.IsCancellationRequested) {
						throw;
					}
				} catch (Exception e) {
					Debug.LogException(e);
					if (sceneCancellationToken.IsCancellationRequested) {
						throw new TaskCanceledException();
					}
				} finally {
					linkedTokenSource.Dispose();
					_avatarCancellationTokenSource.Dispose();
				}
			}

			_window.SetSceneProgress(scene.name, "Complete", 100.0f);
		}

		private static async Task Upload(AvatarDefine avatarDefine, Scene scene, CancellationToken avatarCancellationToken) {
			VRCSdkControlPanelAvatarBuilder builder = _builder;

			PipelineManager? pipelineManager = scene.GetRootGameObjects()
				.SelectMany(o => o.GetComponentsInChildren<PipelineManager>())
				.FirstOrDefault(manager => manager.blueprintId == avatarDefine.BlueprintID);

			if (pipelineManager == null) {
				Debug.LogError($"Avatar is not found : {avatarDefine.ObjectName} : {avatarDefine.BlueprintID}");
				return;
			}

			VRC_AvatarDescriptor? avatar = pipelineManager.gameObject.GetComponent<VRC_AvatarDescriptor>();
			if (avatar == null) return;


			builder.OnSdkBuildStart += OnBuildStart;
			builder.OnSdkBuildProgress += OnBuildProgress;
			builder.OnSdkBuildError += OnBuildError;
			builder.OnSdkBuildSuccess += OnBuildSuccess;
			builder.OnSdkBuildFinish += OnBuildFinish;
			builder.OnSdkBuildStateChange += OnBuildStateChange;

			builder.OnSdkUploadStart += OnUploadStart;
			builder.OnSdkUploadProgress += OnUploadProgress;
			builder.OnSdkUploadError += OnUploadError;
			builder.OnSdkUploadSuccess += OnUploadSuccess;
			builder.OnSdkUploadFinish += OnUploadFinish;
			builder.OnSdkUploadStateChange += OnUploadStateChange;

			try {
				_processingSceneName = scene.name;
				_processingBlueprintId = avatarDefine.BlueprintID ?? "???";
				VRCAvatar avatarData = await VRCApi.GetAvatar(pipelineManager.blueprintId, true, avatarCancellationToken);
				await builder.BuildAndUpload(avatar.gameObject, avatarData, null, avatarCancellationToken);
			} finally {
				builder.OnSdkBuildStart -= OnBuildStart;
				builder.OnSdkBuildProgress -= OnBuildProgress;
				builder.OnSdkBuildError -= OnBuildError;
				builder.OnSdkBuildSuccess -= OnBuildSuccess;
				builder.OnSdkBuildFinish -= OnBuildFinish;
				builder.OnSdkBuildStateChange -= OnBuildStateChange;

				builder.OnSdkUploadStart -= OnUploadStart;
				builder.OnSdkUploadProgress -= OnUploadProgress;
				builder.OnSdkUploadError -= OnUploadError;
				builder.OnSdkUploadSuccess -= OnUploadSuccess;
				builder.OnSdkUploadFinish -= OnUploadFinish;
				builder.OnSdkUploadStateChange -= OnUploadStateChange;
			}
		}

		private static void OnBuildStart(object sender, object o) {
			Debug.Log($"OnBuildStart : {o.GetType()}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, "OnBuildStart", 0.0f);
		}

		private static void OnBuildProgress(object sender, string s) {
			Debug.Log($"OnBuildProgress : {s}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Building : {s}", 0.0f);
		}

		private static void OnBuildError(object sender, string s) {
			Debug.Log($"OnBuildError : {s}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Build Error : {s}", 100.0f);
			_window.SetAvatarError(_processingSceneName, _processingBlueprintId);
		}

		private static void OnBuildSuccess(object sender, string s) {
			Debug.Log($"OnBuildSuccess : {s}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Build Success : {s}", 0.0f);
		}

		private static void OnBuildFinish(object sender, string s) {
			Debug.Log($"OnBuildFinish : {s}");
		}

		private static void OnBuildStateChange(object sender, SdkBuildState state) {
			Debug.Log($"OnBuildStateChange : {state}");
		}

		private static void OnUploadStart(object sender, EventArgs args) {
			Debug.Log($"OnUploadStart : {args.GetType()}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Upload Start", 0.0f);
		}

		private static async void OnUploadProgress(object sender, (string status, float percentage) tuple) {
			Debug.Log($"OnUploadProgress : {tuple.status} : {tuple.percentage}");
			await UniTask.SwitchToMainThread();
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Uploading : {tuple.status}", tuple.percentage * 100.0f);
		}

		private static void OnUploadError(object sender, string s) {
			Debug.Log($"OnUploadError : {s}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Upload Error : {s}", -1f);
			_window.SetAvatarError(_processingSceneName, _processingBlueprintId);
		}

		private static void OnUploadSuccess(object sender, string s) {
			Debug.Log($"OnUploadSuccess : {s}");
			_window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Upload Success : {s}", 100.0f);
		}

		private static void OnUploadFinish(object sender, string s) {
			Debug.Log($"OnUploadFinish : {s}");
		}

		private static void OnUploadStateChange(object sender, SdkUploadState state) {
			Debug.Log($"OnUploadStateChange : {state}");
		}
	}
}