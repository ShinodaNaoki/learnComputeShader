// https://github.com/sugi-cho/Unity-ParticleSystem-GPUUpdate/blob/master/Assets/ParticleSystem-GPU/Shaders/ParticleSystem.Particle.cginc

#ifndef PARTICLE_SYSTEM_PARTICLE
#define PARTICLE_SYSTEM_PARTICLE
struct Particle
{
    float3 position;
    float3 velocity;
    float3 animatedVelocity;
    float3 unused_totalVelocity; //velocity��animated�̑����Z�B�����I�Ɍv�Z�����
    float3 axisOfRotation;
    float3 rotation3D;
    float3 angularVelocity3D;
    float3 startSize3D;
    uint startColor; //Color32�̑��� 0xFFFFFFFF�Őݒ肷��(ABRG�̏�)
    uint randomSeed;
    float remainingLifetime;
    float startLifetime;
    float unused_rotation; //rotation3D����A�����I�ɐݒ肳���ۂ�
    float unused_angularVelocity; //angularVelociry3D����A�����ݒ�
    //float startSize;
    // sizeof(ParticleSystem.Particle) = 120�Ȃ̂ŁAfloat��int�A1�������̂ŁA�Ƃ肠����startSize���R�����g�A�E�g���Ă݂�
    // ���̕��я��́AParticleUpdate.compute bridge-kernel�Œ�������
};


uint Particle_ColorToUint(half4 col)
{
    uint col32 = ((uint) (col.r * 0xFF))
        + ((uint) (col.g * 0xFF) << 8)
        + ((uint) (col.b * 0xFF) << 16)
        + ((uint) (col.a * 0xFF) << 24);
    return col32;
}

#endif
