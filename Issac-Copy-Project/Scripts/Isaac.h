#pragma once
#include"Include.h"

enum Direction { Up = 0, Right = 1, Down = 2, Left = 3 };	// 공격방향
enum State {Idle=0,Attack};									// 일반/공격
enum Vector {UpDown=0,Left_,Right_};						// 이동 시 방향
enum MoveChar {Stop=0,Move};								// 캐릭터 움직임

class Isaac
{
	// 소지 아이템 Count
	int coinCount = 0;
	int bombCount = 5;
	int keyCount = 0;

public:
	Isaac();
	~Isaac();

	float moveX, moveY;	// 이동
	float backLen = 0;	// 넉백
	float speed = 4.0f;	// 속도
	int hp;	// 체력
	int maxHp;	// 최대 체력
	int frameIndex;	// 프레임
	int heartGauge = 6;	// 아이템 게이지

	Sprite headSprites[4][2];
	Sprite tearHeadSprites[4][2];
	Sprite bodySprites[2][10];
	Sprite deadSprite;
	Sprite hurtSprite;
	Sprite lightSprite;
	Sprite itemSprite;

	D3DXVECTOR2 pos;	// 현재 위치
	D3DXVECTOR2 prevPos;	// 이전 위치
	D3DXVECTOR2 knockback = { 0,0 };	// 넉백 방향 벡터
	D3DXVECTOR2 nextPos;	// 다음 위치
	D3DXIMAGE_INFO imagesinfo;	// 이미지 정보

	RECT m_rc;	// 충돌

	Direction direction;	// 방향
	Direction attackDirection;	// 공격 방향
	State state;	// 상태
	Vector vector;	// 구분 방향
	MoveChar movechar;	// 움직임

	bool blinkState;	// 눈 깜빡임
	bool isAttacking;	// 공격 여부
	bool isDamaging;	// 데미지 여부
	bool isDeading = false;	// 죽음 여부

	// 아이템 습득 여부
	bool threeTear = false;	
	bool pickSpeed = false;	
	bool pickAttack = false;	
	bool pickHeartItem = false;

	// 시간 측정용
	DWORD m_IsaacTime;
	DWORD attackTime;
	DWORD damageTime;
	DWORD blinkTime;
	DWORD m_Collider;
	const DWORD attackDuration = 300;	// 공격 지속 시간
	DWORD pickTime;

	void Init();
	void Update();
	void Draw();
	void SavePrevPosition() 
	{
		prevPos.x = pos.x;
		prevPos.y = pos.y;
	}
	void Damage(int dmg);

	void AddCoin();
	int GetCoinCount() const;
	void AddBomb();
	int GetBombCount() const;
	void UseBomb();
	void AddKey();
	int GetKeyCount() const;
	bool CheckKey();
	void UseKey();
	void AddTear(bool pickup);
	void PickSpeedItem();
	void PickAttackItem();
	void PickHeartItem();
	void UseHeartItem();
};

extern Isaac isaac;