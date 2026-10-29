# 03. C# 코드 컨벤션

> 기준: 실제 작성 코드(팀 프로젝트 `jatdummy`, 10월 수업 실습)에서 굳어진 습관을 반영했다.

## 1. 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 | PascalCase | `PlayerController`, `GoldManager` |
| enum 타입 | `E` + PascalCase | `ETowerType`, `ETileType` |
| enum 값 | PascalCase | `ETowerType.ArrowTower` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 메서드 | PascalCase, 동사로 시작 | `TakeDamage()`, `InstallTower()` |
| public 프로퍼티 | PascalCase | `Gold`, `IsTopView` |
| private 필드 | `_` + camelCase | `_moveSpeed`, `_selectedTile` |
| 지역 변수 / 매개변수 | camelCase | `targetPos`, `amount` |
| 상수 (`const`, `static readonly`) | PascalCase | `StartGold`, `MaxTowerLevel` |
| bool | `is / has / can` 접두사 | `_isTopView`, `CanJump` |
| 이벤트 / 콜백 | `On` + 과거형·동사 | `OnGoldChanged`, `OnTileSelected` |

- 축약어는 피한다. (`plr` ✕ → `player` ○), 단 `Hp`, `UI`, `Id` 같은 관용어는 허용.
- 스크립트 파일명 = 클래스명. **한 파일에 public 클래스 하나.**
- 식별자 철자에 주의한다. (`SkillAblity` ✕ → `SkillAbility` ○) 이름은 한 번 퍼지면 고치기 어렵다.

## 2. enum
- 모든 enum은 `Assets/Project/Scripts/Core/EnumList.cs` **한 파일에 모은다.** 어디서 쓰든 찾는 위치가 하나로 고정된다.
- enum 타입 위에만 `/// <summary>`로 용도를 적는다. 값마다 달 필요는 없다.
- "없음"이 필요하면 `None = -1`로 둔다.

```csharp
/// <summary> 타워 타입 </summary>
public enum ETowerType
{
    None = -1,
    ArrowTower = 0,
    FireTower = 1,
    IceTower = 2,
}
```

## 3. Inspector 노출
- 필드는 `public`으로 열지 않는다. Inspector 노출은 `[SerializeField] private`을 사용한다.
- 외부에서 읽어야 하면 프로퍼티로 제공한다.
- 값의 의미는 **줄 끝 주석**으로 적는다. `[Tooltip]`은 Inspector에서 꼭 봐야 하는 값에만 선택적으로 쓴다.

```csharp
[SerializeField] private float _panSpeed = 15f;     // 기본 이동 속도
[SerializeField] private float _smoothSpeed = 10f;  // 클수록 딱딱, 작을수록 부드럽게

public float PanSpeed => _panSpeed;
```

- 관련 필드가 많으면 `[Header("이동")]`으로 묶는다.

## 4. 클래스 내부 순서
1. 상수 / static 필드
2. `[SerializeField]` 필드 (Header로 그룹)
3. private 필드
4. 프로퍼티
5. 이벤트
6. Unity 생명주기 메서드 (`Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → OnDestroy` 순)
7. public 메서드
8. private 메서드

- 필드는 클래스 위쪽에 모은다. 메서드 사이에 필드를 선언하지 않는다.

## 5. 생명주기 메서드
- `Awake`, `Start`, `Update` 등은 **`private`**으로 선언한다. 외부에서 호출할 일이 없고, `public`이면 다른 스크립트가 실수로 부를 수 있다.
- 하는 일이 메서드 하나로 위임될 때는 `=>` 한 줄로 쓴다. 생명주기마다 무슨 일을 하는지 한눈에 보인다.

```csharp
private void Awake() => CacheComponents();
private void OnEnable() => BindInputEvents();
private void Update() => MoveToGoal();
```

## 6. 포맷
- 중괄호는 새 줄에서 시작 (Allman 스타일).
- **가독성이 유지되면 중괄호를 생략해도 된다.** 기준:
  - ○ 조기 반환·건너뛰기: `if (!_isPlaying) return;`, `if (tile == null) continue;`
  - ○ 본문이 한 줄이고 바로 아래에 다른 `if`/`else`가 이어지지 않는 경우
  - ✕ `if`–`else`가 섞이거나 본문이 두 줄 이상이면 중괄호를 쓴다.
- 들여쓰기 공백 4칸.
- 타입이 분명할 때는 `new()`를 써도 된다. (`private List<Monster> _monsters = new();`) `var`는 쓰지 않는다.

## 7. 파일 인코딩
- 모든 `.cs` 파일은 **UTF-8**로 저장한다.
- EUC-KR(CP949)로 저장되면 GitHub와 다른 PC에서 한글 주석이 `�⺻ �̵�`처럼 깨진다.
- Visual Studio: `파일 > 다른 이름으로 저장 > 인코딩하여 저장 > 유니코드(UTF-8 서명 있음)`
  또는 프로젝트 루트에 `.editorconfig`를 두고 `charset = utf-8`을 지정한다.

## 8. Unity 작성 원칙
- `GetComponent`, `Find`류는 `Awake`/`Start`에서 한 번만 호출해 캐싱한다. `Update` 안에서 호출하지 않는다.
- `GameObject.Find("이름")`, 문자열 태그 비교는 가급적 피하고 Inspector 참조나 `CompareTag()`를 사용한다.
- 입력은 `Docs/TechSpec.md`의 Input System 규칙을 따른다. (`InputActionReference`, `OnEnable`/`OnDisable`에서 Enable/Disable)
- 물리 처리는 `FixedUpdate`, 입력 값 읽기·일반 로직은 `Update`에서 한다.
- 매직 넘버 금지 — 수치는 `[SerializeField]` 또는 상수로 뺀다.
- 밸런스 데이터(체력, 공격력, 웨이브 정보 등)는 `ScriptableObject`로 분리하는 것을 우선 고려한다.
- 자주 생성/파괴되는 오브젝트(총알, 이펙트)는 오브젝트 풀링을 고려한다.
- 싱글톤은 꼭 필요한 매니저(GameManager 등)에만 사용한다.
- 스크립트 간 결합은 직접 참조보다 이벤트(`event Action`)나 인터페이스를 우선한다.

## 9. 주석
- 주석은 **한국어**로 작성한다.
- 메서드 위에는 **한 줄로 역할**을 적는다. (`// 타워 강화`, `// 탑뷰 카메라가 맵 밖으로 못 나가게`)
- 코드만 봐서 이유를 알기 어려운 곳에는 **"왜"**를 덧붙인다.
- 다른 사람이 호출할 public 메서드는 사용법을 남긴다. (`// GoldManager.Instance.AddGold() 로 값 넣으면 됨`)
- 표시용 주석:
  - `// TODO:` 나중에 할 일
  - `// FIXME:` 알려진 문제
  - `// 테스트 로그` 지워야 할 `Debug.Log`
