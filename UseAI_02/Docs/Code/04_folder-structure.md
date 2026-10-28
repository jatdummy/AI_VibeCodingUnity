# 04. 폴더 구조

```
Assets/
├── Project/            # 내가 만든 에셋은 전부 여기 (외부 에셋과 분리)
│   ├── Scripts/
│   │   ├── Core/        # GameManager, 공용 유틸
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── UI/
│   │   └── Data/        # ScriptableObject 정의
│   ├── Data/            # ScriptableObject 에셋(.asset)
│   ├── Prefabs/
│   ├── Scenes/
│   │   └── Test/        # 기능 테스트용 씬
│   ├── Materials/
│   ├── Animations/
│   ├── Audio/
│   └── Sprites/ (또는 Models/)
├── Plugins/
└── (외부 에셋 스토어 폴더들)
```

## 규칙
- 직접 만든 에셋은 모두 `_Project/` 아래에 둔다. 앞의 `_` 덕분에 목록 맨 위에 정렬되고, 외부 에셋과 섞이지 않는다.
- 새 스크립트는 기능별 폴더에 넣는다. 폴더가 애매하면 AI가 위치를 제안한다.
- 기능 테스트는 `Scenes/Test/`의 테스트 씬에서 한다. (메인 씬 충돌·오염 방지)
- 폴더 이름은 PascalCase, 영어로 짓는다.
