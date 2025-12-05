#pragma once
#include"Include.h"
#include "PoopObstacle.h"
#include "Chest.h"
#include<vector>

class Obstacle 
{
public:
	std::vector<Collider> obstacles;

	Obstacle();
	~Obstacle();

	Obstacle(const Obstacle& other);

	Obstacle& operator=(const Obstacle& other);
	Sprite rock1;
	PoopObstacle poop;
	Chest chest;

	void Init();
	void AddObstacle(int x, int y, int w, int h);
	void Draw();
	void Update();
	bool CheckCollision(const RECT& rc);
	void OnHit(const RECT& attackRc);
	void Clear();
	void AssignObstaclesToRoom(int type);
	void TryOpenChest(const RECT& isaacRc);
};

extern Obstacle obstacle;