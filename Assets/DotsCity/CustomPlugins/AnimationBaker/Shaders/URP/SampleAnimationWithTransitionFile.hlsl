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
	in float transitionTime,
	in float targetPlaybackTime,
	in float targetFrameStepInv,
	in float2 targetFrameOffset,
	in bool interpolate,
	out float3 position, out float3 normal)
{
    float currentPlaybackTime = playbackTime >= 0 ? playbackTime : globalTime % clipLength;
     
    float framePlayback = currentPlaybackTime * frameStepInv;
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

    if (targetFrameOffset.x >= 0)
    {
        float targetFrameNumber = floor(targetPlaybackTime * targetFrameStepInv);
            
        float3 targetPosition;
        float3 targetNormal;
    
        CalcVertex(animationTexture, normalTexture, vertexId, targetFrameNumber, vertexCount, targetFrameOffset, targetPosition, targetNormal);
 
        float remainTime = clipLength - currentPlaybackTime;
        float currentLerp = clamp(1 - remainTime / transitionTime, 0, 1);
        
        currentPosition = lerp(currentPosition, targetPosition, currentLerp);
        currentNormal = lerp(currentNormal, targetNormal, currentLerp);
    }
    
    position = currentPosition;
    normal = currentNormal;
}

