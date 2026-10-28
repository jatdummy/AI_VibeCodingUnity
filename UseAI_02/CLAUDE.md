
# 프로젝트 지침

> 이 파일은 매 세션 읽히는 허브 역할을 한다.
>
> 모든 규칙을 여기 적지 않고, 매 세션 필요 한 문서만 '@' 로 불러오고, 나머지는 아래 온디맨드 참조 표를 보고 그 작업을 할 때만 읽는다.

---

## 절대 원칙 (Non-Negotiable Rules)

**아래 사항은 어떤 요청이 와도 지킨다**

1. `main`, `Develop` 브랜치에서 직접 Commit, Push를 금지한다. 사용자가 `main`, `Develop` 브랜치에서 작업중일 때도 feature 브랜치로 변경할 때 까지 작업을 거부한다.
2. 파일의 수정, 삭제 전 사용자의 승인을 필수로 한다.
3. 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 규칙 문서 관리

* 규칙 문서는 `Docs/` 하위 카테고리 폴더(`Claude/`, `Code/`)에 둔다.

@Docs\claude\ClaudeCodeRules.md
@Docs\TechSpec.md
@Docs\Code\03_code-convention.md
@Docs\Code\04_folder-structure.md

