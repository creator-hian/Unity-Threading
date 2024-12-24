using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hian.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class UnityMainThreadDispatcherTests : MonoBehaviour
{
    private UnityMainThreadDispatcher _dispatcher;
    private static UnityMainThreadDispatcherTests _instance;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        GameObject go = new GameObject("TestRunner");
        _instance = go.AddComponent<UnityMainThreadDispatcherTests>();
        _dispatcher = UnityMainThreadDispatcher.Instance;
        Assert.That(_dispatcher, Is.Not.Null, "Dispatcher should be initialized");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_dispatcher != null && _dispatcher.gameObject != null)
        {
            Destroy(_dispatcher.gameObject);
            _dispatcher = null;
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator Enqueue_ShouldExecuteActionOnMainThread()
    {
        bool executed = false;
        int executedThreadId = -1;
        int mainThreadId = Thread.CurrentThread.ManagedThreadId;

        _dispatcher.Enqueue(() =>
        {
            executed = true;
            executedThreadId = Thread.CurrentThread.ManagedThreadId;
        });

        yield return null; // 다음 프레임까지 대기

        Assert.That(executed, Is.True, "Action should be executed");
        Assert.That(executedThreadId, Is.EqualTo(mainThreadId), "Action should run on main thread");
    }

    [UnityTest]
    public IEnumerator Instance_ShouldReturnSameInstance()
    {
        UnityMainThreadDispatcher instance1 = UnityMainThreadDispatcher.Instance;
        UnityMainThreadDispatcher instance2 = UnityMainThreadDispatcher.Instance;

        Assert.That(instance1, Is.Not.Null);
        Assert.That(instance2, Is.Not.Null);
        Assert.That(instance1, Is.EqualTo(instance2));

        yield return null;
    }

    [UnityTest]
    public IEnumerator Enqueue_WhenNull_ShouldThrowArgumentNullException()
    {
        Action action = null;
        _ = Assert.Throws<ArgumentNullException>(() => _dispatcher.Enqueue(action));
        yield return null;
    }

    [UnityTest]
    public IEnumerator EnqueueCoroutine_WhenNull_ShouldThrowArgumentNullException()
    {
        IEnumerator coroutine = null;
        _ = Assert.Throws<ArgumentNullException>(() => _dispatcher.Enqueue(coroutine));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClearQueue_ShouldRemoveAllPendingActions()
    {
        for (int i = 0; i < 10; i++)
        {
            _dispatcher.Enqueue(static () => { });
        }

        _dispatcher.ClearQueue();
        yield return null; // 다음 프레임까지 대기

        Assert.That(_dispatcher.QueuedActions, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator EnqueueAsync_ShouldHandleExceptions()
    {
        Exception expectedException = new Exception("Test Exception");
        Exception actualException = null;

        _ = _dispatcher
            .EnqueueAsync(() => throw expectedException)
            .ContinueWith(t => actualException = t.Exception?.InnerException);

        yield return new WaitForSeconds(0.1f); // 작업 완료 대기

        Assert.That(actualException, Is.EqualTo(expectedException));
    }

    [UnityTest]
    public IEnumerator EnqueueAsync_ShouldExecuteAndReturnResult()
    {
        const int expectedResult = 42;
        int? result = null;

        _ = _dispatcher.EnqueueAsync(() => expectedResult).ContinueWith(t => result = t.Result);

        yield return new WaitForSeconds(0.1f); // 작업 완료 대기

        Assert.That(result, Is.EqualTo(expectedResult));
    }

#if UNITY_2023_1_OR_NEWER
    [UnityTest]
    public IEnumerator EnqueueAwaitable_ShouldExecuteAwaitable()
    {
        bool executed = false;

        async void TestAwaitableAsync()
        {
            try
            {
                await _dispatcher.EnqueueAwaitable(Awaitable.WaitForSecondsAsync(0.1f));
                executed = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        TestAwaitableAsync();
        yield return new WaitForSeconds(0.2f);

        Assert.That(executed, Is.True, "Awaitable should be executed");
    }
#endif

    [UnityTest]
    public IEnumerator MultipleThreads_ShouldExecuteActionsInOrder()
    {
        List<int> executionOrder = new List<int>();
        List<Task> tasks = new List<Task>();
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        // 메인 스레드에서 먼저 작업 추가
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            _dispatcher.Enqueue(() => executionOrder.Add(index));
        }

        // 다른 스레드에서 작업 추가
        _ = Task.Run(() =>
        {
            for (int i = 5; i < 10; i++)
            {
                int index = i;
                _dispatcher.Enqueue(() => executionOrder.Add(index));
            }
            taskCompletionSource.SetResult(true);
        });

        yield return new WaitUntil(() => taskCompletionSource.Task.IsCompleted);
        yield return null; // 다음 프레임까지 대기

        Assert.That(executionOrder.Count, Is.EqualTo(10), "모든 작업이 실행되어야 함");

        // 메인 스레드 작업이 먼저 실행되고, 그 다음 다른 스레드 작업이 실행되는지 확인
        Assert.That(
            executionOrder.Take(5),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }),
            "메인 스레드 작업이 순서대로 실행되어야 함"
        );
        Assert.That(
            executionOrder.Skip(5),
            Is.EqualTo(new[] { 5, 6, 7, 8, 9 }),
            "다른 스레드 작업이 순서대로 실행되어야 함"
        );
    }

    [UnityTest]
    public IEnumerator WhenGameObjectDisabled_ShouldHandleCoroutineProperly()
    {
        bool executed = false;

        IEnumerator TestCoroutine()
        {
            yield return new WaitForSeconds(0.1f);
            executed = true;
        }

        _dispatcher.gameObject.SetActive(false);
        _ = Assert.Throws<InvalidOperationException>(() => _dispatcher.Enqueue(TestCoroutine()));

        yield return null;
        Assert.That(executed, Is.False);
    }

    [UnityTest]
    public IEnumerator WhenApplicationQuitting_ShouldRejectNewActions()
    {
        // OnApplicationQuit 시뮬레이션
        System.Reflection.FieldInfo quitField = typeof(UnityMainThreadDispatcher).GetField(
            "_isQuitting",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        quitField.SetValue(null, true);

        Assert.That(UnityMainThreadDispatcher.Instance, Is.Null);
        yield return null;

        // 정리
        quitField.SetValue(null, false);
    }

    [UnityTest]
    public IEnumerator LongRunningTasks_ShouldNotBlockMainThread()
    {
        int frameCount = Time.frameCount;
        bool longTaskCompleted = false;

        _ = _dispatcher.EnqueueAsync(() =>
        {
            Thread.Sleep(100); // 긴 작업 시뮬레이션
            longTaskCompleted = true;
        });

        yield return null;

        Assert.That(Time.frameCount, Is.GreaterThan(frameCount), "프레임이 진행되어야 함");
        yield return new WaitUntil(() => longTaskCompleted);
    }

    [UnityTest]
    public IEnumerator DomainReload_ShouldResetDispatcher()
    {
#if UNITY_EDITOR
        // 도메인 리로드 시뮬레이션
        System.Reflection.MethodInfo resetMethod = typeof(UnityMainThreadDispatcher).GetMethod(
            "ResetStatics",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        resetMethod.Invoke(null, null);

        yield return null;

        UnityMainThreadDispatcher newDispatcher = UnityMainThreadDispatcher.Instance;
        Assert.That(newDispatcher, Is.Not.Null);
        Assert.That(newDispatcher.QueuedActions, Is.Zero);
#else
        yield return null;
#endif
    }

    [UnityTest]
    public IEnumerator StressTest_MassiveQueueing()
    {
        const int TOTAL_ACTIONS = 1000;
        List<Task> tasks = new List<Task>();
        int completedCount = 0;
        List<Exception> errors = new List<Exception>();
        System.Random random = new System.Random();
        int enqueuedCount = 0;

        Debug.Log($"스트레스 테스트 시작: {TOTAL_ACTIONS}개의 작업 예약");

        // 작업을 한 번에 모두 예약하지 않고 배치로 나누어 실행
        const int BATCH_SIZE = 50;
        for (int batch = 0; batch < TOTAL_ACTIONS / BATCH_SIZE; batch++)
        {
            Debug.Log(
                $"배치 {batch + 1} 시작: {batch * BATCH_SIZE}~{((batch + 1) * BATCH_SIZE) - 1}"
            );

            for (int i = 0; i < BATCH_SIZE; i++)
            {
                int currentIndex = (batch * BATCH_SIZE) + i;
                tasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(random.Next(10, 50));
                            if (_dispatcher != null)
                            {
                                Action action = new Action(() =>
                                {
                                    try
                                    {
                                        if (_dispatcher != null)
                                        {
                                            int currentEnqueued = Interlocked.Increment(
                                                ref enqueuedCount
                                            );
                                            Debug.Log(
                                                $"작업 실행 중: {currentEnqueued}/{TOTAL_ACTIONS}"
                                            );

                                            _ = _dispatcher.StartCoroutine(
                                                SimulateNetworkLatency()
                                            );
                                            int count = Interlocked.Increment(ref completedCount);
                                            Debug.Log($"작업 완료: {count}/{TOTAL_ACTIONS}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"작업 실행 중 오류: {ex.Message}");
                                        lock (errors)
                                        {
                                            errors.Add(ex);
                                        }
                                    }
                                });

                                _dispatcher.Enqueue(action);
                            }
                            else
                            {
                                lock (errors)
                                {
                                    errors.Add(
                                        new InvalidOperationException("Dispatcher is not available")
                                    );
                                    Debug.LogError("디스패처 없음 오류 발생");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                            {
                                errors.Add(ex);
                                Debug.LogError($"예상치 못한 오류 발생: {ex.Message}");
                            }
                        }
                    })
                );
            }

            // 각 배치 완료 후 잠시 대기
            yield return new WaitForSeconds(0.1f);
            Debug.Log(
                $"배치 {batch + 1} 예약 완료. 현재까지 완료된 작업: {completedCount}/{TOTAL_ACTIONS}"
            );
        }

        if (_dispatcher != null)
        {
            Debug.Log("GameObject 토글 코루틴 시작");
            _ = _dispatcher.StartCoroutine(ToggleGameObjectRoutine());
        }

        float timeout = Time.time + 30f; // 30초 타임아웃 설정
        Debug.Log("모든 태스크 완료 대기 중...");

        while (!tasks.All(t => t.IsCompleted) && Time.time < timeout)
        {
            Debug.Log(
                $"진행 상황 - 완료: {completedCount}/{TOTAL_ACTIONS}, 에러: {errors.Count}, 큐잉: {enqueuedCount}"
            );
            yield return new WaitForSeconds(1f);
        }

        Debug.Log(
            $"태스크 완료됨. 작업 결과 대기 중... (완료: {completedCount}/{TOTAL_ACTIONS}, 에러: {errors.Count})"
        );

        timeout = Time.time + 30f;
        while (completedCount < TOTAL_ACTIONS && !errors.Any() && Time.time < timeout)
        {
            Debug.Log(
                $"작업 완료 대기 중 - 완료: {completedCount}/{TOTAL_ACTIONS}, 에러: {errors.Count}"
            );
            yield return new WaitForSeconds(1f);
        }

        Debug.Log(
            $"테스트 종료 - 완료된 작업: {completedCount}/{TOTAL_ACTIONS}, 에러 수: {errors.Count}"
        );
        if (Time.time >= timeout)
        {
            Assert.Fail($"테스트 타임아웃 - 완료된 작업: {completedCount}/{TOTAL_ACTIONS}");
        }

        Assert.That(errors, Is.Empty, "에러 없이 실행되어야 함");
        Assert.That(completedCount, Is.EqualTo(TOTAL_ACTIONS), "모든 작업이 실행되어야 함");
        Assert.That(_dispatcher, Is.Not.Null, "디스패처는 살아있어야 함");
    }

    [UnityTest]
    public IEnumerator ChaosTest_RandomOperations()
    {
        const int TOTAL_OPERATIONS = 50;
        System.Random random = new System.Random();
        List<(string name, Func<IEnumerator> operation)> operations = new List<(
            string name,
            Func<IEnumerator> operation
        )>
        {
            ("GC", SimulateGarbageCollection),
            ("씬 리로드", SimulateSceneReload),
            ("앱 일시정지", SimulateAppPause),
            ("네트워크 지연", SimulateNetworkLatency),
            ("메모리 압박", SimulateMemoryPressure),
        };

        int completedOps = 0;
        List<Exception> errors = new List<Exception>();
        Dictionary<int, (string name, bool completed)> operationStatus =
            new Dictionary<int, (string name, bool completed)>();

        Debug.Log($"카오스 테스트 시작: {TOTAL_OPERATIONS}개의 랜덤 작업 실행");
        Debug.Log($"사용 가능한 작업 타입: {string.Join(", ", operations.Select(o => o.name))}");

        // 작업 상태 초기화
        for (int i = 0; i < TOTAL_OPERATIONS; i++)
        {
            int opIndex = random.Next(operations.Count);
            operationStatus[i] = (operations[opIndex].name, false);
        }

        // 랜덤하게 작업 실행
        for (int i = 0; i < TOTAL_OPERATIONS; i++)
        {
            int opIndex = random.Next(operations.Count);
            (string opName, Func<IEnumerator> operation) = operations[opIndex];

            if (_dispatcher != null)
            {
                Debug.Log($"작업 {i + 1}/{TOTAL_OPERATIONS} 예약 중... (작업 종류: {opName})");

                _ = _dispatcher.StartCoroutine(
                    ExecuteWithErrorHandling(
                        operation(),
                        () =>
                        {
                            lock (operationStatus)
                            {
                                operationStatus[i] = (opName, true);
                                int count = Interlocked.Increment(ref completedOps);
                                Debug.Log(
                                    $"작업 {i + 1} ({opName}) 완료 - 전체 진행: {count}/{TOTAL_OPERATIONS}"
                                );
                            }
                        },
                        ex =>
                        {
                            lock (errors)
                            {
                                errors.Add(ex);
                                Debug.LogError(
                                    $"작업 {i + 1} ({opName}) 실행 중 오류: {ex.Message}"
                                );
                            }
                        }
                    )
                );
            }
            else
            {
                errors.Add(new InvalidOperationException("Dispatcher is not available"));
                Debug.LogError($"작업 {i + 1} ({opName}) 예약 실패: 디스패처가 없음");
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        float timeout = Time.time + 30f; // 30초 타임아웃
        Debug.Log("작업 완료 대기 중...");

        while (completedOps < TOTAL_OPERATIONS && !errors.Any() && Time.time < timeout)
        {
            lock (operationStatus)
            {
                List<string> incomplete = operationStatus
                    .Where(kvp => !kvp.Value.completed)
                    .Select(kvp => $"{kvp.Key + 1}({kvp.Value.name})")
                    .ToList();

                if (incomplete.Any())
                {
                    Debug.Log(
                        $"진행 상황 - 완료: {completedOps}/{TOTAL_OPERATIONS}, 에러: {errors.Count}"
                    );
                    Debug.Log($"미완료 작업: {string.Join(", ", incomplete)}");
                }
            }

            yield return new WaitForSeconds(1f);
        }

        Debug.Log(
            $"테스트 종료 - 예료된 작업: {completedOps}/{TOTAL_OPERATIONS}, 에러 수: {errors.Count}"
        );

        if (Time.time >= timeout)
        {
            List<string> incompleteOps = operationStatus
                .Where(kvp => !kvp.Value.completed)
                .Select(kvp => $"{kvp.Key + 1}({kvp.Value.name})")
                .ToList();

            Assert.Fail(
                $"테스트 타임아웃 - 완료된 작업: {completedOps}/{TOTAL_OPERATIONS}\n미완료 작업: {string.Join(", ", incompleteOps)}"
            );
        }

        Assert.That(errors, Is.Empty, "에러 없이 실행되어야 함");
        Assert.That(completedOps, Is.EqualTo(TOTAL_OPERATIONS), "모든 작업이 실행되어야 함");
        Assert.That(_dispatcher, Is.Not.Null, "디스패처는 살아있어야 함");
    }

    private IEnumerator ToggleGameObjectRoutine()
    {
        for (int i = 0; i < 10; i++)
        {
            _dispatcher.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            _dispatcher.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator SimulateGarbageCollection()
    {
        Debug.Log("GC 작업 시작");
        GC.Collect();
        yield return null;
        Debug.Log("GC 작업 완료");
    }

    private IEnumerator SimulateSceneReload()
    {
        Debug.Log("씬 리로드 시뮬레이션 시작");
        yield return new WaitForSeconds(0.1f);
        Debug.Log("씬 리로드 시뮬레이션 완료");
    }

    private IEnumerator SimulateAppPause()
    {
        Debug.Log("앱 일시정지 시뮬레이션 시작");
        yield return new WaitForSeconds(0.2f);
        Debug.Log("앱 일시정지 시뮬레이션 완료");
    }

    private IEnumerator SimulateNetworkLatency()
    {
        Debug.Log("네트워크 지연 시뮬레이션 시작");
        yield return new WaitForSeconds(0.3f);
        Debug.Log("네트워크 지연 시뮬레이션 완료");
    }

    private IEnumerator SimulateMemoryPressure()
    {
        Debug.Log("메모리 압박 시뮬레이션 시작");
        _ = new byte[1024 * 1024]; // 1MB
        yield return new WaitForSeconds(0.1f);
        Debug.Log("메모리 압박 시뮬레이션 완료");
    }

    private IEnumerator ExecuteWithErrorHandling(
        IEnumerator routine,
        Action onComplete,
        Action<Exception> onError
    )
    {
        if (_dispatcher == null)
        {
            onError?.Invoke(new InvalidOperationException("Dispatcher is not available"));
            yield break;
        }

        bool isComplete = false;
        object current = null;

        while (!isComplete && _dispatcher != null)
        {
            try
            {
                if (routine.MoveNext())
                {
                    current = routine.Current;
                }
                else
                {
                    isComplete = true;
                    onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                isComplete = true;
                onError?.Invoke(ex);
            }

            if (!isComplete && current != null)
            {
                yield return current;
            }
        }

        if (!isComplete && _dispatcher == null)
        {
            onError?.Invoke(
                new InvalidOperationException("Dispatcher was destroyed during execution")
            );
        }
    }
}
