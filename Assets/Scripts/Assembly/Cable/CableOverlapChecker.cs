using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CableOverlapChecker : MonoBehaviour
{
    public static CableOverlapChecker Instance { get; private set; }

    [SerializeField] private TMP_Text warningText;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private LayerMask checkMask;

    public bool IsBlocked { get; private set; }

    private readonly HashSet<GameObject> conflictParts = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (warningPanel != null) warningPanel.SetActive(false);
        else if (warningText != null) warningText.gameObject.SetActive(false);
    }

    // 새로 장착된 부품 하나만 케이블들과 검사
    public void RunCheckForPart(GameObject part)
    {
        if (part == null) return;

        bool overlap = false;

        foreach (var cable in FindObjectsOfType<CableComponent>())
        {
            if (!cable.IsInitialized) continue;
            float r = cable.CollisionRadius;

            for (int i = 1; i < cable.Segments; i++)
            {
                Vector3 p = cable.GetParticle(i);
                Collider[] hits = Physics.OverlapSphere(p, r, checkMask, QueryTriggerInteraction.Ignore);
                foreach (var col in hits)
                {
                    GameObject root = FindPartRoot(col.gameObject);
                    if (root != part) continue;
                    if (conflictParts.Add(part))
                        Debug.LogWarning($"[CableOverlap] '{cable.gameObject.name}' particle[{i}] overlaps '{part.name}' (collider: {col.gameObject.name})");
                    overlap = true;
                }
                if (overlap) break;
            }
            if (overlap) break;
        }

        UpdateBlockState();
    }

    // 케이블 연결 완료 시 해당 케이블을 모든 장착 부품과 검사
    public void RunCheckForCable(CableComponent cable)
    {
        if (cable == null || !cable.IsInitialized) return;

        float r = cable.CollisionRadius;

        for (int i = 1; i < cable.Segments; i++)
        {
            Vector3 p = cable.GetParticle(i);
            Collider[] hits = Physics.OverlapSphere(p, r, checkMask, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                GameObject root = FindPartRoot(col.gameObject);
                if (root != null && conflictParts.Add(root))
                    Debug.LogWarning($"[CableOverlap] '{cable.gameObject.name}' particle[{i}] overlaps '{root.name}' (collider: {col.gameObject.name})");
            }
        }

        UpdateBlockState();
    }

    // 부품 해체 시 충돌 목록에서 제거
    public void OnPartDetached(GameObject part)
    {
        if (part == null) return;
        conflictParts.Remove(part);
        UpdateBlockState();
    }

    public bool IsConflictPart(GameObject part) => conflictParts.Contains(part);

    void UpdateBlockState()
    {
        IsBlocked = conflictParts.Count > 0;

        if (warningPanel != null)
            warningPanel.SetActive(IsBlocked);

        if (warningText != null)
        {
            if (!IsBlocked) warningText.gameObject.SetActive(false);
            else
            {
                warningText.gameObject.SetActive(true);
                warningText.text = "Cable overlap detected.\nPlease remove the conflicting part.";
            }
        }
    }

    GameObject FindPartRoot(GameObject obj)
    {
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.GetComponent<PartInfo>() != null) return t.gameObject;
            t = t.parent;
        }
        return null;
    }
}
