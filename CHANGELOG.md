# Changelog

All notable changes to this project will be documented in this file.

## 버전 관리 정책

이 프로젝트는 Semantic Versioning을 따릅니다:

- **Major.Minor.Patch** 형식
  - **Major**: 호환성이 깨지는 변경
  - **Minor**: 하위 호환성 있는 기능 추가
  - **Patch**: 하위 호환성 있는 버그 수정
- **최신 버전이 상단에, 이전 버전이 하단에 기록됩니다.**

## [0.0.2] - 2024-12-06

### Changed

- 잦은 로그 발생을 `UNITY_INCLUDE_TESTS` 심볼 정의에 따라 로깅하도록 변경.

## [0.0.1] - 2024-12-05

### Added

- 기본 디스패처 기능 구현
  - 스레드 안전한 작업 실행
  - 코루틴 지원
  - 비동기 작업 지원
  - Unity 6.0+ Awaitable 지원
  - 자동 인스턴스 생성
  - 씬 전환 시 유지

- 안전성 기능
  - 도메인 리로드 대응
  - 중복 인스턴스 방지
  - 종료 시 정리
  - 예외 처리

- 최적화 기능
  - 메인 스레드 감지
  - 즉시 실행 최적화
  - 락 메커니즘

### Changed

- 초기 릴리스

### Fixed

- 초기 릴리스