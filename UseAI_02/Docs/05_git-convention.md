# 05. Git / GitHub 컨벤션

## 1. 초기 설정 (한 번만)
- Unity 전용 `.gitignore` 적용 (GitHub의 Unity 템플릿 사용) → `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/` 등 제외
- `Edit > Project Settings > Editor`
  - Asset Serialization Mode: **Force Text**
  - Version Control Mode: **Visible Meta Files**
- `.meta` 파일은 **반드시 함께 커밋**한다. (빠지면 참조가 깨짐)
- 용량 큰 바이너리(음원, 고용량 텍스처, 모델)가 많아지면 Git LFS 도입을 검토한다.

## 2. 브랜치

| 브랜치 | 용도 |
|---|---|
| `main` | 항상 실행 가능한 상태만 유지 |
| `feature/기능명` | 기능 개발 (예: `feature/player-move`) |
| `fix/버그명` | 버그 수정 (예: `fix/enemy-null-ref`) |

- 기능 하나 = 브랜치 하나. 완료되면 PR을 만들어 `main`에 머지한다. (혼자라도 PR로 기록을 남김)

## 3. 커밋 메시지

```
<타입>: <무엇을 했는지 한 줄 요약>

- (선택) 상세 내용 / 이유
- (선택) Closes #이슈번호
```

| 타입 | 의미 |
|---|---|
| `feat` | 새 기능 |
| `fix` | 버그 수정 |
| `refactor` | 동작 변화 없는 구조 개선 |
| `asset` | 에셋·씬·프리팹 추가/수정 |
| `docs` | 문서(README, 지침 문서 등) |
| `chore` | 설정, 패키지, 기타 |

예시: `feat: 플레이어 점프 및 이중 점프 구현`

- 커밋은 "의미 있는 작은 단위"로 자주 한다. 하루 작업을 하나로 몰아서 커밋하지 않는다.
- 씬 파일은 충돌이 잘 나므로, 작업 중인 씬을 명확히 하고 기능 테스트는 테스트 씬에서 한다.

## 4. 작업 기록
- **Issues**: 할 일·버그는 이슈로 등록한다. (라벨: `feature`, `bug`, `refactor`, `docs`)
- **Projects(칸반)**: `Todo / In Progress / Done` 보드로 진행 상황을 관리한다.
- **Milestones**: 주차별 또는 MVP / 폴리싱 단계별로 묶는다.
- **README.md**: 게임 소개, 조작법, 스크린샷/GIF, 실행 방법, 개발 기간을 정리한다.
- 공부한 내용·트러블슈팅은 블로그에 정리하고 README나 이슈에 링크를 남긴다.
