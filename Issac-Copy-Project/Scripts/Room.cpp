#include "Include.h"

Room room;

Room::Room()
{
	roomIdCounter = 0;					// 다음 방의 ID
	currentRoomId = 0;					// 현재 방의 ID
}
Room::~Room()
{
}

const DoorInfo doorInfoTable[4] = {
	{ 580, 120,  90, 40, "./resource/isaac/door/door_open.png", "./resource/isaac/door/door_close.png",590, 160, 70, 40 },   // UP
	{ 580, 680,  90, 40, "./resource/isaac/door/door_open.png", "./resource/isaac/door/door_close.png",590, 640, 70, 40 },    // DOWN
	{ 0,   390,  60, 80, "./resource/isaac/door/door_open.png", "./resource/isaac/door/door_close.png" ,60, 390, 60, 80},   // LEFT
	{ 1190,390,  60, 80, "./resource/isaac/door/door_open.png", "./resource/isaac/door/door_close.png" ,1130, 390, 60, 80}   // RIGHT

};

void Room::Init()
{
	// 랜덤 시드 설정
	srand((unsigned int)time(NULL));
	// 기존 방 초기화
	rooms.clear();
	positionMap.clear();
	roomIdCounter = 0;
	currentRoomId = 0;
	colliderManager.Clear();
	doorManager.Clear();

	// 방향 벡터 : 상하좌우
	RoomVec directions[4] = {
		{0,-1},
		{0,1},
		{-1,0},
		{1,0}
	};

	// 시작방 설정
	RoomInfo startRoom;
	startRoom.id = roomIdCounter++;
	startRoom.pos = { 0,0 };		// 시작룸 좌표
	for (int i = 0; i < 4; ++i)startRoom.adjacent[i] = -1;	// 4방향 초기화
	startRoom.type = StartRoom;
	startRoom.roomImg.Create("./resource/Img/room/startRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
	rooms[startRoom.id] = startRoom;
	positionMap[startRoom.pos] = startRoom.id;

	// 일반 방 이미지 번호 섞기
	int img[4] = { 1,2,3,4 };
	for (int i = 0; i < 4; i++) {
		int r = i + rand() % (4 - i);
		std::swap(img[i], img[r]);
	}

	std::vector<int> normalRoomIds;		// 일반 방 ID 저장
	std::vector<int> candidateParents;	// 방을 확장할 후보 부모 방 리스트
	candidateParents.push_back(0);		// 시작방부터 확장
	int normalCount = 0;				// 일반방 카운트
	int maxRooms = 8;					// 총 생성할 방 수

	// 방 랜덤 생성 루프
	while (roomIdCounter < maxRooms && !candidateParents.empty()) {
		// 시작룸부터 시작해서 생성된 방을 다시 부모방으로 설정후 검사 및 방 생성
		int parentId = candidateParents.front();
		candidateParents.erase(candidateParents.begin());
		RoomInfo& parent = rooms[parentId];

		// 방향 순서 랜덤 섞기
		std::vector<int> dirOrder = { 0,1,2,3 };
		for (int i = 0; i < 4; ++i)
		{
			int r = i + rand() % (4 - i);
			std::swap(dirOrder[i], dirOrder[r]);
		}

		// 연결 할 방 수 ( 1~4개중 랜덤 )
		int connections = 1 + rand() % 4;
		int connected = 0;

		for (int i = 0; i < 4 && roomIdCounter < maxRooms && connected < connections; ++i)
		{
			int dir = dirOrder[i];
			if (parent.adjacent[dir] != -1)continue; // 이미 연결된 방향이면 스킵

			RoomVec newPos = {
				parent.pos.x + directions[dir].x,
				parent.pos.y + directions[dir].y
			};

			// 이미 방이 있으면 스킵
			if (positionMap.find(newPos) != positionMap.end()) continue;
	
			// 새로운 위치 주변에 방이 2개 이상이면 스킵
			int neighborCount = 0;
			for (int d = 0; d < 4; ++d) {
				RoomVec neighborPos = {
					newPos.x + directions[d].x,
					newPos.y + directions[d].y
				};
				if (positionMap.find(neighborPos) != positionMap.end()) {
					neighborCount++;
				}
			}
			if (neighborCount > 1) continue;

			// 새로운 방 생성
			RoomInfo room;
			room.id = roomIdCounter++;
			room.pos = newPos;
			for (int j = 0; j < 4; ++j) room.adjacent[j] = -1;
			room.type = NormalRoom;

			if (normalCount < 4) {
				char FileName[256];
				sprintf(FileName, "./resource/Img/room/room%02d.png", img[normalCount]);
				room.roomImg.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
				normalCount++;
			}

			// 부모와 서로 연결
			room.adjacent[dir ^ 1] = parentId;
			parent.adjacent[dir] = room.id;

			// 방 등록
			rooms[room.id] = room;
			positionMap[newPos] = room.id;
			candidateParents.push_back(room.id);
			normalRoomIds.push_back(room.id);
			connected++;
		}
	}

	// Dead End 탐색 (링크가 1개만 있는 방 )
	std::vector<int> deadEnds;
	for (std::map<int, RoomInfo>::iterator it = rooms.begin(); it != rooms.end(); ++it)
	{
		if (it->second.type != NormalRoom)continue;

		int links = 0;
		for (int d = 0; d < 4; ++d)
		{
			if (it->second.adjacent[d] != -1)links++;
		}
		if (links == 1)deadEnds.push_back(it->first);
	}

	// Dead End 셔플
	for (int i = 0; i < (int)deadEnds.size(); ++i)
	{
		int r = i + rand() % (deadEnds.size() - i);
		std::swap(deadEnds[i], deadEnds[r]);
	}

	// Dead End 중에서 보스 아이템 열쇠방 배치
	int specials = 0;
	if (deadEnds.size() >= 3)
	{
		rooms[deadEnds[0]].type = ItemRoom;
		rooms[deadEnds[0]].roomImg.Create("./resource/Img/room/itemRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		specials++;
		rooms[deadEnds[1]].type = BossRoom;
		rooms[deadEnds[1]].roomImg.Create("./resource/Img/room/bossRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		specials++;
		rooms[deadEnds[2]].type = KeyRoom;
		rooms[deadEnds[2]].roomImg.Create("./resource/Img/room/keyRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		specials++;
	}

	// Dead End 가 부족한 경우 일반방에서 채우기
	int remaining = 3 - specials;
	for (int i = 0; i < (int)normalRoomIds.size() && remaining > 0; ++i) {
		RoomInfo& r = rooms[normalRoomIds[i]];
		if (r.type != NormalRoom) continue;
		if (remaining == 3) {
			r.type = ItemRoom;
			r.roomImg.Create("./resource/Img/room/itemRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		}
		else if (remaining == 2) {
			r.type = BossRoom;
			r.roomImg.Create("./resource/Img/room/bossRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		}
		else if (remaining == 1) {
			r.type = KeyRoom;
			r.roomImg.Create("./resource/Img/room/keyRoom.png", false, D3DCOLOR_XRGB(0, 0, 0));
		}
		--remaining;
	}

	// 일반 방에 몬스터 타입 배정
	int assigned = 0;
	for (int i = 0; i < normalRoomIds.size(); i++)
	{
		RoomInfo& r = rooms[normalRoomIds[i]];
		if (r.type == NormalRoom && assigned < 4)
		{
			r.monsterType = assigned;
			assigned++;
		}
	}
	CreateDoors();

	bossLoading.Create("./resource/Img/room/bossLoading.png", true, D3DCOLOR_XRGB(255, 0, 255));
}
// 다음 방으로 넘어갈때 호출
void Room::EnterRoom(int roomId)
{
	currentRoomId = roomId;
	itemManager.SetCurrentRoom(roomId);
	// 각 룸타입에 아이템 배치
	if (itemManager.roomItems.find(roomId) == itemManager.roomItems.end()) {
		if (rooms[roomId].type == ItemRoom) {
			Item* coin = new CoinItem(300, 300);
			itemManager.Add(roomId, coin);
			Item* tear = new TearItem(400, 400);
			itemManager.Add(roomId, tear);

			Item* life = new LifeItem(500, 400);
			itemManager.Add(roomId, life);

			Item* key = new KeyItem(600, 400);
			itemManager.Add(roomId, key);

			Item* bomb = new BombItem(700, 400);
			itemManager.Add(roomId, bomb);

			Item* speed = new SpeedItem(300, 500);
			itemManager.Add(roomId, speed);

			Item* attack = new AttackItem(400, 500);
			itemManager.Add(roomId, attack);

			Item* heart = new HeartItem(500, 500);
			itemManager.Add(roomId, heart);

			obstacle.chest.AddChest(800, 500, 50, 50);
			rooms[roomId].roomObstacle = obstacle;
		}
	}
	if (itemManager.roomItems.find(roomId) == itemManager.roomItems.end()) {
		if (rooms[roomId].type == KeyRoom) {
			Item* bomb = new BombItem(400, 400);
			itemManager.Add(roomId, bomb);
		}
	}
	if (itemManager.roomItems.find(roomId) == itemManager.roomItems.end())
	{
		RoomInfo& room = rooms[roomId];
		if (room.type == NormalRoom && room.monsterType == 0)
		{
			Item* bomb = new BombItem(610, 410);
			itemManager.Add(roomId, bomb);
		}
		if (room.type == NormalRoom && room.monsterType == 2)
		{
			Item* key = new KeyItem(1070, 230);
			itemManager.Add(roomId, key);

			Item* coin1 = new CoinItem(170, 230);
			itemManager.Add(roomId, coin1);

			Item* coin2 = new CoinItem(170, 590);
			itemManager.Add(roomId, coin2);

			Item* coin3 = new CoinItem(1070, 590);
			itemManager.Add(roomId, coin3);
		}
	}
	RoomInfo& room = rooms[roomId];
	RoomType type = rooms[roomId].type;
	// 몬스터 및 장애물 초기화
	monstermanager.ClearAll();
	if (type != ItemRoom)
		obstacle.chest.Clear();
	if (type == StartRoom || type == ItemRoom || type == KeyRoom) {
		doorManager.OpenAllDoors();
		obstacle.Clear();
		// 현재 장애물 상태 저장
		room.roomObstacle = obstacle;
		return;
	}

	if (type == BossRoom)
	{
		if (!room.cleared)
		{
			obstacle.Clear();
			isBossLoading = true;
			startLoading = GetTickCount();
			bossMaking = true;

			return;
		}
		else
		{
			doorManager.OpenAllDoors();
		}
		return;
	}
	// 몬스터가 모두 처치되면 문 열림
	if (room.cleared)
	{
		monstermanager.ClearAll();
		doorManager.OpenAllDoors();
		obstacle = room.roomObstacle;
	}
	else
	{// 방에 처음 방문시
		if (!room.isVisited)
		{// 각 몬스터 및 장애물 타입별로 설치
			if (room.monsterType != -1) {
				monstermanager.AssignMonstersToRoom(room.monsterType);
				obstacle.AssignObstaclesToRoom(room.monsterType);
				room.roomObstacle = obstacle;
			}
			room.isVisited = true;
		}
		else {
			monstermanager.AssignMonstersToRoom(room.monsterType);
		}
		// 저장된 장애물 상태 복구
		obstacle = room.roomObstacle;
		doorManager.ClosedAllDoors();
	}
	
}

// 문 생성 함수
void Room::CreateDoors()
{
	RoomInfo& currentRoom = rooms[currentRoomId];

	doorManager.Clear();


	for (int dir = 0; dir < 4; dir++)
	{	// 방향 검사후 연결된 방이 없으면 스킵
		int nextRoomId = currentRoom.adjacent[dir];
		if (nextRoomId == -1)continue;

		RoomInfo& nextRoom = rooms[nextRoomId];
		RoomType doorType = NormalRoom;
		// 각 룸타입별 문 모양
		if (nextRoom.type == ItemRoom) {
			doorType = ItemRoom;
		}
		else if (nextRoom.type == BossRoom) {
			doorType = BossRoom;
		}
		else if (nextRoom.type == KeyRoom) {
			doorType = KeyRoom;
		}
		else if (currentRoom.type != NormalRoom) {
			doorType = currentRoom.type;
		}

		const DoorInfo& Info = doorInfoTable[dir];

		const char* closeImg = Info.closedImg;
		const char* openImg = Info.openImg;

		switch (doorType) {
		case ItemRoom:
			openImg = "./resource/isaac/door/door_item_open.png";
			closeImg = "./resource/isaac/door/door_item_close.png";
			break;
		case BossRoom:
			openImg = "./resource/isaac/door/door_boss_open.png";
			closeImg = "./resource/isaac/door/door_boss_close.png";
			break;
		case KeyRoom:
			openImg = "./resource/isaac/door/door_key_open.png";
			closeImg = "./resource/isaac/door/door_key_close.png";
			break;
		default:
			break;
		}

		doorManager.AddDoor(
			(DoorDirection)dir,
			Info.x, Info.y,
			Info.w, Info.h,
			openImg, closeImg
		);
	}
}

void Room::Update()
{
	if (isBossLoading && bossMaking)
	{// 보스방 입장시 로딩창
		DWORD now = GetTickCount();

		if (now - startLoading >= 2000)
		{
			monstermanager.MakeBoss();
			doorManager.ClosedAllDoors();

			isBossLoading = false;
			bossMaking = false;
		}
		return;
	}

	RoomInfo& currentRoom = rooms[currentRoomId];
	currentRoom.roomObstacle = obstacle;	// 현재 방의 장애물 상태 저장
	if (!currentRoom.cleared && monstermanager.monster.empty() && monstermanager.IsAllMonstersDead())
	{	// 몬스터 클리어시 문 열리게
		currentRoom.cleared = true;
		doorManager.OpenAllDoors();

		if (isaac.heartGauge > 6)
			isaac.heartGauge = 6;
		else if (currentRoom.type != KeyRoom)
		{	// 몬스터 클리어시 아이템 게이지 1 상승
			if (isaac.heartGauge < 6)
				isaac.heartGauge += 1;
		}
	}
}

void Room::Draw()
{
	if (isBossLoading)
	{
		bossLoading.Render(0, 0, 0, 1.0f, 1.0f);
		return;
	}

	rooms[currentRoomId].roomImg.Render(0, 120, 0, 1, 1);
	doorManager.Draw();
	obstacle.Draw();
	itemManager.Draw();

}

void Room::MoveToDirection(RoomDirection dir)
{// 문 이동 체크
	int nextRoom = rooms[currentRoomId].adjacent[dir];

	if (nextRoom != -1)
	{
		currentRoomId = nextRoom;

		doorManager.Clear();
		CreateDoors();

		EnterRoom(currentRoomId);
	}
}

bool Room::IsRoomCleared(int roomId)
{
	auto it = roomClearStatus.find(roomId);
	return it != roomClearStatus.end() && it->second;
}
void Room::SetRoomCleared(int roomId)
{
	roomClearStatus[roomId] = true;
	OpenDoors(roomId);
}
void Room::OpenDoors(int roomId)
{
	doorManager.OpenAllDoors();
}
