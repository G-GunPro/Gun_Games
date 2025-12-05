#pragma once
#include <vector>
#include "Door.h"

class DoorManager 
{
	std::vector<Door> doors;	/// 벡터로 문들 관리

public:
	DoorManager();
	~DoorManager();

	DoorDirection checkEnter(const RECT& playerRc);	// 충돌 체크 및 방향

	int GetCount() const { return (int)doors.size(); }	// 문 개수

	void Clear();	// 초기화
	void AddDoor(DoorDirection dir, int x, int y, int w, int h, const char* openImg, const char* closedImg);	// 문 추가
	void Draw();
	void ToggleAll();	// 열림->닫힘 & 닫힘->열림

	// 강제 오픈 및 클로즈
	void OpenAllDoors();
	void ClosedAllDoors();

	const std::vector<Door>& GetDoors() const;	// 전체 문들
};
extern DoorManager doorManager;