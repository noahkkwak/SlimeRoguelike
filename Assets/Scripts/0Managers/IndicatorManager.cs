using UnityEngine;
using System.Collections;

public class IndicatorManager : MonoBehaviour
{
    public static IndicatorManager Instance { get; private set; }

    [SerializeField] private GameObject rangeIndicator;
    private Coroutine _checkHoldCoroutine;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(false);

        if (InputManager.Instance != null)
        {
            // [MODIFY] 누르기 시작하면 바로 띄우지 않고, 시간을 체크하는 코루틴 시작
            InputManager.Instance.OnAttackStarted += StartHoldCheck;
            // 떼면 체크 중단 및 인디케이터 숨김
            InputManager.Instance.OnAttackReleased += (duration) => HideAll();
        }
    }

    private void StartHoldCheck()
    {
        if (_checkHoldCoroutine != null) StopCoroutine(_checkHoldCoroutine);
        _checkHoldCoroutine = StartCoroutine(CheckHoldTime());
    }

    private IEnumerator CheckHoldTime()
    {
        float elapsed = 0f;
        float threshold = SkillManager.Instance.GetHoldThreshold();

        while (elapsed < threshold)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // [ADD] 설정한 시간(0.5초 등) 이상 눌렀을 때만 인디케이터 표시
        ShowSwallowRange();
    }

    private void ShowSwallowRange()
    {
        if (rangeIndicator == null || SkillManager.Instance == null) return;

        rangeIndicator.SetActive(true);
        float radius = SkillManager.Instance.GetSwallowRadius();
        rangeIndicator.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
    }

    public void HideAll()
    {
        if (_checkHoldCoroutine != null) StopCoroutine(_checkHoldCoroutine);
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }
}