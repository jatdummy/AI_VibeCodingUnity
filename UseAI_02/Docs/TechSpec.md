# Tech Specification

> 이 프로젝트의 기술 스택 관련 결정 사항이다.
> Claude Code는 코드를 작성할 때 이 문서를 기준으로 한다.

---

## 프로젝트 제원

- 프로젝트명 : UnityAISetting
- 장르 : 3D 탑다운 슈팅
- 팀 구성 : 1인

## Unity 환경
- 버전 : 6000.3.9.f1 LTS
- 렌더 파이프라인 : URP
- 빌드 플랫폼 : Windows

## 유저 입력 시스템

- new InputSystem
  - 구 `InputManager`는 사용하지 않는다.
- 특성 입력에 대한 기능 구현 시 `InputActionReference`를 사용해 InputAction을 참조하는 형태로 구현한다.
- OnEnable에서 InputAction에 대한 Enable(), OnDisable에서 Disable()을 사용한다. 