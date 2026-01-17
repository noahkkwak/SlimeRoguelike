using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 리스트 필터링을 위해 필요

[System.Serializable]
public struct EnemySpawnRule
{
    public string ruleName; // 에디터 식별용 (예: "Goblin 1-3")
    public GameObject enemyPrefab; // 데이터가 연결된 완성형 프리팹
    public int minStage; // 등장 시작 단계
    public int maxStage; // 등장 종료 단계 (0이면 무제한)
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    [Header("Game Progress")]
    public int currentStage = 1; // 현재 진행 단계 (Depth)

    [Header("Spawn Rules")]
    public List<EnemySpawnRule> enemySpawnRules; // 에디터에서 설정

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject); // 씬이 넘어가도 유지되도록 설정 (선택사항)
    }

    // 현재 스테이지에 등장 가능한 적 프리팹 리스트 반환
    public GameObject GetRandomEnemyPrefab()
    {
        // 조건에 맞는 적들만 필터링
        List<GameObject> validEnemies = new List<GameObject>();

        foreach (var rule in enemySpawnRules)
        {
            bool isMinOk = currentStage >= rule.minStage;
            bool isMaxOk = (rule.maxStage == 0) || (currentStage <= rule.maxStage);

            if (isMinOk && isMaxOk)
            {
                validEnemies.Add(rule.enemyPrefab);
            }
        }

        if (validEnemies.Count == 0)
        {
            Debug.LogError($"Stage {currentStage}에 등장 가능한 적이 없습니다! 규칙을 확인하세요.");
            return null;
        }

        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    // 스테이지 클리어 시 호출 (다음 단계로)
    public void NextStage()
    {
        currentStage++;
        Debug.Log($"<color=cyan>--- 스테이지 {currentStage} 진입 ---</color>");
        // 씬 재로드 혹은 적 재소환 로직 연결
    }
}