#pragma once
#include <vector>
#include "Include.h"

class ColliderManager 
{
public:
	std::vector<Collider> walls;
	std::vector<RECT> wallRects;

	bool debugDraw = false;

	void AddWall(int x, int y, int width, int height) 
	{
		Collider wall;
		wall.Init(x, y, width, height);
		walls.push_back(wall);
	}

	void AddWallRect(int x, int y, int w, int h) 
	{
		RECT rect = { x,y,x + w,y + h };
		wallRects.push_back(rect);
	}

	void DrawWallDebug()
	{
		if (!debugDraw) 
			return;

		for (const RECT& rc : wallRects)
		{
			coll.BoxSow(rc, 0, 0, D3DCOLOR_ARGB(255, 0, 255, 0));
		}
	}

	void RemoveWall(int x, int y, int w, int h)
	{
		RECT target = { x,y,x + w,y + h };

		auto it = std::remove_if(wallRects.begin(), wallRects.end(),[&](const RECT& rc) 
			{
				return rc.left == target.left &&
					rc.top == target.top &&
					rc.right == target.right &&
					rc.bottom == target.bottom;
			});

		wallRects.erase(it, wallRects.end());
	}

	void Update() 
	{
		for (auto& wall : walls)
			wall.Update();
	}

	void Draw() 
	{
		for (auto& wall : walls)
			wall.Draw();
	}

	bool CheckCollision(const RECT& target)
	{
		for (auto& wall : walls) 
		{
			if (wall.CheckCollision(target, wall.m_rc))
				return true;
		}
		return false;
	}

	void DrawBoxes(const RECT& rc, long x = 0, long y = 0, D3DCOLOR color = 0xffff0000) 
	{
		for (auto& wall : walls)
			wall.BoxSow(wall.m_rc, x, y, color);
	}

	void Clear()
	{
		walls.clear();
	}

	void ToggleDebugDraw()
	{
		debugDraw = !debugDraw;
	}

};

extern ColliderManager colliderManager;