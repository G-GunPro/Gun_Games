#include "Include.h"
#include <vector>
#include "Door.h"

DoorManager doorManager;

DoorManager::DoorManager()
{
}

DoorManager::~DoorManager()
{
}

void DoorManager::Clear()	// 문 초기화
{
	doors.clear();
}

void DoorManager::AddDoor(DoorDirection dir, int x, int y, int w, int h, const char* openImg, const char* closedImg)
{	// 새 문 생성
	Door d;
	d.Init(dir, x, y, w, h, openImg, closedImg);
	doors.push_back(d);
}

void DoorManager::Draw()
{
	for (auto& d : doors)
		d.Draw();
}

void DoorManager::ToggleAll()	// 모든 문 열기
{
	for (auto& d : doors)
		d.ToggleOpen(door.GetDirection());
}

DoorDirection DoorManager::checkEnter(const RECT& playerRc)	// 충돌 체크 및 문 통과 시 위치 변경
{
	for (auto& d : doors)
	{
		if (d.IsPlayerEntered(playerRc))
			return d.GetDirection();
	}
	return (DoorDirection)-1;
}

const std::vector<Door>& DoorManager::GetDoors() const 
{	// 벡터에 문 참조
	return doors;
}

void DoorManager::OpenAllDoors()	// 강제 오픈
{
	for (auto& d : doors)
		d.ForceOpen();
}

void DoorManager::ClosedAllDoors()	// 강제 클로즈
{
	for (auto& d : doors)
		d.ForceClosed();
}