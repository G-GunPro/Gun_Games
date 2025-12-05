#pragma once
#include "Include.h"

class Collider
{
public:
	Collider();
	~Collider();

	RECT m_rc;

	void Init(int x, int y, int width, int height);
	void Update();
	void Draw();
	void BoxSow(RECT m_rc, long x, long y, D3DCOLOR color = D3DCOLOR_ARGB(255, 0, 255, 0));
	void TearBoxSow(RECT m_rc, long x, long y, D3DCOLOR color = D3DCOLOR_ARGB(255, 0, 255, 0));
	void BoxLine(RECT m_rc, long x, long y, D3DCOLOR color = D3DCOLOR_ARGB(255, 0, 255, 0));
	
	static bool CheckCollision(RECT a, RECT b);
};

extern Collider coll;