# 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/            # GameManager, 공용 유틸, EnumList.cs
│   ├── Player/
│   ├── Enemy/
│   ├── UI/
│   └── Data/            # ScriptableObject 정의
├── Input/               # Input Action 에셋 (.inputactions)
├── Data/                # ScriptableObject 에셋(.asset)
├── Prefabs/
│   └── Player/          # 기능별 하위 폴더
├── Scenes/
│   └── Test/            # 기능 테스트용 씬
├── Materials/
├── Animations/
├── Audio/
├── Models/
├── Plugins/
└── Imports/             # 외부 에셋 스토어 에셋은 전부 여기 (직접 만든 에셋과 분리)
```

## 규칙
- 직접 만든 에셋은 `Assets/` 바로 아래의 용도별 폴더에 둔다.
- 에셋 스토어 등 외부 에셋은 모두 `Assets/Imports/` 아래에 둔다. 직접 만든 에셋과 섞이지 않게 하기 위해서다.
- 새 스크립트는 기능별 폴더에 넣는다. 폴더가 애매하면 AI가 위치를 제안한다.
- Input Action 에셋은 `Assets/Input/`에 둔다. (`TechSpec.md` — new Input System 사용)
- 기능 테스트는 `Assets/Scenes/Test/`의 테스트 씬에서 한다. (메인 씬 충돌·오염 방지)
- 폴더 이름은 PascalCase, 영어로 짓는다.
- 에셋·폴더를 옮기거나 이름을 바꿀 때는 **Unity 에디터 안에서** 한다. 탐색기에서 옮기면 `.meta`가 따라가지 않아 참조가 끊어진다.
