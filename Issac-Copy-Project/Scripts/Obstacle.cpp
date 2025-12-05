#include "Include.h"

Obstacle obstacle;

Obstacle::Obstacle()
{

}

Obstacle::~Obstacle()
{

}

Obstacle::Obstacle(const Obstacle& other)
{
	obstacles = other.obstacles;
	poop = other.poop;
    chest = other.chest;
}

Obstacle& Obstacle::operator=(const Obstacle& other)
{
	if (this != &other)
	{
		obstacles = other.obstacles;
		poop = other.poop;
        chest = other.chest;
	}
	return *this;
}

void Obstacle::Init()
{
	char FileName[256];
	sprintf_s(FileName, "./resource/Img/obstacle/rock01.png");
	rock1.Create(FileName, false, D3DCOLOR_XRGB(0, 0, 0));

	poop.Init();
    chest.Init();
}

void Obstacle::AddObstacle(int x, int y, int w, int h)		// 厘局拱 积己
{
	Collider obs;
	obs.Init(x, y, w, h);
	obstacles.push_back(obs);
}

void Obstacle::Update() 
{
	poop.Update();
    chest.Update();
}

void Obstacle::Draw()
{
	for (auto& obs : obstacles)
	{
		obs.BoxSow(obs.m_rc, 0, 0, 0xff00ff00);
		RECT& rc = obs.m_rc;
		int width = rc.right-rc.left;
		int height = rc.bottom-rc.top;

		float scaleX = (float)width / rock1.imagesinfo.Width;
		float scaleY = (float)height / rock1.imagesinfo.Height;

		rock1.Render(rc.left+10, rc.top+10, 0, scaleX+0.2, scaleY+0.2);
	}

	poop.Draw();
    chest.Draw();
}

bool Obstacle::CheckCollision(const RECT& rc)		// 厘局拱 面倒贸府
{
	for (auto& obs : obstacles)
	{
		if (Collider::CheckCollision(rc, obs.m_rc))
			return true;
	}
	return poop.CheckCollision(rc)||chest.CheckCollision(rc);
}

void Obstacle::Clear()
{
	obstacles.clear();
	poop.Clear();
    //chest.Clear();
}

void Obstacle::AssignObstaclesToRoom(int type)
{
    Clear();

    switch (type)
    {
    case 0:
        AddObstacle(330, 270, 64, 64);
        AddObstacle(470, 270, 64, 64);
        AddObstacle(400, 220, 64, 64);

        AddObstacle(590, 270, 64, 64);
        AddObstacle(710, 270, 64, 64);
        AddObstacle(850, 270, 64, 64);
        AddObstacle(780, 220, 64, 64);

        AddObstacle(330, 390, 64, 64);
        AddObstacle(850, 390, 64, 64);
        AddObstacle(710, 390, 64, 64);
        AddObstacle(470, 390, 64, 64);
        break;
    case 1:
        AddObstacle(590, 390, 64, 64);
        AddObstacle(590, 315, 64, 64);
        AddObstacle(590, 460, 64, 64);
        AddObstacle(670, 390, 64, 64);
        AddObstacle(510, 390, 64, 64);

        //AddObstacle(140, 210, 64, 64);
        AddObstacle(220, 210, 64, 64);
        AddObstacle(140, 280, 64, 64);
        AddObstacle(220, 280, 64, 64);

        AddObstacle(1040, 210, 64, 64);
        AddObstacle(140, 560, 64, 64);
        AddObstacle(1040, 560, 64, 64);
        break;
    case 2:
        AddObstacle(220, 280, 64, 64);
        AddObstacle(220, 200, 64, 64);
        AddObstacle(140, 280, 64, 64);

        AddObstacle(960, 280, 64, 64);
        AddObstacle(960, 200, 64, 64);
        AddObstacle(1040, 280, 64, 64);

        AddObstacle(140, 480, 64, 64);
        AddObstacle(220, 480, 64, 64);
        AddObstacle(220, 560, 64, 64);

        AddObstacle(960, 480, 64, 64);
        AddObstacle(960, 560, 64, 64);
        AddObstacle(1040, 480, 64, 64);
        break;
    case 3:
        AddObstacle(670, 375, 64, 64);
        AddObstacle(520, 375, 64, 64);
        AddObstacle(600, 300, 64, 64);
        AddObstacle(600, 450, 64, 64);
        break;
    }

    poop.AssignObstaclesToRoom(type);
}

void Obstacle::OnHit(const RECT& attackRc) 
{
	poop.HitPoop(attackRc);
}

void Obstacle::TryOpenChest(const RECT& isaacRc)
{
    chest.TryOpen(isaacRc, isaac.GetKeyCount());
}