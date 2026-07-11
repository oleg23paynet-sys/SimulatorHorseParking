# Карта файлов разработчика

Этот файл - живая карта Unity-проекта. После каждой завершённой задачи он обновляется вместе с TXT-журналом в `Assets/_Project/Docs/TaskLogs/`.

## Статус

- Текущий этап roadmap: Этап 1 - основа Unity-проекта и взаимодействия.
- Последняя выполненная задача: 1.4 - Bootstrap-сцена, первый игрок и техническое взаимодействие.
- Unity Editor: `6000.5.0f1`.

## Корневые настройки

| Путь | Назначение | Где менять |
|---|---|---|
| `Packages/manifest.json` | Список Unity-пакетов: URP, UI и тесты. | Добавлять пакет только при согласованной технической необходимости. |
| `ProjectSettings/ProjectVersion.txt` | Зафиксированная версия Unity Editor. | Менять только при осознанном обновлении Unity. |

## Clean Architecture: код

| Путь | Слой | Назначение | Где менять |
|---|---|---|---|
| `Assets/_Project/Core/` | Core | Чистые правила игры, доменные модели и интерфейсы. Не содержит UnityEngine. | Парковка, экономика, стройка, локализация через контракты. |
| `Assets/_Project/Application/` | Application | Use cases и координация Core. Зависит только от Core. | Сценарии: взаимодействовать, купить, построить, принять оплату. |
| `Assets/_Project/Infrastructure/` | Infrastructure | Реализации сохранений, времени, локализации и внешних сервисов. | Подключение конкретных источников данных. |
| `Assets/_Project/Presentation/` | Presentation | Unity-слой: MonoBehaviour, UI, камера, ввод, префабы. | Всё, что напрямую использует UnityEngine. |
| `Assets/_Project/Tests/` | Tests | Автоматические тесты Core. | Добавлять тесты для правил игры без запуска сцены. |

## Core-контракты: первая версия

| Путь | Назначение | Где менять |
|---|---|---|
| `Assets/_Project/Core/Localization/LocalizationKey.cs` | Стабильный ключ любого текста, который видит игрок. | Добавлять только ключи и правила их валидации; не хранить здесь переводы. |
| `Assets/_Project/Core/Localization/ILocalizationService.cs` | Контракт получения перевода для Core. | Реализация Unity или таблиц должна быть в Infrastructure. |
| `Assets/_Project/Core/Time/IGameClock.cs` | Контракт игрового времени. | Use cases получают его через конструктор. |
| `Assets/_Project/Core/Randomness/IRandomSource.cs` | Контракт контролируемой случайности. | Передавать в Core извне для воспроизводимых тестов. |
| `Assets/_Project/Core/Interaction/IInteractionTarget.cs` | Контракт объекта, с которым может взаимодействовать игрок. | Будущие табличка стройки, шлагбаум и мешочек оплаты реализуют этот контракт. |
| `Assets/_Project/Core/Interaction/InteractionPrompt.cs` | Локализуемые ключи подсказки действия и цели. | Presentation переводит ключи через ILocalizationService. |
| `Assets/_Project/Core/Interaction/InteractionResult.cs` | Успех/ошибка взаимодействия с ключом сообщения. | Не добавлять отображаемый текст. |
| `Assets/_Project/Core/Interaction/InteractionAvailability.cs` | Состояние доступности взаимодействия. | Расширять при появлении согласованных состояний. |
| `Assets/_Project/Application/Interaction/InteractWithTargetUseCase.cs` | Единственная точка запуска взаимодействия из Presentation. | Unity-компоненты вызывают use case, не доменный объект напрямую. |

## Реализации контрактов и DI

| Путь | Назначение | Где менять |
|---|---|---|
| `Assets/_Project/Infrastructure/Localization/DictionaryLocalizationService.cs` | Получает перевод из словаря; отсутствующий перевод безопасно возвращает ключ. | Будущая загрузка таблиц локализации заменяет источник данных, не контракт Core. |
| `Assets/_Project/Infrastructure/Randomness/SeededRandomSource.cs` | Воспроизводимая случайность по seed. | Для конкретных сценариев задавать seed в Composition Root или тестах. |
| `Assets/_Project/Infrastructure/Time/StopwatchGameClock.cs` | Внешний источник прошедшего времени. | Можно заменить Unity-часами через DI без правки Core. |
| `Assets/_Project/Presentation/Composition/GameCompositionRoot.cs` | Единственное место создания конкретных сервисов и use cases. | Здесь связывать реализации и передавать их Presentation-системам; не делать singleton/Service Locator. |
| `Assets/_Project/Tests/Core/LocalizationKeyTests.cs` | Редакторские тесты базового контракта ключей локализации. | Добавлять тесты Core-правил без создания Unity-сцены. |

## Bootstrap: запуск и техническая проверка

| Путь | Назначение | Где менять |
|---|---|---|
| `Assets/_Project/Presentation/Player/FirstPersonPlayerController.cs` | Первое лицо без рук: движение, обзор мышью, гравитация и raycast взаимодействия. | Настройки скорости/чувствительности находятся в начале файла; не переносить игровую логику в этот Unity-компонент. |
| `Assets/_Project/Docs/TaskLogs/2026-07-11_bugfix-first-person-camera-reference.txt` | Отчёт об исправлении ошибки запуска `NullReferenceException`. | Причина и проверка исправления. |
| `Assets/_Project/Presentation/Interaction/DebugInteractionTarget.cs` | Временный технический объект, реализующий IInteractionTarget. | Удалить из Bootstrap после появления реальных адаптеров таблички, шлагбаума и т. п. |
| `Assets/_Project/Presentation/Editor/BootstrapSceneBuilder.cs` | Команда Unity `Horse Parking -> Build Bootstrap Scene`; создаёт техническую сцену. | Здесь только сборка сцены для проверки, не финальный игровой контент. |
| `Assets/_Project/Presentation/Editor/HorseParking.Presentation.Editor.asmdef` | Editor-сборка для builder-скриптов. | Добавлять сюда только UnityEditor-код. |
| `Assets/_Project/Scenes/Bootstrap.unity` | Запускаемая техническая сцена Этапа 1. | Открывать для проверки; будущая игровая сцена будет отдельной. |
| `Docs/Developer/HowToRun.md` | Пошаговая инструкция открытия и запуска проекта. | Обновлять при смене стартовой сцены или управления. |

## Content и данные

| Путь | Что лежит здесь |
|---|---|
| `Assets/_Project/Content/Models/` | Только готовые 3D-модели FBX. |
| `Assets/_Project/Content/Animations/` | Готовые FBX-анимации: Mixamo или согласованный внешний источник. |
| `Assets/_Project/Content/Materials/` | Готовые материалы. |
| `Assets/_Project/Content/Textures/` | Готовые текстуры. |
| `Assets/_Project/Content/Audio/` | Готовые звуки и музыка. |
| `Assets/_Project/Content/VFX/` | Готовые VFX assets. |
| `Assets/_Project/Content/UI/` | Готовые UI-assets и иконки. |
| `Assets/_Project/Scenes/` | Unity-сцены. |
| `Assets/_Project/Settings/` | ScriptableObject-конфигурации и балансные числа. |

## Ожидаемые assets для замены технической Bootstrap-сцены

| Цель | Что требуется | Точный путь после скачивания | Что заменит |
|---|---|---|---|
| Поверхность парковки | Готовая статичная FBX-модель средневековой ground layout/площадки с PBR-текстурами. | `Assets/_Project/Content/Models/Environment/ParkingGround/SM_ParkingGround.fbx` | `TechnicalGround_ReplaceWithReadyAsset` в Bootstrap.unity. |
| Текстуры поверхности | PNG/JPG PBR-карты той же модели. | `Assets/_Project/Content/Textures/Environment/ParkingGround/` | Материалы ground-модели. |
| Табличка строительства | Готовая статичная FBX-модель знака; pivot должен быть у нижнего основания. | `Assets/_Project/Content/Models/Props/ConstructionSign/SM_ConstructionSign.fbx` | `TechnicalInteractionTarget_ReplaceWithReadyAsset` в Bootstrap.unity. |
| Текстуры таблички | PNG/JPG карты таблички. | `Assets/_Project/Content/Textures/Props/ConstructionSign/` | Материалы модели таблички. |
| Лицензии и атрибуции | Оригинальные README/License или текст ссылки на источник. | `Docs/Attributions/` | Обязательная информация по использованию assets. |

До добавления этих файлов технические Unity-примитивы остаются только временной проверкой и не считаются игровым контентом.

## Техническое решение ввода (временное)

| Область | Решение | Причина |
|---|---|---|
| Базовое движение и взаимодействие Этапа 1 | Встроенный Unity Input Manager (`ProjectSettings/InputManager.asset`) | Пакет Input System 1.12.0 несовместим с Unity 6.5.0f1 и вызывает ошибки внутри своего Editor-кода. Позже смена ввода будет выполняться только через адаптер Presentation. |
| Навигация NPC | Не добавлена на Этапе 1 | Пакет AI Navigation 2.0.0 несовместим с Unity 6.5.0f1. Вернёмся к выбору совместимого решения на Этапе 2, когда понадобятся маршруты клиентов. |

## Документация и журналы

| Путь | Назначение |
|---|---|
| `Assets/_Project/Docs/Developer/FileMap.md` | Эта карта файлов. |
| `Assets/_Project/Docs/TaskLogs/` | TXT-отчёт после каждой задачи реализации. |
| `Docs/Roadmap_SimulatorHorseParking.docx` | Главный roadmap и обязательные правила проекта. |

## Parking MVP scene

| Путь | Назначение |
|---|---|
| `Assets/_Project/Presentation/Editor/ParkingMvpSceneBuilder.cs` | Собирает первую сцену ТЗ из готовых FBX: площадка, клиентская лошадь, наездник, ворота, мешочек, магазин и склад. |
| `Assets/_Project/Scenes/ParkingMvp.unity` | Первая видимая Parking MVP-сцена. В ней будет развиваться парковочный цикл клиента. |
| `Assets/_Project/Content/Models/Environment/ParkingGround/CobblestoneLowpoly/cobblestone.fbx` | Готовая площадка парковки. |
| `Assets/_Project/Content/Models/Characters/Horse/LowPolyHorse/uploads_files_2798555_Horse.fbx` | Лошадь клиента. |
| `Assets/_Project/Content/Models/Characters/Rider/X Bot.fbx` | Наездник клиента; временный готовый Mixamo character asset. |
| `Assets/_Project/Content/Materials/Characters/Horse/M_HorseChestnut.mat` | Каштановый материал готовой FBX-лошади. | Менять только цвет/свойства материала, не модель. |
| `Assets/_Project/Content/Animations/Characters/Rider/XBot_SeatedIdle.fbx` | Готовая Mixamo-анимация сидящего наездника. | Не редактировать вручную; при замене скачивать `FBX for Unity`, `Without Skin`. |
| `Assets/_Project/Presentation/Animation/Controllers/AC_XBotSeatedIdle.controller` | Animator Controller: единственное состояние готовой анимации `Seated Idle`. | Добавлять новые состояния только из готовых импортированных клипов. |
| `Assets/_Project/Docs/TaskLogs/2026-07-11_stage-2-01-rider-seated-idle.txt` | Отчёт о подключении наездника. | Текущий итог подзадачи 2.1. |
| `Assets/_Project/Content/Models/Characters/Horse/CartoonRiggedHorse/` | Бесплатная скачанная FBX-пони. | Сохранена, но не использовать для Parking MVP-клиента. |
| `Assets/_Project/Content/Models/AdultTexturedHorse/` | Взрослая лошадь, скачанная пользователем, но только в OBJ/BLEND/STL. | Не подключать: в комплекте нет FBX. |
| `Assets/_Project/Content/Models/Characters/Rider/MedievalCharacter/` | Текстурированный средневековый наездник FBX и texture. | Кандидат на замену X Bot. |
| `Assets/_Project/Content/Models/Characters/MountedClients/RedHorseRider/SM_RedHorseRider.fbx` | Готовая цельная FBX-пара: взрослая лошадь и сидящий наездник. | Используется как `ClientMountedHorseRider_01` в ParkingMvp. |
| `Assets/_Project/Content/Materials/Characters/MountedClients/M_RedHorseRiderHorse.mat` | Материал лошади с готовой текстурой из архива Red Horse Rider. | Менять только материал/texture assignment. |
| `Tools/ConvertRedHorseRiderToFbx.py` | Автоматический Blender-export исходного `.blend` в FBX. | Не моделирует и не меняет позу; только конвертирует формат. |
| `Assets/_Project/Content/Animations/Characters/Rider/MedievalRider/` | Готовые FBX `Walk` и `T-Pose` для средневекового наездника. | Привязывать через Humanoid Avatar, не создавать вручную. |

### Current correction status (2026-07-11)

| Path | What to edit / current fact |
|---|---|
| `Assets/_Project/Presentation/Editor/ParkingMvpSceneBuilder.cs` | The only source of the ParkingMvp visual layout: coordinates, FBX selection and scale. `CreateParkingZone` owns the one parking slot; `CreateClient` owns only the horse. |
| `Assets/_Project/Scenes/ParkingMvp.unity` | Generated scene. Do not hand-edit it for layout; change the builder then run `Horse Parking -> Build Parking MVP Scene`. |
| `Assets/_Project/Content/Models/Characters/Rider/X Bot.fbx` | Kept in content but intentionally hidden until a ready, suitable Mixamo riding animation is supplied. |
| `Assets/_Project/Docs/TaskLogs/2026-07-11_stage-2-01-visual-correction.txt` | What was corrected and what has not been implemented. |

## Bootstrap: границы технической сцены

| Путь | Назначение |
|---|---|
| `Assets/_Project/Presentation/Editor/BootstrapSceneBuilder.cs` | Создаёт только Composition Root, first-person камеру и контроллер. Не создаёт землю, леса, таблички, парковку или другие видимые объекты. |
| `Assets/_Project/Presentation/Player/FirstPersonPlayerController.cs` | Подготавливает movement/look/raycast механику; реальные interaction targets будут добавляться в соответствующих этапах roadmap. |
| `Assets/_Project/Content/Models/Environment/KayKitMedievalHexagon/` | Скачанный пользователем CC0-набор. Сохранён как доступный контент, но не подключён к Bootstrap. |

Визуальная сборка KayKit из задач 1.5 признана не соответствующей ТЗ и откатена. Unity Plane/Cube также не используются.
