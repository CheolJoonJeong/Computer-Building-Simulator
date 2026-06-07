using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Play 모드에서 옮긴 오브젝트 위치를 기록해뒀다가, 정지 후 그대로 적용
public static class PlayModeTransformSaver
{
    private class Record
    {
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
    }

    // GlobalObjectId 문자열 -> 기록된 트랜스폼
    private static readonly Dictionary<string, Record> saved = new();

    [MenuItem("Tools/Play Mode Transforms/Record Selected")]
    private static void RecordSelected()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PlayModeTransformSaver] Play 모드에서만 기록할 수 있습니다.");
            return;
        }

        saved.Clear();
        foreach (var go in Selection.gameObjects)
        {
            string id = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            saved[id] = new Record
            {
                localPos = go.transform.localPosition,
                localRot = go.transform.localRotation,
                localScale = go.transform.localScale
            };
        }

        Debug.Log($"[PlayModeTransformSaver] {saved.Count}개 오브젝트 위치 기록 완료. Play를 정지한 뒤 'Apply Recorded'를 누르세요.");
    }

    [MenuItem("Tools/Play Mode Transforms/Apply Recorded")]
    private static void ApplyRecorded()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[PlayModeTransformSaver] Play를 정지한 뒤 적용해주세요.");
            return;
        }

        if (saved.Count == 0)
        {
            Debug.LogWarning("[PlayModeTransformSaver] 기록된 위치가 없습니다. 먼저 Play 모드에서 'Record Selected'를 실행하세요.");
            return;
        }

        int applied = 0;
        foreach (var kv in saved)
        {
            GlobalObjectId.TryParse(kv.Key, out var gid);
            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
            if (obj == null) continue;

            Undo.RecordObject(obj.transform, "Apply Recorded Play Mode Transform");
            obj.transform.localPosition = kv.Value.localPos;
            obj.transform.localRotation = kv.Value.localRot;
            obj.transform.localScale = kv.Value.localScale;
            applied++;
        }

        Debug.Log($"[PlayModeTransformSaver] {applied}개 오브젝트에 위치 적용 완료.");
        saved.Clear();
    }
}
