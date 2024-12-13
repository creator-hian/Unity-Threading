# 테스트 목록

## UnityMainThreadDispatcherTests.cs

### Enqueue_ShouldExecuteActionOnMainThread

- `Enqueue` 메서드를 통해 메인 스레드에서 액션이 실행되는지 확인합니다.

### Instance_ShouldReturnSameInstance

- `Instance` 프로퍼티가 항상 동일한 인스턴스를 반환하는지 확인합니다.

### Enqueue_WhenNull_ShouldThrowArgumentNullException

- `Enqueue` 메서드에 null 액션을 전달했을 때 `ArgumentNullException`이 발생하는지 확인합니다.

### EnqueueCoroutine_WhenNull_ShouldThrowArgumentNullException

- `Enqueue` 메서드에 null 코루틴을 전달했을 때 `ArgumentNullException`이 발생하는지 확인합니다.

### ClearQueue_ShouldRemoveAllPendingActions

- `ClearQueue` 메서드가 대기 중인 모든 액션을 제거하는지 확인합니다.

### EnqueueAsync_ShouldHandleExceptions

- `EnqueueAsync` 메서드가 예외를 올바르게 처리하는지 확인합니다.

### EnqueueAsync_ShouldExecuteAndReturnResult

- `EnqueueAsync` 메서드가 작업을 실행하고 결과를 반환하는지 확인합니다.

### EnqueueAwaitable_ShouldExecuteAwaitable

- `EnqueueAwaitable` 메서드가 awaitable 작업을 실행하는지 확인합니다. (Unity 2023.1 이상)

### MultipleThreads_ShouldExecuteActionsInOrder

- 여러 스레드에서 `Enqueue`된 액션들이 순서대로 실행되는지 확인합니다.

### WhenGameObjectDisabled_ShouldHandleCoroutineProperly

- `GameObject`가 비활성화되었을 때 코루틴이 올바르게 처리되는지 확인합니다.

### WhenApplicationQuitting_ShouldRejectNewActions

- 애플리케이션이 종료 중일 때 새로운 액션이 거부되는지 확인합니다.

### LongRunningTasks_ShouldNotBlockMainThread

- 긴 시간이 걸리는 작업이 메인 스레드를 차단하지 않는지 확인합니다.

### DomainReload_ShouldResetDispatcher

- 도메인 리로드 시 디스패처가 초기화되는지 확인합니다. (Unity 에디터 환경)

### StressTest_MassiveQueueing

- 대량의 작업을 큐에 추가했을 때 디스패처가 안정적으로 동작하는지 확인합니다.

### ChaosTest_RandomOperations

- 다양한 랜덤 작업을 실행하여 디스패처의 안정성을 테스트합니다.
