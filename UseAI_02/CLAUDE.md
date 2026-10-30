
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

- 규칙 문서는 `Docs/` 하위 카테고리 폴더(`Claude/`, `Code/`, `Documentation/`, `Template/`)에 둔다.
- 규칙 하나는 문서 한 곳에만 적는다(규칙의 단일출처 유지), 규칙이 바뀌면 그 한 곳만 고친다.
- 새 문서를 만들면, 매 세션 필요한 것은 "상시 로드"에 `@경로` 로, 특정 작업 때만 필요한 것은 "온디맨드 참조"에 등록한다.

## 상시 로드 (매 세션 적용)

- @Docs\Claude\ClaudeCodeRules.md
- @Docs\Claude\WorkflowRules.md
- @Docs\TechSpec.md

## 온디맨드 참조 (매 세션 읽지 않음 - 해당 작업을 할 때만 읽음)

### 소스코드 작성 시

- @Docs\Code\CodeConvention.md
- @Docs\Code\FolderStructure.md

### Github Issue 관련

- 이슈 등록 권유 / 작성은 `/github-issue` 스킬을 호출해 수행한다. (규칙 원문은 스킬이 안내하는 문서에서 읽는다)

### Github PR 관련

- PR 작성은 `/github-pr` 스킬을 호출해 수행한다. (규칙 원문은 스킬이 안내하는 문서에서 읽는다)

### Github Branch 관련

- 브랜치 규칙은 `Docs/Documentation/GithubBranchRule.md`를 참고한다.