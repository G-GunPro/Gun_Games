#include "Include.h"

Door door;

Door::Door()
{
}

Door::~Door()
{
}

void Door::Init(DoorDirection _dir, int x, int y, int w, int h,	const char* openImg, const char* closedImg)	// 방향, 좌표, 크기, 이미지 이름
{
	dir = _dir;	// 방향
	col.Init(x, y, w, h);	// 충돌 검사
	openSprite.Create(openImg, false, D3DCOLOR_XRGB(0, 0, 0));
	closedSprite.Create(closedImg, false, D3DCOLOR_XRGB(0, 0, 0));
}

void Door::ToggleOpen(DoorDirection direction)	// 문 열림 닫힘 상태						// 문 열림/닫힘 처리
{
	isOpen = !isOpen;

	if (!IsOpen())
		g_SoundMgr.EffectPlay(4);
	else
		g_SoundMgr.EffectPlay(5);
}

bool Door::IsOpen() const	// 문 상태 확인
{
	return isOpen;
}

bool Door::IsPlayerEntered(const RECT& playerRc) const		// 문이 닫혀 있을 때 충돌 처리
{
	if (!isOpen)
		return false;
	
	return Collider::CheckCollision(playerRc, col.m_rc);	// 문이 열려 있으면 통과
}

void Door::Draw()	// 문 이미지 출력
{
	Sprite& s = isOpen ? openSprite : closedSprite;

	switch (dir)
	{	// 방향별 문 출력
	case DOOR_LEFT:
		s.Render(57, 517, -1.58, 3.0, 2.5);
		break;
	case DOOR_RIGHT:
		s.Render(1225, 370, 1.57, 3.0, 2.5);	
		break;
	case DOOR_UP:
		s.Render(567, 162, 0, 3.0, 1.9);		
		break;
	case DOOR_DOWN:
		s.Render(710, 721, 3.13, 3.0, 1.9);	
		break;
	}
}

DoorDirection Door::GetDirection() const	// 문의 방향
{
	return dir;
}

void Door::SetConnectRoom(int id)	// 문과 연결된 방 ID
{
	connectedRoomId = id;
}

int Door::GetConnectedRoom() const	// 연결된 방 정보 저장
{ 
	return connectedRoomId; 
}

void Door::ForceOpen()	// 강제 오픈
{
	if (!isOpen)
	{
		isOpen = true;
		g_SoundMgr.EffectPlay(5);
	}
}

void Door::ForceClosed()	// 강제 클로즈
{
	if (isOpen)
	{
		isOpen = false;
		g_SoundMgr.EffectPlay(4);
	}
}