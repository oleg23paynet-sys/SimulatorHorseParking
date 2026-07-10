using System.Collections.Generic;
using HorseParking.Application.Interaction;
using HorseParking.Core.Localization;
using HorseParking.Core.Randomness;
using HorseParking.Core.Time;
using HorseParking.Infrastructure.Localization;
using HorseParking.Infrastructure.Randomness;
using HorseParking.Infrastructure.Time;
using UnityEngine;

namespace HorseParking.Presentation.Composition
{
    /// <summary>
    /// Single Composition Root: creates concrete services and injects them into Presentation systems.
    /// Attach to the Bootstrap scene when that scene is created.
    /// </summary>
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private ILocalizationService localizationService = null!;
        private IGameClock gameClock = null!;
        private IRandomSource randomSource = null!;
        private InteractWithTargetUseCase interactWithTargetUseCase = null!;

        public ILocalizationService LocalizationService => localizationService;

        public IGameClock GameClock => gameClock;

        public IRandomSource RandomSource => randomSource;

        public InteractWithTargetUseCase InteractWithTargetUseCase => interactWithTargetUseCase;

        private void Awake()
        {
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            localizationService = new DictionaryLocalizationService("en", new Dictionary<string, string>());
            gameClock = new StopwatchGameClock();
            randomSource = new SeededRandomSource(12345);
            interactWithTargetUseCase = new InteractWithTargetUseCase();
        }
    }
}
