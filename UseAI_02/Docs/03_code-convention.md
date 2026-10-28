# 03. C# 코드 컨벤션

## 1. 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 / enum | PascalCase | `PlayerController`, `EnemyState` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 메서드 | PascalCase, 동사로 시작 | `TakeDamage()`, `SpawnEnemy()` |
| public 프로퍼티 | PascalCase | `CurrentHp` |
| private 필드 | `_` + camelCase | `_moveSpeed`, `_rigidbody` |
| 지역 변수 / 매개변수 | camelCase | `targetPos`, `damage` |
| 상수 (`const`) | PascalCase | `MaxHp` |
| bool | `is / has / can` 접두사 | `_isGrounded`, `CanJump` |
| 이벤트 / 콜백 | `On` + 과거형·동사 | `OnDied`, `OnHpChanged` |
| enum 값 | PascalCase | `EnemyState.Chase` |

- 축약어는 피한다. (`plr` ✕ → `player` ○), 단 `Hp`, `UI`, `Id` 같은 관용어는 허용.
- 스크립트 파일명 = 클래스명. **한 파일에 public 클래스 하나.**

## 2. Inspector 노출
- 필드는 `public`으로 열지 않는다. Inspector 노출은 `[SerializeField] private`을 사용한다.
- 외부에서 읽어야 하면 프로퍼티로 제공한다.

```csharp
[SerializeField] private float _moveSpeed = 5f;
public float MoveSpeed => _moveSpeed;
```

- 관련 필드는 `[Header("이동")]`으로 묶고, 의미가 애매한 값은 `[Tooltip]`을 단다.

## 3. 클래스 내부 순서
1. 상수 / static 필드
2. `[SerializeField]` 필드 (Header로 그룹)
3. private 필드
4. 프로퍼티
5. 이벤트
6. Unity 생명주기 메서드 (`Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → OnDestroy` 순)
7. public 메서드
8. private 메서드

## 4. 포맷
- 중괄호는 새 줄에서 시작 (Visual Studio 기본, Allman 스타일).
- 한 줄짜리 `if`도 중괄호를 생략하지 않는다.
- 들여쓰기 공백 4칸.

## 5. Unity 작성 원칙
- `GetComponent`, `Find`류는 `Awake`/`Start`에서 한 번만 호출해 캐싱한다. `Update` 안에서 호출하지 않는다.
- `GameObject.Find("이름")`, 문자열 태그 비교는 가급적 피하고 Inspector 참조나 `CompareTag()`를 사용한다.
- 물리 처리는 `FixedUpdate`, 입력·일반 로직은 `Update`에서 한다.
- 매직 넘버 금지 — 수치는 `[SerializeField]` 또는 상수로 뺀다.
- 밸런스 데이터(체력, 공격력, 웨이브 정보 등)는 `ScriptableObject`로 분리하는 것을 우선 고려한다.
- 자주 생성/파괴되는 오브젝트(총알, 이펙트)는 오브젝트 풀링을 고려한다.
- 싱글톤은 꼭 필요한 매니저(GameManager 등)에만 사용한다.
- 스크립트 간 결합은 직접 참조보다 이벤트(`event Action`)나 인터페이스를 우선한다.

## 6. 주석
- 주석은 **한국어**로 작성한다.
- "무엇을"보다 **"왜"** 이렇게 했는지 적는다.
- public 메서드에는 `/// <summary>` XML 주석을 단다.
- 임시 코드는 `// TODO:`, 알려진 문제는 `// FIXME:`로 표시한다.
