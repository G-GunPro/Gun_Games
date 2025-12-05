#include "Include.h"

Collider coll;

Collider::Collider()
{
}

Collider::~Collider()
{
}

void Collider::Init(int x, int y, int width, int height)
{
	m_rc.left = x;
	m_rc.top = y;
	m_rc.right = x+width;
	m_rc.bottom = y+height;
}

void Collider::Update()
{
}

// 바닥
void Collider::Draw()
{
	if (Gmanager.m_GameStart == true)
	{
		dv_font.DrawString("┌ ", m_rc.left, m_rc.top, D3DCOLOR_ARGB(255, 0, 255, 0));
		dv_font.DrawString(" ┐", m_rc.right, m_rc.top, D3DCOLOR_ARGB(255, 0, 255, 0));
		dv_font.DrawString("└", m_rc.left, m_rc.bottom, D3DCOLOR_ARGB(255, 0, 255, 0));
		dv_font.DrawString(" ┘", m_rc.right, m_rc.bottom, D3DCOLOR_ARGB(255, 0, 255, 0));
	}
}

// 디버그 콜라이더 (x, y는 오프셋)
void Collider::BoxSow(RECT m_rc, long x, long y, D3DCOLOR color)
{
	if (Gmanager.m_GameStart == true&& colliderManager.debugDraw /* && 디버그 일때 처리 */)
	{
		dv_font.DrawString("┌ ", m_rc.left-x, m_rc.top  - y, color);
		dv_font.DrawString(" ┐", m_rc.right+x, m_rc.top - y, color);
		dv_font.DrawString("└", m_rc.left-x, m_rc.bottom + y, color);
		dv_font.DrawString(" ┘", m_rc.right+x, m_rc.bottom + y, color);
	}
}

void Collider::TearBoxSow(RECT m_rc, long x, long y, D3DCOLOR color)
{	// 눈물 충돌 박스
	if (Gmanager.m_GameStart == true && colliderManager.debugDraw)
	{
		dv_font.DrawString("┌ ", m_rc.left - x, m_rc.top - y + 10, color);
		dv_font.DrawString(" ┐", m_rc.right + x - 10, m_rc.top - y + 10, color);
		dv_font.DrawString("└", m_rc.left - x, m_rc.bottom + y + 10, color);
		dv_font.DrawString(" ┘", m_rc.right + x - 10, m_rc.bottom + y + 10, color);
	}

}

void Collider::BoxLine(RECT m_rc, long x, long y, D3DCOLOR color)
{	// 메뉴 게임 선택
	if (Gmanager.m_GameStart == true && colliderManager.debugDraw)
	{
		dv_font.DrawString("┌ ", 610, 580, color);
		dv_font.DrawString(" ┐", 800, 580, color);
		dv_font.DrawString("└", 610, 650, color);
		dv_font.DrawString(" ┘", 800, 650, color);
	}
}

bool Collider::CheckCollision(RECT a, RECT b)
{
	return !(a.right<b.left||a.left>b.right|| a.bottom < b.top || a.top > b.bottom);
}