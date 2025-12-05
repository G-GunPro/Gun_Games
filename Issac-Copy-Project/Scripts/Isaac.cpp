#include "Include.h"

Isaac isaac;

Isaac::Isaac()
{
	// 처음 출력 위치
	pos.x = 610;			
	pos.y = 390;

	direction = Down;		// 처음 출력할 방향

	// 이동값
	moveX = 0;
	moveY = 0;

	// UI Count
	coinCount = 0;
	keyCount = 0;
}

Isaac::~Isaac()
{
}

void Isaac::Init()
{
	maxHp = 6;	// 최대 체력
	hp = maxHp;	// 현재 체력

	pos.x = 610;			
	pos.y = 390;

	direction = Down;

	moveX = 0;
	moveY = 0;

	isDamaging = false;	// 무적 상태
	isDeading = false;	// 죽음 여부

	heartGauge = 6;	// 아이템 게이지
	
	// 아이템 Count
	coinCount = 0;
	bombCount = 1;
	keyCount = 0;

	threeTear = false;	// 눈물 공격 아이템
	pickSpeed = false;	// 스피드 증가 아이템
	pickAttack = false;	// 공격력 증가 아이템
	pickHeartItem = false;	// 충전 하트 아이템

	speed = 4.0f;	// 이동 속도

	char FileName[256];

	for (int i = 0; i < 4; ++i)
	{
		for (int j = 0; j < 2; ++j)
		{
			tearHeadSprites[i][j] = headSprites[i][j];
		}
	}
	for (int i = 0; i < 4; ++i) 
	{
		for (int j = 0; j < 2; ++j) 
		{
			sprintf_s(FileName, "./resource/isaac/player/head/head%02d_%02d.png", i + 1, j + 1);
			headSprites[i][j].Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
		}
	}
	for (int i = 0; i < 4; ++i)
	{
		for (int j = 0; j < 2; ++j)
		{
			sprintf_s(FileName, "./resource/isaac/player/attack/head%02d_%02d.png", i + 1, j + 1);
			tearHeadSprites[i][j].Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
		}
	}
	for (int i = 0; i < 2; ++i) 
	{
		for (int j = 0; j < 10; ++j) 
		{
			sprintf_s(FileName, "./resource/isaac/player/body/body%02d_%02d.png", i + 1, j + 1);
			bodySprites[i][j].Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
		}
	}

	sprintf_s(FileName, "./resource/isaac/menu/deadLight.png");
	lightSprite.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));

	sprintf_s(FileName, "./resource/isaac/player/motion/item.png");
	itemSprite.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
}

void Isaac::Update()
{
	if (isDeading)	// 죽었을 때
	{
		backLen = 0.0f;
		return;
	}

	if (isDamaging && GetTickCount() - damageTime > 1000)	// 무적 상태
	{
		isDamaging = false;
	}

	// 넉백
	if (backLen > 0.0f)
	{
		nextPos = pos + knockback * backLen;	// 다음 위치 = 넉백 방향 벡터 * 넉백 거리

		// 충돌 박스
		m_rc.left = nextPos.x;
		m_rc.top = nextPos.y + 20;
		m_rc.right = nextPos.x + 30;
		m_rc.bottom = nextPos.y + 50;

		if (!colliderManager.CheckCollision(m_rc) && !obstacle.CheckCollision(m_rc) && !monstermanager.CheckCollision(m_rc))	// 벽 & 장애물 & 몬스터 충돌 검사
			pos = nextPos;

		backLen *= 0.8f;	// 점점 멈추도록

		if (backLen < 0.1f)
			backLen = 0.0f;	// 종료
	}

	if (movechar == Move && GetTickCount() - m_IsaacTime > 50)		// 걷는 모션 효과
	{
		frameIndex = (frameIndex + 1) % 10;
		m_IsaacTime = GetTickCount();
	}

	if (isAttacking)	// 공격시 눈 깜빡이는 모션 효과
	{
		if (GetTickCount() - blinkTime > 300)
		{
			blinkState = !blinkState;
			blinkTime = GetTickCount();
		}
	}
	else
	{
		blinkState = false;
	}

	// 충돌 체크
	if (GetTickCount() - m_Collider > 10)
	{
		pos.x += moveX;

		m_rc.left = pos.x;
		m_rc.top = pos.y + 20;
		m_rc.right = pos.x + 30;	// imagesinfo.Width;
		m_rc.bottom = pos.y + 50;	// imagesinfo.Height;

		if (colliderManager.CheckCollision(m_rc) || obstacle.CheckCollision(m_rc) || monstermanager.CheckCollision(m_rc))
		{	// x좌표의 충돌 체크
			pos.x = prevPos.x;
		}

		pos.y += moveY;

		m_rc.left = pos.x;
		m_rc.top = pos.y + 20;
		m_rc.right = pos.x + 30;	// imagesinfo.Width;
		m_rc.bottom = pos.y + 50;	// imagesinfo.Height;
		
		if (colliderManager.CheckCollision(m_rc) || obstacle.CheckCollision(m_rc) || monstermanager.CheckCollision(m_rc))
		{	// y좌표의 충돌 체크
			pos.y = prevPos.y;
		}

		m_Collider = GetTickCount();	// 충돌 체크 시간
	}

	DoorDirection entered = room.doorManager.checkEnter(m_rc);	// 아이작이 문 안에 들어갔는지 체크
	
	if (entered != (DoorDirection)-1)
	{	// 문이 열려 있을 때 들어간 문의 방향으로 다음 방에 출력
		switch (entered)	// 방향별 새 방의 해당 위치로
		{
		case DOOR_LEFT:
			pos.x = 1100;
			room.MoveToDirection(r_Left);
			break;
		case DOOR_RIGHT:
			pos.x = 120;
			room.MoveToDirection(r_Right);
			break;
		case DOOR_UP:
			pos.y = 590;
			room.MoveToDirection(r_Up);
			break;
		case DOOR_DOWN:
			pos.y = 190;
			room.MoveToDirection(r_Down);
			break;
		}
		prevPos = pos;
	}

	obstacle.chest.TryOpen(m_rc, GetKeyCount());	// 보물 상자 호출
}

void Isaac::Draw()
{
	if (isDeading)	// 죽었을 때
	{
		lightSprite.Render(pos.x, pos.y - 190, 0, 0.5f, 0.5f);
		deadSprite.Render(pos.x, pos.y, 0, 2.0f, 2.0f);

		return;
	}

	if (isDamaging && GetTickCount() - damageTime < 300)	// 데미지
	{
		hurtSprite.Render(pos.x, pos.y, 0, 2.0f, 2.0f);
		g_SoundMgr.EffectPlay(2);

		return;
	}

	if ((pickSpeed || pickAttack || pickHeartItem) && GetTickCount() - pickTime < 1000)	// 아이템 픽업
		itemSprite.Render(pos.x, pos.y, 0, 2, 2);
	else
	{
		if (movechar == Stop)		// 캐릭터가 멈춰 있을 때
			bodySprites[0][0].Render(pos.x + 12, pos.y + 40, 0, 2, 2);
		else
		{
			if (vector == Left_)			// 왼쪽 방향 걷는 모션
				bodySprites[1][frameIndex].Render(pos.x + 30, pos.y + 55, 0, -2, 2, 1);
			else if (vector == Right_)		// 오른쪽 방향 걷는 모션
				bodySprites[1][frameIndex].Render(pos.x + 12, pos.y + 40, 0, 2, 2);
			else
				bodySprites[0][frameIndex].Render(pos.x + 12, pos.y + 40, 0, 2, 2);
		}									//위 아래 걷는 모션
	}

	if ((pickSpeed || pickAttack || pickHeartItem) && GetTickCount() - pickTime < 1000)
		itemSprite.Render(pos.x, pos.y, 0, 2, 2);
	else
	{
		if (!isAttacking)
		{					// 공격 안 할 때 얼굴 방향 이미지
			if (threeTear)
				tearHeadSprites[direction][0].Render(pos.x, pos.y, 0, 2, 2);
			else
				headSprites[direction][0].Render(pos.x, pos.y, 0, 2, 2);
		}
		else
		{
			int renderState = (isAttacking && !blinkState) ? Idle : Attack;
			int num = 0;

			switch (direction)
			{
			case Up:
				num = 0;
				break;
			case Right:
				num = 1;
				break;
			case Down:
				num = 2;
				break;
			case Left:
				num = 3;
				break;
			}
			// 공격시 눈 깜빡이는 효과와 방향
			if (threeTear)
				tearHeadSprites[num][renderState].Render(pos.x, pos.y, 0, 2, 2);
			else
				headSprites[num][renderState].Render(pos.x, pos.y, 0, 2, 2);
		}
	}
	coll.BoxSow(m_rc, 0, 0, 0xffff0000);	// 충돌 체크용 박스
}

void Isaac::Damage(int dmg)
{
	if (isDamaging)	// 무적 상태
		return;

	hp -= dmg;	// 데미지받으면 체력 감소

	char FileName[256];

	sprintf_s(FileName, "./resource/isaac/player/motion/hurt.png");
	hurtSprite.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));

	if (hp <= 0)	// 체력 0 이하
	{
		isDeading = true;

		sprintf_s(FileName, "./resource/isaac/player/motion/dead.png");
		deadSprite.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));
	}

	isDamaging = true;
	damageTime = GetTickCount();
}

void Isaac::AddTear(bool pickup)	// 눈물 아이템
{
	threeTear = pickup;
}

void Isaac::AddCoin()	// 동전
{
	coinCount++;
}

int Isaac::GetCoinCount() const	// 동전 개수
{
	return coinCount;
}

void Isaac::AddKey()	// 열쇠
{
	keyCount++;
}

int Isaac::GetKeyCount() const	// 열쇠 개수
{
	return keyCount;
}

bool Isaac::CheckKey()	// 열쇠 소지 여부
{
	return keyCount > 0;
}

void Isaac::UseKey()	// 열쇠 사용
{
	if (keyCount > 0)
		keyCount--;
}

void Isaac::AddBomb()	// 폭탄
{
	bombCount++;
}

int Isaac::GetBombCount() const	// 폭탄 개수
{
	return bombCount;
}

void Isaac::UseBomb()	// 폭탄 사용
{
	if (bombCount > 0)
		bombCount--;
}

void Isaac::PickSpeedItem()	// 스피드 아이템
{
	if (!pickSpeed)
	{
		pickSpeed = true;
		pickTime = GetTickCount();
		speed += 2.0f;
	}
}

void Isaac::PickAttackItem()	// 공격 아이템
{
	if (!pickAttack)
	{
		pickAttack = true;
		pickTime = GetTickCount();
	}
}

void Isaac::PickHeartItem()	// 하트 아이템
{
	if (!pickHeartItem)
	{
		pickHeartItem = true;
		pickTime = GetTickCount();
	}
}

void Isaac::UseHeartItem()	// 하트 사용
{
	if (pickHeartItem)
	{
		pickHeartItem = false;

		if (hp < maxHp)
			hp += 2;

		heartGauge = 0;
	}

	if (heartGauge >= 4)
	{
		heartGauge -= 4;
		PickHeartItem();
	}

	if (hp > isaac.maxHp)
		hp = isaac.maxHp;
	{
		if (hp >= 6)
			UI.m_Heart[0].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else if (hp == 5)
			UI.m_Heart[1].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else if (hp == 4)
			UI.m_Heart[2].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else if (hp == 3)
			UI.m_Heart[3].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else if (hp == 2)
			UI.m_Heart[4].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else if (hp == 1)
			UI.m_Heart[5].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
		else
			UI.m_Heart[0].Draw(UI.m_rc.left + 940, UI.m_rc.top + 32, 0.4f, 0.4f);
	}
}