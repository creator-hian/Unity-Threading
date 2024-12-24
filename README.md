# Unity Threading

Unity에서 스레드 관리와 비동기 작업을 위한 종합적인 도구 모음입니다.

## 요구사항

- Unity 2021.3 이상
- .NET Standard 2.1
- Unity 6.0 이상 (Awaitable 기능 사용 시)

## 개요

Unity에서 멀티스레딩과 비동기 작업을 쉽고 안전하게 처리할 수 있는 도구들을 제공합니다. 메인 스레드 디스패처, 스레드 풀, 태스크 관리 등 다양한 기능을 포함합니다.

## 주요 기능

### 메인 스레드 디스패처

- 스레드 안전한 작업 실행
- 코루틴 지원
- 비동기 작업 지원 (Task 기반)
- Unity 6.0+ Awaitable 지원
- 자동 인스턴스 생성
- 씬 전환 시 유지
- 도메인 리로드 대응

## 사용 예제

### 기본 사용

```csharp
// 메인 스레드에서 작업 실행
UnityMainThreadDispatcher.Instance.Enqueue(() => {
    // UI 업데이트 등 메인 스레드 작업
});

// 비동기 작업
await UnityMainThreadDispatcher.Instance.EnqueueAsync(() => {
    // 메인 스레드에서 실행될 작업
});

// 결과값이 필요한 경우
var result = await UnityMainThreadDispatcher.Instance.EnqueueAsync(() => {
    return someCalculation();
});

// 코루틴 실행
UnityMainThreadDispatcher.Instance.Enqueue(SomeCoroutine());

#if UNITY_2023_1_OR_NEWER
// Unity 6.0+ Awaitable 지원
await UnityMainThreadDispatcher.Instance.EnqueueAwaitable(someAwaitable);
#endif
```

### 에러 처리

```csharp
try 
{
    await UnityMainThreadDispatcher.Instance.EnqueueAsync(() => {
        // 예외가 발생할 수 있는 작업
    });
}
catch (Exception ex)
{
    Debug.LogException(ex);
}
```

## 스레드 안전성

- 모든 public API는 스레드로부터 안전합니다.
- 큐 조작은 lock으로 보호됩니다.
- 도메인 리로드 시 안전하게 정리됩니다.

## 설치 방법

### UPM을 통한 설치 (Git URL 사용)

#### 선행 조건

- Git 클라이언트(최소 버전 2.14.0)가 설치되어 있어야 합니다.
- Windows 사용자의 경우 `PATH` 시스템 환경 변수에 Git 실행 파일 경로가 추가되어 있어야 합니다.

#### 설치 방법 1: Package Manager UI 사용

1. Unity 에디터에서 Window > Package Manager를 엽니다.
2. 좌측 상단의 + 버튼을 클릭하고 "Add package from git URL"을 선택합니다.

   ![Package Manager Add Git URL](https://i.imgur.com/1tCNo66.png)

3. 다음 URL을 입력합니다:

```text
https://github.com/creator-hian/Unity-Common.git
```
<!-- markdownlint-disable MD029 -->
4. 'Add' 버튼을 클릭합니다.

   ![Package Manager Add Button](https://i.imgur.com/yIiD4tT.png)
<!-- markdownlint-enable MD029 -->

#### 설치 방법 2: manifest.json 직접 수정

1. Unity 프로젝트의 `Packages/manifest.json` 파일을 열어 다음과 같이 dependencies 블록에 패키지를 추가하세요:

```json
{
  "dependencies": {
    "com.creator-hian.unity.threading": "hhttps://github.com/creator-hian/Unity-Threading.git",
    ...
  }
}
```

#### 특정 버전 설치

특정 버전을 설치하려면 URL 끝에 #{version} 을 추가하세요:

```json
{
  "dependencies": {
     "com.creator-hian.unity.threading": "hhttps://github.com/creator-hian/Unity-Threading.git#0.0.1",
    ...
  }
}
```

#### 참조 문서

- [Unity 공식 매뉴얼 - Git URL을 통한 패키지 설치](https://docs.unity3d.com/kr/2023.2/Manual/upm-ui-giturl.html)

## 문서

각 기능에 대한 자세한 설명은 해당 기능의 README를 참조하세요:

## 원작성자

- [Hian](https://github.com/creator-hian)

## 기여자

## 라이센스

[라이센스 정보 추가 필요]
