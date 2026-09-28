void CalcVertex(
    UnityTexture2D animationTexture,
    UnityTexture2D normalTexture,
    float vertexId,
    float frameNumber,
    float2 vertexCount,
    float2 frameOffset,
    out float3 position,
    out float3 normal)
{
    float positionY = (frameOffset.y + frameNumber * vertexCount + vertexId + 0.5) * animationTexture.texelSize.y;
    
    float uvFrameOffset = floor(positionY);
    
    positionY -= uvFrameOffset;
    
    float positionX = (frameOffset.x + uvFrameOffset + 0.5) * animationTexture.texelSize.x;
    
    float2 positionUv = float2(positionX, positionY);
    
    position = animationTexture.SampleLevel(animationTexture.samplerstate, positionUv, 0).xyz;
    normal = normalTexture.SampleLevel(normalTexture.samplerstate, positionUv, 0).xyz;
}