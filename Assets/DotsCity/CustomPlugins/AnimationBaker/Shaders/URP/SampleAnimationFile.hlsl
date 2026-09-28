#include "CalcVertexUtils.hlsl"

void SampleAnimation_float(
	in UnityTexture2D animationTexture,
	in UnityTexture2D normalTexture,
	in float vertexId,
	in float globalTime,
	in float playbackTime,
	in float clipLength,
	in float vertexCount,
	in float frameStepInv,
	in float frameCount,
	in float2 frameOffset,
	in bool interpolate,
	out float3 position, out float3 normal)
{
    float currentPlayback = playbackTime >= 0 ? playbackTime : globalTime % clipLength;
     
    float framePlayback = currentPlayback * frameStepInv;
    float frameNumber = floor(framePlayback);

    float3 currentPosition;
    float3 currentNormal;
    
    CalcVertex(animationTexture, normalTexture, vertexId, frameNumber, vertexCount, frameOffset, currentPosition, currentNormal);

    if (interpolate)
    {
        float nextFrameNumber = (frameNumber + 1) % frameCount;
    
        float3 nextPosition;
        float3 nextNormal;
    
        CalcVertex(animationTexture, normalTexture, vertexId, nextFrameNumber, vertexCount, frameOffset, nextPosition, nextNormal);
    
        float frameT = framePlayback - frameNumber;
        
        currentPosition = lerp(currentPosition, nextPosition, frameT);
        currentNormal = lerp(currentNormal, nextNormal, frameT);
    }
  
    position = currentPosition;
    normal = currentNormal;
}

