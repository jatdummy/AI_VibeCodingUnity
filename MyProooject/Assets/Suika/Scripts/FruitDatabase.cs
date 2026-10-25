using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 과일 한 단계의 정보. 단계(level)는 배열 인덱스와 같다.
    /// </summary>
    [System.Serializable]
    public class FruitDef
    {
        public string displayName;
        [Tooltip("월드 단위 반지름 (콜라이더 기준)")]
        public float radius = 0.5f;
        public Color color = Color.white;
        [Tooltip("이 과일이 합쳐져서 만들어질 때 얻는 점수")]
        public int score;
        public Sprite sprite;
    }

    /// <summary>
    /// 과일 진화 순서 전체를 담는 데이터. 작은 과일 → 큰 과일 순서.
    /// </summary>
    [CreateAssetMenu(menuName = "Suika/Fruit Database", fileName = "FruitDatabase")]
    public class FruitDatabase : ScriptableObject
    {
        public FruitDef[] fruits;

        [Tooltip("떨어뜨리는 과일로 나올 수 있는 단계 수 (0단계부터 이 개수만큼)")]
        public int spawnableCount = 5;

        public int Count => fruits == null ? 0 : fruits.Length;

        public FruitDef Get(int level) => fruits[Mathf.Clamp(level, 0, Count - 1)];

        public bool IsMaxLevel(int level) => level >= Count - 1;
    }
}
