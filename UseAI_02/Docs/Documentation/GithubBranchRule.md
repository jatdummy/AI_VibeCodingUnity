# Github Branch 작성 규칙

> Github 브랜치를 생성하고 관리할 때의 규칙
>
> 브랜치의 생성 / 삭제는 사용자가 직접 수행한다. (`Docs/Claude/WorkflowRules.md`의 `0. 전체 흐름`)
>
> 해당 문서는 기준 브랜치와 브랜치 이름에 대한 규칙을 정의한다.

## 1. 기준 브랜치

- 작업 브랜치는 `main`에서 생성한다.
- PR 대상 브랜치는 `main`으로 한다.
- `main`, `Develop` 브랜치에서는 직접 작업하지 않는다. (`CLAUDE.md` 절대 원칙)

## 2. 이름 형식

- `[말머리]/[이슈번호]-[작업명]` 형태로 적는다.
  - 말머리 : `Docs/Documentation/GithubPRRule.md`의 `1. Title` 말머리를 소문자로 사용한다.
  - 작업명 : 영어 소문자로 적고, 단어 사이는 `-`로 구분한다.
- Issue가 없는 작업은 이슈번호를 생략한다.

- 예시 :

    ```
    feat/12-player-jump
    fix/15-key-input
    docs/pr-rule
    ```

## 3. 임시 브랜치

- 테스트용 임시 브랜치는 `test/[작업명]` 형태로 생성한다.
- 임시 브랜치는 PR 대상이 아니며, 사용이 끝나면 삭제한다.

- 예시 :

    ```
    test/skill-check
    ```
