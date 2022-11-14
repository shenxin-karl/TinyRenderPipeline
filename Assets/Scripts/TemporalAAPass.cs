using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TemporalAAPass : IPass {
    static readonly float[,] Halton23 = {
        { 0.0f / 8.0f, 0.0f / 9.0f }, { 4.0f / 8.0f, 3.0f / 9.0f },
        { 2.0f / 8.0f, 6.0f / 9.0f }, { 6.0f / 8.0f, 1.0f / 9.0f },
        { 1.0f / 8.0f, 4.0f / 9.0f }, { 5.0f / 8.0f, 7.0f / 9.0f },
        { 3.0f / 8.0f, 2.0f / 9.0f }, { 7.0f / 8.0f, 5.0f / 9.0f }
    };

    private uint _currentFrameIndex = 0;
    
    public TemporalAAPass(CameraRenderer cameraRenderer) : base(cameraRenderer) {
    }

    public override void ExecutePass(ScriptableRenderContext context) {
        ++_currentFrameIndex;
    }

    public Vector2 Jitter {
        get {
            uint index = _currentFrameIndex % 8;
            return new Vector2(Halton23[index, 0], Halton23[index, 1]);
        }
    } 
    
}
