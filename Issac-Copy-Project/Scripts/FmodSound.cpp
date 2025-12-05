#pragma comment(lib,"./FMODENGIN/lib/fmodex64_vc.lib")
#include "FMODENGIN/inc/fmod.hpp"
#include "FmodSound.h"

FmodSound g_SoundMgr;

FMOD::Channel* GetBGMChannel()	// 사운드 컨트롤
{
	return g_SoundMgr.GetBGMChannel();
}

int AddSoundFile(std::string _FullPath, bool _IsLoop)	// 사운드 파일 추가
{
	return g_SoundMgr.AddSoundFile(_FullPath, _IsLoop);
}

void EffectPlay(int _SoundNum)	// 효과음 재생
{
	g_SoundMgr.EffectPlay(_SoundNum);
}

void BGPlay(int _SoundNum)	// BGM 재생
{
	g_SoundMgr.BGPlay(_SoundNum);
}

// 볼륨 조절
void VolumUp()
{
	g_SoundMgr.VolumUp();
}

void VolumDown()
{
	g_SoundMgr.VolumDown();
}

void BGStop()	// BGM 멈추기
{
	g_SoundMgr.BGStop();
}
