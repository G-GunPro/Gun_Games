#pragma once

enum DoorDirection {	// 문 방향
	DOOR_UP=0,
	DOOR_DOWN=1,
	DOOR_LEFT=2,
	DOOR_RIGHT=3
};

struct DoorInfo	// 문 정보 구조체
{
	int x, y;	// 위치
	int w, h;	// 크기

	const char* openImg;
	const char* closedImg;

	int wallX, wallY, wallW, wallH;	// 충돌 처리용
};

class Door 
{
	DoorDirection dir;				// 문 방향

	Collider col;					// 충돌 영역

	Sprite openSprite;				// 열린 문
	Sprite closedSprite;			// 닫힌 문

	bool isOpen=false;				// 열림/닫힘 상태

	int connectedRoomId = -1;		// 연결된 방 ID

public:
	Door();
	~Door();

	DoorDirection GetDirection() const;	// 문 방향

	int GetConnectedRoom()const;

	bool IsOpen() const;	// 문 열림/닫힘 여부
	bool IsPlayerEntered(const RECT& playerRc) const;	// 아이작 충돌 여부

	void Init(DoorDirection _dir, int x, int y, int w, int h, const char* openImg, const char* closedImg);
	void ToggleOpen(DoorDirection direction);	// 문 열림/닫힘
	void Draw();
	void SetConnectRoom(int id);	// 연결 방 ID 설정
	
	// 강제 문 열림/닫힘
	void ForceOpen();
	void ForceClosed();
};

extern Door door;
extern const DoorInfo doorInfoTable[4];