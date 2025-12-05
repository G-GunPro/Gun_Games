#pragma once
#include "FMODENGIN/inc/fmod.hpp"
#include "Include.h"
#include <string>

using namespace FMOD;

class FmodSound
{
	System* m_pSystem;	// FMOD 시스템
	Channel* m_pBGChannel;	// 배경음
	Channel* m_pChannel;	// 효과음

	float m_vloum;	// 전체 볼륨
	float m_vloumBGM;	// BGM 볼륨
	float m_vloumSE;	// 효과음 볼륨
	int	m_Index;
	int mainsound;
	int menusound;
	int isaachit;
	int isaactear;
	int opendoor;
	int closedoor;
	int bomb;
	int hpup;
	int openchest;
	int itempick;
	int coinpick;

public:
	std::map<std::string, int> m_CheckList;	// 중복 경로 방지
	std::map<int, Sound*>  	   m_SoundList;	// 사운드 리스트

	FmodSound()
	{
		System_Create(&m_pSystem);	// 시스템 생성
		m_pSystem->init(11, FMOD_INIT_NORMAL, 0);	// 최대 11개

		m_Index = 0;
		m_vloum = 0.5f;
		m_vloumBGM = 0.5f;
		m_vloumSE = 0.5f;
		m_pBGChannel = nullptr;

		mainsound = AddSoundFile("./resource/Sound/isaacmain.mp3", true);
		menusound=AddSoundFile("./resource/Sound/isaacmenu.mp3",true);
		isaachit = AddSoundFile("./resource/Sound/isaachit.wav", false);
		isaactear = AddSoundFile("./resource/Sound/isaactear.wav", false);
		opendoor = AddSoundFile("./resource/Sound/opendoor.wav", false);
		closedoor = AddSoundFile("./resource/Sound/closedoor.wav", false);
		bomb = AddSoundFile("./resource/Sound/bomb.wav", false);
		hpup = AddSoundFile("./resource/Sound/hpup.wav", false);
		openchest = AddSoundFile("./resource/Sound/openchest.wav", false);
		itempick = AddSoundFile("./resource/Sound/itempick.wav", false);
		coinpick = AddSoundFile("./resource/Sound/coinpick.wav", false);
	}

	~FmodSound()
	{
		m_CheckList.clear();

		for (auto& lter : m_SoundList)
		{
			lter.second->release();
		}
		m_SoundList.clear();

		m_pSystem->release();
		m_pSystem->close();
	}

	int AddSoundFile(std::string _FullPath, bool _IsLoop)	// 사운드 추가
	{
		auto Find = m_CheckList.find(_FullPath);

		if (Find != m_CheckList.end())
		{
			return Find->second;
		}

		Sound* pSound = nullptr;
		int Mode = FMOD_HARDWARE | (_IsLoop ? FMOD_LOOP_NORMAL | FMOD_DEFAULT : FMOD_LOOP_OFF);

		m_pSystem->createSound(_FullPath.c_str(), Mode, 0, &pSound);

		if (pSound == nullptr)
		{
			return -1;
		}

		m_CheckList.insert(std::make_pair(_FullPath, m_Index));
		m_SoundList.insert(std::make_pair(m_Index, pSound));

		return m_Index++;
	}

	void EffectPlay(int _SoundNum)	// 효과음 재생
	{
		auto Find = m_SoundList.find(_SoundNum);

		m_pSystem->playSound(FMOD_CHANNEL_FREE, Find->second, 0, &m_pChannel);
		if (m_pChannel)
			m_pChannel->setVolume(m_vloumSE);
	}

	void BGPlay(int _SoundNum)	// BGM 재생
	{
		auto Find = m_SoundList.find(_SoundNum);

		m_pSystem->playSound(FMOD_CHANNEL_REUSE, Find->second, 0, &m_pBGChannel);
		
		if (m_pBGChannel)
			m_pBGChannel->setVolume(m_vloumBGM);
	}

	void BGStop()	// BGM 정지
	{
		if (m_pBGChannel != nullptr)
		{
			m_pBGChannel->stop();
		}
	}

	// 볼륨 조절
	void VolumDown()
	{
		m_vloum -= 0.1f;
		
		if (m_vloum <= 0)
			m_vloum = 0;

		m_pBGChannel->setVolume(m_vloum);
		m_pChannel->setVolume(m_vloum);
	}

	void VolumUp()
	{
		m_vloum += 0.1f;

		if (m_vloum >= 1.0)
			m_vloum = 1.0;

		m_pBGChannel->setVolume(m_vloum);
		m_pChannel->setVolume(m_vloum);
	}

	FMOD::Channel* GetBGMChannel() const 
	{
		return m_pBGChannel;
	}
	FMOD::Channel* GetSEChannel() const 
	{
		return m_pChannel;
	}

	float GetBGMVolume() const { return m_vloumBGM; }
	float GetSEVolume() const { return m_vloumSE; }

	void SetBGMVolume(float vol)
	{
		if (vol < 0.0f) 
			vol = 0.0f;
		if (vol > 1.0f)
			vol = 1.0f;
		
		m_vloumBGM = vol;

		if (m_pBGChannel) 
			m_pBGChannel->setVolume(m_vloumBGM);
	}

	void SetSEVolume(float vol)	// 볼륨 설정
	{
		if (vol < 0.0f) 
			vol = 0.0f;
		if (vol > 1.0f)
			vol = 1.0f;
		
		m_vloumSE = vol;

		if (m_pChannel) 
			m_pChannel->setVolume(m_vloumSE);
	}
};
extern FmodSound g_SoundMgr;