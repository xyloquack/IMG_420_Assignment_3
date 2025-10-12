#[compute]
#version 450

struct BoidWeights {
    float separation;
    float alignment;
    float cohesion;
};

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) restrict buffer TurningVectors {
    vec2 data[];
} turningVectors;

layout(set = 0, binding = 1, std430) restrict buffer BoidPositionBuffer {
    vec2 data[];
} boidPosition;

layout(set = 0, binding = 2, std430) restrict buffer BoidVelocityBuffer {
    vec2 data[];
} boidVelocity;

layout(set = 0, binding = 3, std430) restrict buffer BoidGoalBuffer {
    vec2 data[];
} boidGoals;

layout(set = 0, binding = 4, std430) restrict buffer NumBoidBuffer {
    uint count;
} numBoid;

layout(set = 0, binding = 5, std430) restrict buffer BoidGridIndicesBuffer {
    uvec2 data[];
} boidGridIndices;

layout(set = 1, binding = 0, std430) restrict buffer SeparatingGridBuffer {
    vec2 data[];
} separatingGrid;

layout(set = 1, binding = 1, std430) restrict buffer SeparatingGridInfoBuffer {
    uint width;
    uint height;
    float distance;
    uvec2 cellData[];
} separatingGridInfo;

layout(set = 2, binding = 0, std430) restrict buffer LocalGridBuffer {
    vec4 data[];
} localGrid;

layout(set = 2, binding = 1, std430) restrict buffer LocalGridInfoBuffer {
    uint width;
    uint height;
    float distance;
    uvec2 cellData[];
} localGridInfo;

layout(set = 3, binding = 0, std430) restrict buffer TurningWeights {
    BoidWeights weights;
    float padding;
    float goalSeeking[];
} turningWeight;

vec2 Separation(vec2 boidPos, uint offset, uint count);
void Local(vec2 boidPos, uint offset, uint count, inout uint localCount, inout vec2 alignmentVector, inout vec2 cohesionVector);
vec2 GoalSeeking(vec2 boidPos, vec2 boidGoal);

void main() {
    uint boidInfoIndex = gl_GlobalInvocationID.x;
    if (boidInfoIndex >= numBoid.count) {
        return;
    }
    vec2 boidPos = boidPosition.data[boidInfoIndex];
    uint boidSeparatingGridIndex = boidGridIndices.data[boidInfoIndex].x;
    uint boidLocalGridIndex = boidGridIndices.data[boidInfoIndex].y;

    vec2 separationVector = vec2(0);
    uint sepGridX = boidGridIndices.data[boidInfoIndex].x % separatingGridInfo.width;
    uint sepGridY = boidGridIndices.data[boidInfoIndex].x / separatingGridInfo.width;

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            int currentY = int(sepGridY) + y;
            int currentX = int(sepGridX) + x;

            if (currentY >= 0 && currentY < separatingGridInfo.height && currentX >= 0 && currentX < separatingGridInfo.width) {
                uint neighborIndex = currentY * separatingGridInfo.width + currentX;
                separationVector += Separation(boidPos, separatingGridInfo.cellData[neighborIndex][0], separatingGridInfo.cellData[neighborIndex][1]);
            }
        }
    }

    vec2 alignmentVector = vec2(0);
    vec2 cohesionVector = vec2(0);
    uint localCount = 0;
    uint localGridX = boidGridIndices.data[boidInfoIndex].y % localGridInfo.width;
    uint localGridY = boidGridIndices.data[boidInfoIndex].y / localGridInfo.width;

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            int currentY = int(localGridY) + y;
            int currentX = int(localGridX) + x;

            if (currentY >= 0 && currentY < localGridInfo.height && currentX >= 0 && currentX < localGridInfo.width) {
                uint neighborIndex = currentY * localGridInfo.width + currentX;
                Local(boidPos, localGridInfo.cellData[neighborIndex][0], localGridInfo.cellData[neighborIndex][1], localCount, alignmentVector, cohesionVector);
            }
        }
    }

    vec2 finalVector = separationVector * turningWeight.weights.separation;

    if (localCount != 0)
    {
        alignmentVector = alignmentVector / localCount;
        if (dot(alignmentVector, alignmentVector) > 0.0) {
            finalVector += normalize(alignmentVector) * turningWeight.weights.alignment;
        }

        cohesionVector = cohesionVector / localCount - boidPosition.data[boidInfoIndex];
        if (dot(cohesionVector, cohesionVector) > 0.0) {
            finalVector += normalize(cohesionVector) * turningWeight.weights.cohesion;
        }
    }

    vec2 goalVector = GoalSeeking(boidPos, boidGoals.data[boidInfoIndex]);
    finalVector += goalVector * turningWeight.goalSeeking[boidInfoIndex];

    turningVectors.data[boidInfoIndex] = finalVector;
}

vec2 Separation(vec2 boidPos, uint offset, uint count) {
    if (count == 0)
    {
        return vec2(0, 0);
    }
    vec2 repulseVector = vec2(0, 0);
    for (uint i = offset; i < offset + count; i++)
    {
        vec2 currentBoidPos = separatingGrid.data[i].xy;
        float distSq = dot((currentBoidPos - boidPos), (currentBoidPos - boidPos));
        if (distSq < separatingGridInfo.distance && boidPos != currentBoidPos)
        {
            if (distSq > 0.0001)
            {
                repulseVector += (boidPos - currentBoidPos) * float(1.0 / (dot((currentBoidPos - boidPos), (currentBoidPos - boidPos)) + 0.0001));
            }
        }
    }
    if (dot(repulseVector, repulseVector) < 0.0001)
    {
        return vec2(0, 0);
    }
    return repulseVector;
}

void Local(vec2 boidPos, uint offset, uint count, inout uint localCount, inout vec2 alignmentVector, inout vec2 cohesionVector) {
    if (count == 0)
    {
        return;
    }
    vec2 velocitySum = vec2(0, 0);
    vec2 positionSum = vec2(0, 0);
    for (uint i = offset; i < offset + count; i++)
    {
        vec2 currentBoidPos = localGrid.data[i].xy;
        vec2 currentBoidVelocity = localGrid.data[i].zw;
        float distSq = dot(boidPos - currentBoidPos, boidPos - currentBoidPos);
        if (distSq < localGridInfo.distance && distSq > 0.0001)
        {
            localCount++;
            alignmentVector += currentBoidVelocity;
            cohesionVector += currentBoidPos;
        }
    }
}

vec2 GoalSeeking(vec2 boidPos, vec2 boidGoal) {
    vec2 neededDirection = normalize(boidGoal - boidPos);
	return neededDirection;
}