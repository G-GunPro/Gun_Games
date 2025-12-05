#pragma once
#include "DoorManager.h"
#include "Door.h"
#include "ColliderManager.h"
#include "Monster.h"
#include <string>
#include <map>
#include <vector>

enum RoomDirection { r_Up = 0, r_Down = 1, r_Left = 2, r_Right = 3 };

struct RoomVec {
	int x, y;
	bool operator<(const RoomVec& other) const {
		return (x == other.x) ? (y < other.y) : (x < other.x);
	}
	bool operator==(const RoomVec& other) const {
		return (x == other.x) ? (y < other.y) : (x < other.x);
	}
};

enum RoomType {
	StartRoom,
	BossRoom,
	ItemRoom,
	KeyRoom,
	NormalRoom
};

struct RoomInfo {
	int id;								// 룸 아이디
	RoomVec pos;						// 룸 생성 좌표
	int adjacent[4];					// 인접검사용 인덱스
	RoomType type;
	Sprite roomImg;
	std::vector<Monster*> monsters;
	bool cleared = false;				// 클리어 여부
	int monsterType = -1;
	bool isVisited = false;
	Obstacle roomObstacle;
};

class Room
{
	std::map<RoomVec, int> positionMap;
	int currentRoomId = 0;
	int roomIdCounter = 0;
	std::map<int, bool>roomClearStatus;
public:
	Room();
	~Room();

	std::map<int, RoomInfo>rooms;

	Sprite bossLoading;

	bool isBossLoading = false;
	bool bossMaking = false;

	DWORD startLoading = 0;

	DoorManager doorManager;
	void Init();
	void Update();
	void Draw();
	void MoveToDirection(RoomDirection dir);				// 룸 이동 검사
	int GetCurrentRoomId() const { return currentRoomId; }
	void CreateDoors();										// 문 생성
	void EnterRoom(int roomId);								// 룸 이동시 오브젝트 관리
	bool IsRoomCleared(int roomId);							// 각 룸의 클리어 여부 저장
	void SetRoomCleared(int roomId);						// 클리어 여부에 따라 문 열고닫기
	void OpenDoors(int roomId);	
};

extern Room room;