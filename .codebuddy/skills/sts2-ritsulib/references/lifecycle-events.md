# 生命周期事件参考

RitsuLib 所有生命周期事件的完整参考。

## 订阅模式

```csharp
// 按类型订阅（推荐）
var sub = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt => { ... });
sub.Dispose();

// 通过 ILifecycleObserver
public class MyObserver : ILifecycleObserver
{
    public void OnEvent(IFrameworkLifecycleEvent evt) { ... }
}
RitsuLibFramework.SubscribeLifecycle(new MyObserver());
```

可重放事件（`IReplayableFrameworkLifecycleEvent`）：延迟订阅会立即触发回调。

## 框架事件

| 事件 | 可重放 | 载荷 |
|------|--------|------|
| FrameworkInitializingEvent | 否 | FrameworkModId、FrameworkVersion |
| FrameworkInitializedEvent | 是 | FrameworkModId、IsActive |
| ProfileServicesInitializingEvent | 否 | - |
| ProfileServicesInitializedEvent | 是 | ProfileId |

## 游戏引导事件

| 事件 | 可重放 | 载荷 |
|------|--------|------|
| EssentialInitializationStartingEvent | 否 | - |
| EssentialInitializationCompletedEvent | 是 | - |
| DeferredInitializationStartingEvent | 否 | - |
| DeferredInitializationCompletedEvent | 是 | - |
| ContentRegistrationClosedEvent | 是 | Reason |
| ModelRegistryInitializingEvent | 否 | - |
| ModelRegistryInitializedEvent | 是 | RegisteredModelTypeCount |
| ModelIdsInitializingEvent | 否 | - |
| ModelIdsInitializedEvent | 是 | - |
| ModelPreloadingStartingEvent | 否 | - |
| ModelPreloadingCompletedEvent | 是 | - |
| GameTreeEnteredEvent | 是 | Game |
| GameReadyEvent | 是 | Game |

## 战局事件

| 事件 | 可重放 | 载荷 |
|------|--------|------|
| RunStartedEvent | 否 | RunState、IsMultiplayer、IsDaily |
| RunLoadedEvent | 否 | RunState、IsMultiplayer、IsDaily |
| RunEndedEvent | 否 | Run、IsVictory、IsAbandoned |

## 房间与幕事件

| 事件 | 载荷 |
|------|------|
| RoomEnteringEvent | RunState、Room |
| RoomEnteredEvent | RunState、Room |
| RoomExitedEvent | RunManager、Room |
| ActEnteringEvent | RunManager、TargetActIndex、DoTransition |
| ActEnteredEvent | RunState、CurrentActIndex |
| RewardsScreenContinuingEvent | RunManager |

## 战斗事件

| 事件 | 载荷 |
|------|------|
| CombatStartingEvent | RunState、CombatState? |
| CombatEndedEvent | RunState、CombatState?、Room |
| CombatVictoryEvent | RunState、CombatState?、Room |
| SideTurnStartingEvent | CombatState、Side |
| SideTurnStartedEvent | CombatState、Side |
| CardPlayingEvent | CombatState、CardPlay |
| CardPlayedEvent | CombatState、CardPlay |
| CardDrawnEvent | CombatState、Card、FromHand |
| CardMovedBetweenPilesEvent | RunState、CombatState?、Card、PreviousPile、Source |
| BeforeFlushEvent | CombatState、Player |
| CardsFlushedEvent | CombatState、Player、FlushedCards、RetainedCards (API 0.105.0+) |
| CardDiscardedEvent | CombatState、Card |
| CardExhaustedEvent | CombatState、Card、CausedByEthereal |
| CardRetainedEvent | CombatState、Card（已过时；从 CardsFlushedEvent 重放） |

## 生物事件

| 事件 | 载荷 |
|------|------|
| CreatureDyingEvent | CombatState、Creature |
| CreatureDiedEvent | CombatState、Creature |

## 奖励事件

| 事件 | 载荷 |
|------|------|
| GoldGainedEvent | Amount |
| GoldLostEvent | Amount |
| PotionProcuredEvent | Potion |
| PotionDiscardedEvent | Potion |
| RelicObtainedEvent | Relic |
| RelicRemovedEvent | Relic |
| RewardTakenEvent | Reward |

## 解锁事件

| 事件 | 载荷 |
|------|------|
| EpochObtainedEvent | Epoch |
| EpochRevealedEvent | Epoch |
| UnlockIncrementedEvent | UnlockState |

## 档案生命周期事件

| 事件 | 载荷 |
|------|------|
| ProfileIdInitializedEvent | ProfileId |
| ProfileSwitchingEvent | OldProfileId、NewProfileId |
| ProfileSwitchedEvent | ProfileId |
| ProfileDeletingEvent | ProfileId |
| ProfileDeletedEvent | ProfileId |

## 存档事件

| 事件 | 载荷 |
|------|------|
| RunSavingEvent | RunState |
| RunSavedEvent | RunState |
| ProgressSavingEvent | - |
| ProgressSavedEvent | - |

## ModDataStore 数据事件

| 事件 | 说明 |
|------|------|
| ProfileDataReadyEvent | 存档数据已加载，可安全读写 |
| ProfileDataChangedEvent | 存档数据已更改 |
| ProfileDataInvalidatedEvent | 存档数据已失效（如档案切换） |

## 其他事件

| 事件 | 载荷 |
|------|------|
| GameOverScreenCreatedEvent | Screen |
