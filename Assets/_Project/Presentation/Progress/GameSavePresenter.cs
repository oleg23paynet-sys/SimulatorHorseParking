#nullable enable

using System.Collections;
using HorseParking.Application.Progress;
using HorseParking.Core.Localization;
using HorseParking.Presentation.Composition;
using UnityEngine;
using UnityEngine.UI;

namespace HorseParking.Presentation.Progress
{
    /// <summary>Thin Unity adapter for the single save slot owned by the application layer.</summary>
    [DisallowMultipleComponent]
    public sealed class GameSavePresenter : MonoBehaviour
    {
        private static readonly LocalizationKey ShortcutsKey = new("ui.save.shortcuts");
        private static readonly LocalizationKey SavedKey = new("ui.save.saved");
        private static readonly LocalizationKey LoadedKey = new("ui.save.loaded");
        private static readonly LocalizationKey NewGameKey = new("ui.save.new_game");
        private static readonly LocalizationKey NoSaveKey = new("ui.save.no_save");
        private static readonly LocalizationKey InvalidKey = new("ui.save.invalid");
        private static readonly LocalizationKey StorageErrorKey = new("ui.save.storage_error");

        [SerializeField] private GameCompositionRoot compositionRoot = null!;
        [SerializeField] private Text shortcutsText = null!;
        [SerializeField] private Text statusText = null!;
        [Min(1f)] [SerializeField] private float statusDurationSeconds = 4f;

        private float hideStatusAt;
        private bool isReady;

        public void Configure(GameCompositionRoot root, Text shortcuts, Text status)
        {
            compositionRoot = root;
            shortcutsText = shortcuts;
            statusText = status;
        }

        private IEnumerator Start()
        {
            if (compositionRoot == null
                || !compositionRoot.HasGameProgress
                || shortcutsText == null
                || statusText == null)
            {
                Debug.LogError("Save/load presenter is not configured.", this);
                enabled = false;
                yield break;
            }

            shortcutsText.text = Translate(ShortcutsKey);
            isReady = true;

            // Wait one frame so every scene presenter has subscribed before restored
            // application events refresh its visuals.
            yield return null;
            if (compositionRoot.GameProgressUseCase.HasSave)
            {
                ShowResult(compositionRoot.GameProgressUseCase.Load(), LoadedKey);
            }
            else
            {
                ShowStatus(NewGameKey);
            }
        }

        private void Update()
        {
            if (!isReady) return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                ShowResult(compositionRoot.GameProgressUseCase.Save(), SavedKey);
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                ShowResult(compositionRoot.GameProgressUseCase.Load(), LoadedKey);
            }

            if (hideStatusAt > 0f && Time.unscaledTime >= hideStatusAt)
            {
                statusText.text = string.Empty;
                hideStatusAt = 0f;
            }
        }

        private void OnApplicationQuit()
        {
            if (isReady) compositionRoot.GameProgressUseCase.Save();
        }

        private void ShowResult(GameProgressOperationResult result, LocalizationKey successKey)
        {
            if (result.Succeeded)
            {
                ShowStatus(successKey);
                return;
            }

            ShowStatus(result.FailureReason switch
            {
                GameProgressFailureReason.NoSave => NoSaveKey,
                GameProgressFailureReason.InvalidOrUnsupportedSave => InvalidKey,
                _ => StorageErrorKey
            });
        }

        private void ShowStatus(LocalizationKey key)
        {
            statusText.text = Translate(key);
            hideStatusAt = Time.unscaledTime + statusDurationSeconds;
        }

        private string Translate(LocalizationKey key) =>
            compositionRoot.LocalizationService.Translate(key);
    }
}
