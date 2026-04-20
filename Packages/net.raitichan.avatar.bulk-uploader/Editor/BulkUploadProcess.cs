using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using net.raitichan.avatar.bulk_uploader.Editor.ReflectionHelper.VRC.SDKBase;
using net.raitichan.avatar.bulk_uploader.Editor.Window;
using net.raitichan.avatar.bulk_uploader.Runtime.ScriptableObject;
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
        private static bool _errorFlag = false;

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
            int avatarCount = sceneDefines.SelectMany(define => define.Avatars).Count(avatar => avatar.Enable);
            bool result = EditorUtility.DisplayDialog("Bulk Uploader", $"{sceneCount}シーン、計{avatarCount}アバターの一括アップロードを行います。", "Upload", "Cancel");
            if (!result) return;

            if (VRCSdkControlPanel.window == null) {
                Notification.PlayHand();
                EditorUtility.DisplayDialog("Bulk Uploader", "VRCSDKコントロールパネルを開いてください。", "OK");
                return;
            }

            if (!APIUser.IsLoggedIn) {
                Notification.PlayHand();
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
                    if (!avatarDefine.Enable) continue;
                    _window.RegisterAvatar(sceneDefine.Scene.name, avatarDefine.BlueprintID ?? "???", avatarDefine.ObjectName ?? "???");
                }
            }

            _allCancellationTokenSource = new CancellationTokenSource();
            _errorFlag = false;
            try {
                await Upload(sceneDefines, _allCancellationTokenSource.Token);
            } finally {
                if (_errorFlag) {
                    Notification.PlayHand();
                    ToastNotification.ShowToast("Bulk Uploader", "アバターのアップロードが完了しました。", true);
                } else {
                    ToastNotification.ShowToast("Bulk Uploader", "アバターのアップロードが完了しました。");
                }

                EditorUtility.DisplayDialog("Bulk Uploader", "アバターのアップロードが完了しました。", "OK");
                _allCancellationTokenSource.Dispose();
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
                        _errorFlag = true;
                        continue;
                    }


                    _sceneCancellationTokenSource = new CancellationTokenSource();
                    CancellationTokenSource linkedTokenSource =
                        CancellationTokenSource.CreateLinkedTokenSource(allCancellationToken, _sceneCancellationTokenSource.Token);
                    try {
                        await Upload(sceneDefine.Avatars.Where(avatarDef => avatarDef.Enable).ToArray(), scene, linkedTokenSource.Token);
                    } catch (OperationCanceledException e) {
                        Debug.Log(e);
                        if (allCancellationToken.IsCancellationRequested) {
                            throw;
                        }
                    } catch (Exception e) {
                        _errorFlag = true;
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
                _window.SetSceneProgress(scene.name, $"Upload : {avatarDefine.ObjectName ?? avatarDefine.BlueprintID}",
                    (float)currentCount / avatarCount * 100.0f);
                _avatarCancellationTokenSource = new CancellationTokenSource();
                CancellationTokenSource linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(sceneCancellationToken, _avatarCancellationTokenSource.Token);
                try {
                    await Upload(avatarDefine, scene, linkedTokenSource.Token);
                } catch (OperationCanceledException e) {
                    Debug.Log(e);
                    if (sceneCancellationToken.IsCancellationRequested) {
                        throw;
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                    _errorFlag = true;
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
                _errorFlag = true;
                return;
            }

            VRC_AvatarDescriptor? avatar = pipelineManager.gameObject.GetComponent<VRC_AvatarDescriptor>();
            if (avatar == null) {
                _errorFlag = true;
                return;
            }


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

			List<GameObject>? inactiveGameObjects = null;

			try {
				if (!avatar.gameObject.activeInHierarchy) {
					inactiveGameObjects = new List<GameObject>();
					for (var transform = avatar.transform; transform != null; transform = transform.parent) {
						var go = transform.gameObject;
						if (!go.activeSelf) {
							go.SetActive(true);
							inactiveGameObjects.Add(go);
						}
					}
				}

				_processingSceneName = scene.name;
				_processingBlueprintId = avatarDefine.BlueprintID ?? "???";
				VRCCopyrightAgreementHelper.SaveContentAgreementToSession(_processingBlueprintId);
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

				if (inactiveGameObjects != null)
					foreach (var go in inactiveGameObjects)
						go.SetActive(false);
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
            _errorFlag = true;
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
            try {
                Debug.Log($"OnUploadProgress : {tuple.status} : {tuple.percentage}");
                await UniTask.SwitchToMainThread();
                _window.SetAvatarProgress(_processingSceneName, _processingBlueprintId, $"Uploading : {tuple.status}", tuple.percentage * 100.0f);
            } catch (Exception e) {
                _errorFlag = true;
                Debug.LogException(e);
            }
        }

        private static void OnUploadError(object sender, string s) {
            Debug.Log($"OnUploadError : {s}");
            _errorFlag = true;
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