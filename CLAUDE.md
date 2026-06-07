# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 project. PC 조립 시뮬레이터 + AI 견적 추천 기능.
- **조립 씬**: 파츠 선택 → 슬롯 장착 → 케이블 연결
- **견적 씬**: Gemini API로 예산/목적 기반 PC 추천

## Build / Development

Unity Editor에서만 빌드 가능. CLI 빌드 명령 없음.

- Unity 버전: `ProjectSettings/ProjectVersion.txt` 참조
- 스크립트 위치: `Assets/Scripts/`
- 프리팹/씬: `Assets/Prefabs/`, `Assets/Scenes/`
- 케이블 Editor 도구: Unity 메뉴 `Tools > Cable` (CableBuilder, CableSetup, FixAssembledColliders)
- GeminiClient API 키: Inspector에서 직접 입력 (코드에 하드코딩 금지)

## Architecture

### 조립 시스템

**파츠 흐름**: `PartSelector` (UI 버튼) → `PartSelectionManager` (정적 상태) → `SnapZone` (월드 슬롯)

- `PartSelector`: UI 버튼에 붙임. 클릭 시 `PartSelectionManager.SelectedPart` 설정
- `PartSelectionManager`: 선택 상태 전역 관리 (static). `Clear()` 호출 시 모든 SnapZone 숨김
- `SnapZone`: 월드에 배치된 슬롯. `acceptType`과 선택된 파츠의 `PartData.partType`이 일치해야 장착
- `PartInfo` + `PartData` (ScriptableObject): 파츠 종류 식별. `PartType` enum 기준

**레이어 규칙**:
- 장착 전: `Default` 레이어 (레이캐스트 대상)
- 장착 후: `AssembledPart` 레이어 (클릭 차단, 물리 충돌 유지)
- 케이블: `Cable` 레이어

**파츠 제거**: `R` 키 + 장착된 파츠 위 마우스 클릭

### 케이블 시스템

**상태 머신** (`CableManager`): `Idle → TypeSelected → Routing → Idle`

1. `CableSpawner` UI 버튼 클릭 → `CableManager.SelectCableType()`
2. 출발 소켓(`IsSource=true`) 클릭 → 케이블 프리팹 스폰, `Routing` 상태 진입
3. 도착 소켓(`IsSource=false`) 클릭 → `CableComponent.SetEndAnchor()`, 완료

- `CableComponent`: Verlet 물리 시뮬레이션. `segments` 파티클 배열. `pins` 딕셔너리로 고정점 관리
- `CableConnector`: 케이블 프리팹의 양 끝점 컴포넌트. `IsEndPoint`로 start/end 구분
- `CableSocket`: 파츠에 붙이는 소켓. `AnchorTransform => transform` (소켓 자체 위치가 앵커)
- `CablePassThrough`: 클릭 시 케이블 중간 통과점 추가

**케이블 타입** (`CableType` enum): `ATX24Pin, CPU8Pin, PCIe8Pin, FanHeader, PWRSW, RESET, PLED, HDD_LED, FrontUSB3`

소켓-케이블 타입이 반드시 일치해야 연결 가능. 출발→도착 방향 강제 (`IsSource` 순서).

### 충돌 검증 시스템

`CableOverlapChecker` (싱글톤): 케이블-파츠 충돌 감지 시 `IsBlocked = true` → 모든 장착/케이블 연결 동작 차단.

- 파츠 장착 시: `RunCheckForPart()` 호출
- 케이블 연결 완료 시: `RunCheckForCable()` 호출
- 파츠 해체 시: `OnPartDetached()` 호출
- 충돌 부품만 해체 가능 (`IsConflictPart()` 확인)

충돌 판정은 `PartInfo` 컴포넌트 기준으로 루트 GameObject를 식별.

### AI 견적 시스템

`PCRecommendationManager` → `GeminiClient` → Gemini REST API (`gemini-2.5-flash`)

- 1차 요청: 예산/목적 입력 → CPU/GPU/RAM 각 3개 추천 + JSON 파싱으로 드롭다운 자동 설정
- 2차 요청: 드롭다운 선택값 → 전체 부품 최종 견적
- 응답 JSON 파싱: `JsonUtility` 사용 (응답 마지막 `{...}` 블록 추출)

## 핵심 싱글톤

| 클래스 | 역할 |
|--------|------|
| `CableManager` | 케이블 연결 상태 머신 |
| `CableOverlapChecker` | 충돌 감지 및 동작 차단 |

두 싱글톤 모두 씬에 하나만 존재해야 함. `Awake()`에서 중복 시 `Destroy(gameObject)`.

## 주의사항

- `SnapZone.SetLayerRecursively()`는 `SnapZone` 컴포넌트가 붙은 오브젝트는 레이어 변경 건너뜀 (슬롯 자체 레이어 보존)
- 비볼록(non-convex) `MeshCollider`는 케이블 충돌 계산에서 제외 (Unity 제약)
- `CableComponent` 거리 제약: 늘어남만 차단, 압축은 허용 (자연스러운 처짐 유지)
