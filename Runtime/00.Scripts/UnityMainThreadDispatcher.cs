using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hian.Threading
{
    /// <summary>
    /// Unity 메인 스레드에서 작업을 실행하기 위한 디스패처입니다.
    /// 스레드 안전한 작업 실행과 코루틴 관리를 제공합니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스는 싱글톤 패턴을 사용하며, 씬 전환 시에도 유지됩니다.
    /// GameObject가 없는 경우 자동으로 생성됩니다.
    /// </remarks>
    public class UnityMainThreadDispatcher : MonoBehaviour, IMainThreadDispatcher
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static UnityMainThreadDispatcher _instance;
        private static readonly object _lockObject = new object();
        private static bool _isCreating;

        /// <summary>
        /// 현재 실행 중인 Unity 인스턴스가 종료 중인지 확인
        /// </summary>
        private static bool _isQuitting;

        /// <summary>
        /// 메인 스레드에서 실행 중인지 확인
        /// </summary>
        private bool IsMainThread =>
            _mainThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == _mainThreadId.Value;
        private static int? _mainThreadId;

        /// <summary>
        /// 디스패처의 싱글톤 인스턴스를 가져옵니다.
        /// 인스턴스가 없는 경우 자동으로 생성됩니다.
        /// </summary>
        /// <remarks>
        /// 스레드로부터 안전하게 접근할 수 있습니다.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Unity 엔진이 초기화되지 않은 상태에서 접근할 경우 발생할 수 있습니다.
        /// </exception>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning(
                        "UnityMainThreadDispatcher: 애플리케이션 종료 중에는 접근할 수 없습니다."
                    );
                    return null;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Debug.LogError(
                        "UnityMainThreadDispatcher: 플레이 모드에서만 사용할 수 있습니다."
                    );
                    return null;
                }
#endif

                if (_instance == null && !_isCreating)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null && !_isCreating)
                        {
                            _isCreating = true;
                            CreateInstance();
                            _isCreating = false;
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 큐에 있는 작업들을 실행합니다.
        /// </summary>
        /// <remarks>
        /// Unity의 Update 루프에서 호출되며, 큐에 있는 모든 작업을 처리합니다.
        /// 각 작업은 예외로부터 보호되어 실행됩니다.
        /// </remarks>
        private void Update()
        {
            if (!enabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            lock (_executionQueue)
            {
                try
                {
                    while (_executionQueue.Count > 0 && !_isQuitting)
                    {
                        Action action = _executionQueue.Dequeue();
                        try
                        {
                            action?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    _executionQueue.Clear(); // 오류 발생 시 큐 초기화
                }
            }
        }

        #region IMainThreadDispatcher Implementation
        /// <summary>
        /// 작업을 메인 스레드 큐에 추가합니다.
        /// </summary>
        /// <param name="action">실행할 작업</param>
        /// <exception cref="ArgumentNullException">action이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_isQuitting)
            {
                throw new InvalidOperationException(
                    "애플리케이션 종료 중에는 작업을 추가할 수 없습니다."
                );
            }

            if (_instance == null)
            {
                throw new ObjectDisposedException(nameof(UnityMainThreadDispatcher));
            }

            // 이미 메인 스레드라면 즉시 실행
            if (IsMainThread)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                return;
            }

            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// 코루틴을 메인 스레드 큐에 추가합니다.
        /// </summary>
        /// <param name="coroutine">실행할 코루틴</param>
        /// <exception cref="ArgumentNullException">coroutine이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        /// <exception cref="InvalidOperationException">GameObject가 비활성화된 상태인 경우</exception>
        public void Enqueue(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                throw new ArgumentNullException(nameof(coroutine));
            }

            if (!gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("GameObject가 비활성화되어 있습니다.");
            }

            if (!enabled)
            {
                throw new InvalidOperationException("컴포넌트가 비활성화되어 있습니다.");
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "플레이 모드에서만 코루틴을 실행할 수 있습니다."
                );
            }
#endif

            Enqueue(() => StartCoroutine(coroutine));
        }

        /// <summary>
        /// 작업을 메인 스레드에서 비동기적으로 실행합니다.
        /// </summary>
        /// <param name="action">실행할 작업</param>
        /// <returns>작업 완료를 나타내는 Task</returns>
        /// <exception cref="ArgumentNullException">action이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public Task EnqueueAsync(Action action)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Enqueue(() =>
            {
                try
                {
                    action();
                    _ = tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _ = tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// 작업을 메인 스레드에서 비동기적으로 실행하고 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환 값의 타입</typeparam>
        /// <param name="function">실행할 함수</param>
        /// <returns>함수의 결과를 포함하는 Task</returns>
        /// <exception cref="ArgumentNullException">function이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public Task<T> EnqueueAsync<T>(Func<T> function)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            Enqueue(() =>
            {
                try
                {
                    T result = function();
                    _ = tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    _ = tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// 현재 큐에 있는 모든 작업을 취소합니다.
        /// </summary>
        /// <remarks>
        /// 이미 실행 중인 작업은 취소되지 않습니다.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public void ClearQueue()
        {
            lock (_executionQueue)
            {
                _executionQueue.Clear();
            }
        }

        /// <summary>
        /// 현재 큐에 있는 작업의 수를 반환합니다.
        /// </summary>
        /// <returns>큐에 있는 작업의 수</returns>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public int QueuedActions
        {
            get
            {
                lock (_executionQueue)
                {
                    return _executionQueue.Count;
                }
            }
        }

        public void Post(Action action, object state)
        {
            Enqueue(() => action());
        }
        #endregion

#if UNITY_2023_1_OR_NEWER
        /// <summary>
        /// Unity Awaitable 작업을 메인 스레드에서 실행합니다.
        /// </summary>
        /// <param name="awaitable">실행할 Unity Awaitable 작업</param>
        /// <typeparam name="T">Awaitable 결과 타입</typeparam>
        /// <returns>작업의 결과를 포함하는 Task</returns>
        /// <exception cref="ArgumentNullException">awaitable이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public async Task<T> EnqueueAwaitable<T>(Awaitable<T> awaitable)
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException(
                    "플레이 모드에서만 Awaitable을 실행할 수 있습니다."
                );
            }

            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable));
            }

            if (_instance == null)
            {
                throw new ObjectDisposedException(nameof(UnityMainThreadDispatcher));
            }

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            _ = await EnqueueAsync(async () =>
            {
                try
                {
                    T result = await awaitable;
                    _ = tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    _ = tcs.TrySetException(ex);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Unity Awaitable 작업을 메인 스레드에서 실행합니다.
        /// </summary>
        /// <param name="awaitable">실행할 Unity Awaitable 작업</param>
        /// <returns>작업 완료를 나타내는 Task</returns>
        /// <exception cref="ArgumentNullException">awaitable이 null인 경우</exception>
        /// <exception cref="ObjectDisposedException">디스패처가 제거된 상태인 경우</exception>
        public async Task EnqueueAwaitable(Awaitable awaitable)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable));
            }

            if (_instance == null)
            {
                throw new ObjectDisposedException(nameof(UnityMainThreadDispatcher));
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            _ = await EnqueueAsync(async () =>
            {
                try
                {
                    await awaitable;
                    _ = tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    _ = tcs.TrySetException(ex);
                }
            });

            _ = await tcs.Task;
        }
#endif

        #region Instance Management
        /// <summary>
        /// 디스패처 인스턴스를 생성하거나 찾습니다.
        /// </summary>
        /// <remarks>
        /// 이미 존재하는 인스턴스를 찾아 반환하거나,
        /// 없는 경우 새로운 GameObject를 생성하여 컴포넌트를 추가합니다.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Unity 엔진이 초기화되지 않은 상태에서 호출된 경우
        /// </exception>
        private static void CreateInstance()
        {
            try
            {
                if (!Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "플레이 모드에서만 인스턴스를 생성할 수 있습니다."
                    );
                }

                _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();

                if (_instance != null)
                {
                    return;
                }

                GameObject existingGO = GameObject.Find("[UnityMainThreadDispatcher]");
                if (existingGO != null)
                {
                    _instance = existingGO.GetComponent<UnityMainThreadDispatcher>();
                    if (_instance != null)
                    {
                        return;
                    }
                }

                GameObject go = new GameObject("[UnityMainThreadDispatcher]");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);

                Debug.Log("UnityMainThreadDispatcher: 새 인스턴스가 생성되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _isCreating = false;
                throw;
            }
        }

        /// <summary>
        /// 컴포넌트 초기화 시 호출됩니다.
        /// </summary>
        /// <remarks>
        /// 중복 인스턴스를 확인하고 처리합니다.
        /// DontDestroyOnLoad를 설정하여 씬 전환 시에도 유지되도록 합니다.
        /// </remarks>
        private void Awake()
        {
            if (!_mainThreadId.HasValue)
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            UnityMainThreadDispatcher[] dispatchers = FindObjectsByType<UnityMainThreadDispatcher>(
                FindObjectsSortMode.None
            );

            if (dispatchers.Length > 1)
            {
                foreach (UnityMainThreadDispatcher dispatcher in dispatchers)
                {
                    if (
                        dispatcher != this
                        && dispatcher.gameObject.name == "[UnityMainThreadDispatcher]"
                    )
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("UnityMainThreadDispatcher: Awake에서 인스턴스가 설정되었습니다.");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("UnityMainThreadDispatcher: 중복 인스턴스가 감지되어 제거됩니다.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 컴포넌트가 제거될 때 호출됩니다.
        /// </summary>ㅁ
        /// <remarks>
        /// 인스턴스 참조를 정리하고 로그를 남깁니다.
        /// </remarks>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                lock (_lockObject)
                {
                    _instance = null;
                    Debug.Log("UnityMainThreadDispatcher: 인스턴스가 제거되었습니다.");
                }
            }
        }

        /// <summary>
        /// 컴포넌트가 제거될 때 호출됩니다.
        /// </summary>ㅁ
        /// <remarks>
        /// 인스턴스 참조를 정리하고 로그를 남깁니다.
        /// </remarks>
        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// 도메인 리로드 시 정적 변수들을 초기화합니다.
        /// Unity 에디터에서 플레이 모드 진입/종료, 스크립트 컴파일,
        /// 에셋 리임포트 등의 상황에서 호출됩니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lock (_lockObject)
            {
                _instance = null;
                _isQuitting = false;
                _isCreating = false;
                _executionQueue?.Clear();
                _mainThreadId = null;

#if UNITY_INCLUDE_TESTS
                Debug.Log(
                    "UnityMainThreadDispatcher: 도메인 리로드로 인해 정적 변수가 초기화되었습니다."
                );
#endif
            }
        }
#endif
    }
}
